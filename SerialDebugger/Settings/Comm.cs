
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
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
        // rx_autoresp
        // tx_autosend

        public Comm()
        {
            Tx = new ReactiveCollection<TxFrame>();
            Tx.AddTo(Disposables);
        }

        public async Task AnalyzeJsonAsync(Json.Comm json)
        {
            if (json is null || json.Tx is null)
            {
                throw new Exception("txキーが定義されていません");
            }

            // TxFrameコレクション作成
            int i = 0;
            foreach (var frame in json.Tx.Frames)
            {
                Tx.Add(await MakeTxFrameAsync(i, frame));
                i++;
            }
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
            if (name == string.Empty)
            {
                name = $"buffer[{idx}]";
            }
            // value定義が無いなら空バッファを返す
            if (json.Values is null)
            {
                return MakeTxBackupBufferEmpty(name, f, idx);
            }

            // Buffer作成
            var buffer = new TxBackupBuffer(name, f.Fields.Count, f.Length);

            // Values作成
            for (int i=0; i<f.Fields.Count; i++)
            {
                var field = f.Fields[i];
                if (i < json.Values.Count)
                {
                    // valueをバッファに反映
                    var value = json.Values[i];
                    buffer.Value[i] = value;
                    f.UpdateBuffer(field, value, buffer.Buffer);
                    // 表示名を作成
                    var index = field.GetSelectsIndex(value);
                    buffer.Disp[i] = field.MakeDisp(index, value);
                }
            }
            // チェックサム
            if (f.HasChecksum)
            {
                var i = f.ChecksumIndex;
                var field = f.Fields[i];
                var value = f.CalcChecksum(buffer.Buffer);
                buffer.Value[i] = value;
                f.UpdateBuffer(field, value, buffer.Buffer);
                // 表示名を作成
                var index = field.GetSelectsIndex(value);
                buffer.Disp[i] = field.MakeDisp(index, value);
            }

            return buffer;
        }
        private TxBackupBuffer MakeTxBackupBufferEmpty(string name, TxFrame f, int idx)
        {
            // Buffer作成
            return new TxBackupBuffer(name, f.Fields.Count, f.Length);
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
            if (field.Name is null)
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
                if (name is null)
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
                if (field.Name is null)
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

    partial class Json
    {

        public class Comm
        {
            // 送信設定
            [JsonPropertyName("tx")]
            public CommTx Tx { get; set; }

            // rx_autoresp
            // tx_autosend
        }

        public class CommTx
        {
            [JsonPropertyName("frames")]
            public IList<CommTxFrame> Frames { get; set; }
        }

        public class CommTxFrame
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("fields")]
            public IList<CommTxField> Fields { get; set; }

            [JsonPropertyName("backup_buffer_size")]
            public int BackupBufferSize { get; set; } = 0;

            [JsonPropertyName("backup_buffers")]
            public IList<CommTxBackupBuffer> BackupBuffers { get; set; }
        }

        public class CommTxField
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("multi_name")]
            public IList<CommTxFieldMultiName> MultiNames { get; set; }

            [JsonPropertyName("bit_size")]
            public int BitSize { get; set; } = 0;

            [JsonPropertyName("value")]
            public UInt64 Value { get; set; } = 0;

            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("unit")]
            public CommTxFieldUnit Unit { get; set; }

            [JsonPropertyName("dict")]
            public IList<CommTxFieldDict> Dict { get; set; }

            [JsonPropertyName("time")]
            public CommTxFieldTime Time { get; set; }

            [JsonPropertyName("script")]
            public CommTxFieldScript Script { get; set; }

            [JsonPropertyName("checksum")]
            public CommTxFieldChecksum Checksum { get; set; }
        }

        public class CommTxFieldMultiName
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("bit_size")]
            public int BitSize { get; set; } = 0;
        }

        public class CommTxFieldUnit
        {
            [JsonPropertyName("unit")]
            public string Unit { get; set; } = string.Empty;

            [JsonPropertyName("lsb")]
            public double Lsb { get; set; } = 0;

            [JsonPropertyName("disp_max")]
            public double DispMax { get; set; } = 0;

            [JsonPropertyName("disp_min")]
            public double DispMin { get; set; } = 0;

            [JsonPropertyName("value_min")]
            public UInt64 ValueMin { get; set; } = 0;

            [JsonPropertyName("format")]
            public string Format { get; set; } = string.Empty;
        }

        public class CommTxFieldDict
        {
            [JsonPropertyName("value")]
            public UInt64 Value { get; set; } = 0;

            [JsonPropertyName("disp")]
            public string Disp { get; set; } = string.Empty;
        }

        public class CommTxFieldTime
        {
            [JsonPropertyName("elapse")]
            public double Elapse { get; set; } = 0;

            [JsonPropertyName("begin")]
            public string Begin { get; set; } = string.Empty;

            [JsonPropertyName("end")]
            public string End { get; set; } = string.Empty;

            [JsonPropertyName("value_min")]
            public UInt64 ValueMin { get; set; } = 0;
        }

        public class CommTxFieldScript
        {
            [JsonPropertyName("mode")]
            public string Mode { get; set; } = string.Empty;

            [JsonPropertyName("count")]
            public int Count { get; set; } = 0;

            [JsonPropertyName("script")]
            public string Script { get; set; } = string.Empty;

        }

        public class CommTxFieldChecksum
        {
            [JsonPropertyName("begin")]
            public int Begin { get; set; } = 0;

            [JsonPropertyName("end")]
            public int End { get; set; } = 0;

            [JsonPropertyName("method")]
            public string Method { get; set; } = string.Empty;
        }

        public class CommTxBackupBuffer
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
            
            [JsonPropertyName("value")]
            public IList<UInt64> Values { get; set; }
        }

    }
}
