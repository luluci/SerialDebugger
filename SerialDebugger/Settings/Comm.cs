
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

using SerialDebugger.Comm;
using System.Windows;

namespace SerialDebugger.Settings
{
    class Comm : BindableBase, IDisposable
    {
        public ReactiveCollection<TxFrame> Tx { get; set; }
        public Dictionary<string, int> TxNameDict { get; set; }
        // rx_autoresp
        // tx_autosend
        public ReactiveCollection<AutoTxJob> AutoTx { get; set; }

        public Comm()
        {
            Tx = new ReactiveCollection<TxFrame>();
            Tx.AddTo(Disposables);
            TxNameDict = new Dictionary<string, int>();

            AutoTx = new ReactiveCollection<AutoTxJob>();
            AutoTx.AddTo(Disposables);
        }

        public async Task AnalyzeJsonAsync(Json.Comm json)
        {
            if (json is null || json.Tx is null || json.Tx.Frames is null)
            {
                throw new Exception("txキーが定義されていません");
            }

            // TxFrameコレクション作成
            int i = 0;
            foreach (var frame in json.Tx.Frames)
            {
                var f = await MakeTxFrameAsync(i, frame);
                Tx.Add(f);
                TxNameDict.Add(f.Name, f.Id);
                i++;
            }


            // AutoTx作成
            if (!(json.AutoTx is null) && !(json.AutoTx.Jobs is null))
            {
                int id = 0;
                foreach (var job in json.AutoTx.Jobs)
                {
                    AutoTx.Add(MakeAutoTxJob(id, job));
                    id++;
                }
            }
        }

        private AutoTxJob MakeAutoTxJob(int id, Json.CommAutoTxJob job)
        {
            // Job作成
            var j = new AutoTxJob(id, job.Name, job.Active);
            // Action解析
            if (!(job.Actions is null))
            {
                int i = 0;
                foreach (var action in job.Actions)
                {
                    j.Actions.Add(MakeAutoTxAction(i, action));
                    i++;
                }
            }
            //
            j.Build(Tx);

            return j;
        }

        private AutoTxAction MakeAutoTxAction(int id, Json.CommAutoTxAction action)
        {
            switch (action.Type)
            {
                case "Send":
                    return MakeAutoTxActionSend(id, action);

                case "Wait":
                    return MakeAutoTxActionWait(id, action);

                case "Recv":
                    throw new Exception("unimplemented!");

                case "Jump":
                    return MakeAutoTxActionJump(id, action);

                case "Script":
                    throw new Exception("unimplemented!");

                default:
                    throw new Exception($"Undefined AutoTx Action Type: {action.Type}");
            }
        }
        private AutoTxAction MakeAutoTxActionSend(int id, Json.CommAutoTxAction action)
        {
            if (Object.ReferenceEquals(action.TxFrameName,string.Empty))
            {
                throw new Exception("AutoTx: Action: Send: 送信対象となるframe名(tx_frame_name)を指定してください。");
            }
            if (action.TxFrameBuffIndex == -1)
            {
                throw new Exception("AutoTx: Action: Send: 送信対象となるframeバッファ(tx_frame_buff_index)を指定してください。");
            }

            // FrameNameが存在しないときの例外処理は上流に任せる
            var frame_idx = TxNameDict[action.TxFrameName];
            var act = AutoTxAction.MakeSendAction(id, action.TxFrameName, frame_idx, action.TxFrameBuffIndex);

            return act;
        }
        private AutoTxAction MakeAutoTxActionWait(int id, Json.CommAutoTxAction action)
        {
            if (action.WaitTime == -1)
            {
                throw new Exception("AutoTx: Action: Wait: 待機時間(WaitTime)を指定してください。");
            }

            var act = AutoTxAction.MakeWaitAction(id, action.WaitTime);

            return act;
        }
        private AutoTxAction MakeAutoTxActionJump(int id, Json.CommAutoTxAction action)
        {
            if (action.JumpTo == -1)
            {
                throw new Exception("AutoTx: Action: Jump: JumpToを指定してください。");
            }

            var act = AutoTxAction.MakeJumpAction(id, action.JumpTo);

            return act;
        }


        /// <summary>
        /// JsonTxFrameからTxFrameを作成する
        /// </summary>
        /// <param name="frame"></param>
        private async Task<TxFrame> MakeTxFrameAsync(int id, Json.CommTxFrame frame)
        {
            // TxFrame作成
            var f = new TxFrame(id, frame.Name);
            if (!(frame.Fields is null))
            {
                int i = 0;
                foreach (var field in frame.Fields)
                {
                    f.Fields.Add(await MakeTxFieldAsync(i, field));
                    i++;
                }
                f.Build();
            }
            // TxFrame作成後にBackupBuffer作成
            MakeTxBackupBuffers(frame, f);

            return f;
        }

        private void MakeTxBackupBuffers(Json.CommTxFrame frame, TxFrame f)
        {
            // バッファサイズ作成
            f.BackupBufferLength = frame.BackupBufferSize;
            if (!(frame.BackupBuffers is null))
            {
                if (f.BackupBufferLength < frame.BackupBuffers.Count)
                {
                    f.BackupBufferLength = frame.BackupBuffers.Count;
                }
            }

            // 送信データバックアップバッファ作成
            if (!(frame.BackupBuffers is null))
            {
                for (int i = 0; i < f.BackupBufferLength; i++)
                {
                    if (i < frame.BackupBuffers.Count)
                    {
                        // BackupBuffers定義から作成
                        f.BackupBuffer.Add(MakeTxBackupBuffer(frame.BackupBuffers[i], f, i));
                    }
                    else
                    {
                        // 空バッファを作成
                        f.BackupBuffer.Add(MakeTxBackupBufferEmpty($"buffer[{i}]", f, i));
                    }
                }
            }
            else
            {
                // BackupBuffers未定義ならすべて空バッファ作成
                for (int i = 0; i < f.BackupBufferLength; i++)
                {
                    f.BackupBuffer.Add(MakeTxBackupBufferEmpty($"buffer[{i}]", f, i));
                }
            }
        }

        private TxBackupBuffer MakeTxBackupBuffer(Json.CommTxBackupBuffer json, TxFrame f, int idx)
        {
            // Buffer名称作成
            string name = json.Name;
            if (Object.ReferenceEquals(name, string.Empty))
            {
                name = $"buffer[{idx}]";
            }
            // Buffer作成
            var buffer = new TxBackupBuffer(idx, name, f);
            // value定義が無いなら空バッファを返す
            if (json.Values is null)
            {
                return buffer;
            }

            // json設定値反映
            for (int i = 0; i < json.Values.Count; i++)
            {
                // valueをバッファに反映
                var field = f.Fields[i];
                var value = json.Values[i];
                var bk_field = buffer.Fields[i];
                bk_field.SetValue(value);
                f.UpdateBuffer(field, value, buffer.TxBuffer);
            }
            
            // チェックサム
            if (f.HasChecksum)
            {
                var i = f.ChecksumIndex;
                var field = f.Fields[i];
                var value = f.CalcChecksum(buffer.TxBuffer);
                buffer.Fields[i].Value.Value = value;
                f.UpdateBuffer(field, value, buffer.TxBuffer);
            }

            return buffer;
        }
        private TxBackupBuffer MakeTxBackupBufferEmpty(string name, TxFrame f, int idx)
        {
            // Buffer作成
            return new TxBackupBuffer(idx, name, f);
        }

        private async Task<TxField> MakeTxFieldAsync(int id, Json.CommTxField field)
        {
            switch (field.Type)
            {
                case "Checksum":
                    return await MakeTxFieldChecksumAsync(id, field);
                case "Dict":
                    return await MakeTxFieldDictAsync(id, field);
                case "Unit":
                    return await MakeTxFieldUnitAsync(id, field);
                case "Time":
                    return await MakeTxFieldTimeAsync(id, field);
                case "Script":
                    return await MakeTxFieldScriptAsync(id, field);
                case "Edit":
                    return await MakeTxFieldImplAsync(id, field, TxField.InputModeType.Edit, null);
                case "Fix":
                default:
                    return await MakeTxFieldImplAsync(id, field, TxField.InputModeType.Fix, null);
            }
        }

        private async Task<TxField> MakeTxFieldChecksumAsync(int id, Json.CommTxField field)
        {
            // Checksumチェック
            if (field.Checksum is null)
            {
                throw new Exception("type:ChecksumではChecksumオブジェクトを指定してください。");
            }
            // Nameチェック
            // Checksumの場合はmulti_nameは許可しない
            if (Object.ReferenceEquals(field.Name, string.Empty))
            {
                throw new Exception("Checksumノードはname,bit_sizeを指定してください。");
            }
            // Checksum計算方法
            var method = new TxField.ChecksumMethod();
            switch (field.Checksum.Method)
            {
                case "2compl":
                    // 2の補数
                    method = TxField.ChecksumMethod.cmpl_2;
                    break;
                case "1compl":
                    // 1の補数
                    method = TxField.ChecksumMethod.cmpl_1;
                    break;
                case "Sum":
                default:
                    // 総和
                    method = TxField.ChecksumMethod.None;
                    break;
            }
            // TxField生成
            var result = new TxField(id, new TxField.ChecksumNode
            {
                Name = field.Name,
                BitSize = field.BitSize,
                Begin = field.Checksum.Begin,
                End = field.Checksum.End,
                Method = method,
            });
            await result.InitAsync();
            return result;
        }

        private async Task<TxField> MakeTxFieldDictAsync(int id, Json.CommTxField field)
        {
            // nullチェック
            var dict = field.Dict;
            if (dict is null)
            {
                throw new Exception("type:DictではDictオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = new (UInt64, string)[dict.Count];
            int i = 0;
            foreach (var pair in dict)
            {
                selecter[i] = (pair.Value, pair.Disp);
                i++;
            }
            // TxField生成
            return await MakeTxFieldImplAsync(id, field, TxField.InputModeType.Dict, TxField.MakeSelecterDict(selecter));
        }

        private async Task<TxField> MakeTxFieldUnitAsync(int id, Json.CommTxField field)
        {
            // nullチェック
            var unit = field.Unit;
            if (unit is null)
            {
                throw new Exception("type:UnitではUnitオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = TxField.MakeSelecterUnit(unit.Unit, unit.Lsb, unit.DispMax, unit.DispMin, unit.ValueMin, unit.Format);
            // TxField生成
            return await MakeTxFieldImplAsync(id, field, TxField.InputModeType.Unit, selecter);
        }

        private async Task<TxField> MakeTxFieldTimeAsync(int id, Json.CommTxField field)
        {
            // nullチェック
            var time = field.Time;
            if (time is null)
            {
                throw new Exception("type:TimeではTimeオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = TxField.MakeSelecterTime(time.Elapse, time.Begin, time.End, time.ValueMin);
            // TxField生成
            return await MakeTxFieldImplAsync(id, field, TxField.InputModeType.Time, selecter);
        }

        private async Task<TxField> MakeTxFieldScriptAsync(int id, Json.CommTxField field)
        {
            // nullチェック
            var script = field.Script;
            if (script is null)
            {
                throw new Exception("type:ScriptではScriptオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = TxField.MakeSelecterScript(script.Mode, script.Count, script.Script);
            // TxField生成
            return await MakeTxFieldImplAsync(id, field, TxField.InputModeType.Script, selecter);
        }

        private async Task<TxField> MakeTxFieldImplAsync(int id, Json.CommTxField field, TxField.InputModeType type, TxField.Selecter selecter)
        {
            TxField result;
            // name, multi_name選択
            if (!(field.MultiNames is null))
            {
                // multi_name優先
                int i = 0;
                var multi_name = new TxField.InnerField[field.MultiNames.Count];
                foreach (var pair in field.MultiNames)
                {
                    multi_name[i] = new TxField.InnerField(pair.Name, pair.BitSize);
                    i++;
                }
                // name設定
                // nameがnullのときはmulti_nameの最初のnameを使用
                var name = field.Name;
                if (Object.ReferenceEquals(name, string.Empty))
                {
                    name = multi_name[0].Name;
                }
                // multi_name指定時はBitSizeは使わない
                // TxField生成
                result = new TxField(id, name, multi_name, field.Value, type, selecter);
            }
            else
            {
                // multi_nameが指定されていないときはnameとbitsizeを使用
                // データチェック, nameとbitsizeの両方が有効である必要がある
                if (Object.ReferenceEquals(field.Name, string.Empty))
                {
                    throw new Exception("nameかmulti_nameのどちらかを指定してください。");
                }
                if (field.BitSize <= 0)
                {
                    throw new Exception("有効なbit_sizeを指定してください。");
                }
                var multi_name = new TxField.InnerField[1];
                multi_name[0] = new TxField.InnerField(field.Name, field.BitSize);
                // TxField生成
                result = new TxField(id, field.Name, multi_name, field.Value, type, selecter);
            }

            await result.InitAsync();
            return result;
        }


        #region IDisposable Support
        private CompositeDisposable Disposables { get; } = new CompositeDisposable();

        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)。
                    this.Disposables.Dispose();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~TxFrame() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        void IDisposable.Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

}
