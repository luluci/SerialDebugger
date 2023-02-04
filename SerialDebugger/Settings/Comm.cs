
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

using SerialDebugger.Comm;
using System.Windows;

namespace SerialDebugger.Settings
{
    using Utility;

    class Comm : BindableBase, IDisposable
    {
        public ReactiveCollection<TxFrame> Tx { get; set; }
        public Dictionary<string, int> TxNameDict { get; set; }
        // 受信解析
        public ReactiveCollection<RxFrame> Rx { get; set; }
        public Dictionary<string, int> RxNameDict { get; set; }
        public class RxPatternInfo
        {
            public int FrameId { get; set; } = -1;
            public int PatternId { get; set; } = -1;
        }
        public Dictionary<string, RxPatternInfo> RxPatternDict { get; set; }
        // 自動送信
        public ReactiveCollection<AutoTxJob> AutoTx { get; set; }
        public Dictionary<string, int> AutoTxJobNameDict { get; set; }

        public Comm()
        {
            Tx = new ReactiveCollection<TxFrame>();
            Tx.AddTo(Disposables);
            TxNameDict = new Dictionary<string, int>();
            Rx = new ReactiveCollection<RxFrame>();
            Rx.AddTo(Disposables);
            RxNameDict = new Dictionary<string, int>();
            RxPatternDict = new Dictionary<string, RxPatternInfo>();
            AutoTx = new ReactiveCollection<AutoTxJob>();
            AutoTx.AddTo(Disposables);
            AutoTxJobNameDict = new Dictionary<string, int>();
        }

        public async Task AnalyzeJsonAsync(Json.Comm json)
        {
            if (json is null)
            {
                return;
            }

            if (!(json.Tx is null) && !(json.Tx.Frames is null))
            {
                // TxFrameコレクション作成
                int i = 0;
                foreach (var frame in json.Tx.Frames)
                {
                    var f = await MakeTxFrameAsync(i, frame);

                    if (TxNameDict.TryGetValue(f.Name, out int value))
                    {
                        // Frame.NameはAutoTxからの参照に使うためユニークである必要がある。
                        throw new Exception($"tx: frames[{i}]: 同じ名前({f.Name})が存在します。frame.nameにはユニークな名前を設定してください。");
                    }
                    TxNameDict.Add(f.Name, f.Id);

                    Tx.Add(f);
                    i++;
                }

            }

            // RxFrame作成
            // TxFrame作成後に実施する
            if (!(json.Rx is null) && !(json.Rx.Frames is null))
            {
                int i = 0;
                foreach (var frame in json.Rx.Frames)
                {
                    var f = await MakeRxFrameAsync(i, frame);

                    // NameDict作成
                    if (RxNameDict.TryGetValue(f.Name, out int value))
                    {
                        // Frame.NameはAutoTxからの参照に使うためユニークである必要がある。
                        throw new Exception($"rx: frames[{f.Id}]: 同じ名前({f.Name})が存在します。frame.nameにはユニークな名前を設定してください。");
                    }
                    RxNameDict.Add(f.Name, f.Id);
                    // Pattern参照情報作成
                    foreach (var ptn in f.Patterns)
                    {
                        if (RxPatternDict.TryGetValue(ptn.Name, out RxPatternInfo inf))
                        {
                            // Pattern.NameはAutoTxからの参照に使うためユニークである必要がある。
                            throw new Exception($"rx: frames[{f.Id}]: patterns[{ptn.Id}]: 同じ名前({ptn.Name})が存在します。frame.patterns.nameにはユニークな名前を設定してください。");
                        }
                        RxPatternDict.Add(ptn.Name, new RxPatternInfo { FrameId = f.Id, PatternId = ptn.Id });
                    }

                    Rx.Add(f);
                    i++;
                }
            }

            // AutoTx作成
            // TxFrame, RxFrame作成後に実施する
            if (!(json.AutoTx is null) && !(json.AutoTx.Jobs is null))
            {
                int id = 0;
                foreach (var job in json.AutoTx.Jobs)
                {
                    // Job作成
                    var j = MakeAutoTxJob(id, job);
                    // Job名重複チェック
                    if (AutoTxJobNameDict.TryGetValue(j.Name.Value, out int idx))
                    {
                        // Frame.NameはAutoTxからの参照に使うためユニークである必要がある。
                        throw new Exception($"auto_tx: jobs[{j.Id}]: 同じ名前({j.Name.Value})が存在します。jobs.nameにはユニークな名前を設定してください。");
                    }
                    AutoTxJobNameDict.Add(j.Name.Value, id);
                    //
                    AutoTx.Add(j);
                    id++;
                }
                // AutoTx整合性チェック
                ValidateAutoTx();
            }
        }


        private async Task<RxFrame> MakeRxFrameAsync(int id, Json.CommRxFrame frame)
        {
            if (Object.ReferenceEquals(frame.Name, string.Empty))
            {
                throw new Exception($"rx: frames[{id}]: Nameを指定してください。");
            }

            try
            {
                // RxFrame作成
                var f = new RxFrame(id, frame.Name);
                if (!(frame.Fields is null))
                {
                    int i = 0;
                    foreach (var field in frame.Fields)
                    {
                        f.Fields.Add(await MakeFieldAsync(i, field));
                        i++;
                    }
                    f.Build();

                    // PatternMatch作成
                    // Fieldsありのとき
                    i = 0;
                    foreach (var pattern in frame.Patterns)
                    {
                        var p = MakeRxPattern(i, pattern);
                        f.Patterns.Add(p);
                        i++;
                    }
                    f.BuildPattern();
                }

                return f;
            }
            catch (Exception ex)
            {
                throw new Exception($"rx: frames[{id}]({frame.Name}): {ex.Message}");
            }
        }

        private RxPattern MakeRxPattern(int id, Json.CommRxPattern pattern)
        {
            if (Object.ReferenceEquals(pattern.Name, string.Empty))
            {
                throw new Exception($"patterns[{id}]: nameを指定してください。");
            }

            try
            {
                var p = new RxPattern(id, pattern.Name, pattern.Active);

                // Match作成
                if (!(pattern.Matches is null))
                {
                    int m_id = 0;
                    foreach (var match in pattern.Matches)
                    {
                        var m = MakeRxMatch(m_id, match);
                        p.Matches.Add(m);

                        m_id++;
                    }
                }

                return p;
            }
            catch (Exception ex)
            {
                throw new Exception($"patterns[{id}]({pattern.Name}): {ex.Message}");
            }
        }

        private RxMatch MakeRxMatch(int id, Json.CommRxMatch match)
        {
            // Type判定
            RxMatchType type;
            switch (match.Type)
            {
                case "value":
                    type = RxMatchType.Value;
                    break;
                case "any":
                    type = RxMatchType.Any;
                    break;
                case "timeout":
                    type = RxMatchType.Timeout;
                    break;
                case "script":
                    type = RxMatchType.Script;
                    break;
                default:
                    type = MakeRxMatchType(id, match);
                    break;
            }
            //
            switch (type)
            {
                case RxMatchType.Any:
                    return MakeRxMatchAny(id);

                case RxMatchType.Value:
                    return MakeRxMatchValue(id, match);

                case RxMatchType.Timeout:
                    return MakeRxMatchTimeout(id, match);

                case RxMatchType.Script:
                    return MakeRxMatchScript(id, match);

                default:
                    throw new Exception($"matches[{id}]: 不正なtype指定です: {type}");
            }
        }

        private RxMatchType MakeRxMatchType(int id, Json.CommRxMatch match)
        {
            // 空文字以外=何か入力してる場合は想定外の文字列なのでエラー
            if (!Object.ReferenceEquals(match.Type, string.Empty))
            {
                throw new Exception($"matches[{id}]: 不正なtype指定です: {match.Type}");
            }

            if (!Object.ReferenceEquals(match.Script, string.Empty))
            {
                return RxMatchType.Script;
            }

            if (match.Msec >= 0)
            {
                return RxMatchType.Timeout;
            }

            if (match.Value >= 0)
            {
                return RxMatchType.Value;
            }

            return RxMatchType.Any;
        }

        private RxMatch MakeRxMatchAny(int id)
        {
            return new RxMatch
            {
                Type = RxMatchType.Any,
            };
        }

        private RxMatch MakeRxMatchValue(int id, Json.CommRxMatch match)
        {
            if (match.Value < 0)
            {
                throw new Exception($"matches[{id}]: 不正なvalue指定です: {match.Value}");
            }

            return new RxMatch
            {
                Type = RxMatchType.Value,
                Value = (UInt64)match.Value,
            };
        }

        private RxMatch MakeRxMatchTimeout(int id, Json.CommRxMatch match)
        {
            if (match.Msec < 0)
            {
                throw new Exception($"matches[{id}]: 不正なtimeout指定です: {match.Msec}");
            }

            return new RxMatch
            {
                Type = RxMatchType.Timeout,
                Msec = match.Msec,
            };
        }

        private RxMatch MakeRxMatchScript(int id, Json.CommRxMatch match)
        {
            if (Object.ReferenceEquals(match.Script, string.Empty))
            {
                throw new Exception($"matches[{id}]: 不正なscript指定です: {match.Script}");
            }

            return new RxMatch
            {
                Type = RxMatchType.Script,
                Script = match.Script,
            };
        }


        private void ValidateAutoTx()
        {
            foreach (var job in AutoTx)
            {
                foreach (var action in job.Actions)
                {
                    ValidateAutoTxAction(job, action);
                }
            }
        }
        private void ValidateAutoTxAction(AutoTxJob job, AutoTxAction action)
        {
            switch (action.Type)
            {
                case AutoTxActionType.ActivateAutoTx:
                    // Activateチェック
                    if (AutoTxJobNameDict.TryGetValue(action.AutoTxJobName.Value, out int index))
                    {
                        // 指定された名称のjobが存在すればOK
                        // インデックスを記憶しておく
                        action.AutoTxJobIndex.Value = index;
                    }
                    else
                    {
                        // 指定された名称のjobが存在しない場合は不正
                        throw new Exception($"auto_tx: jobs[{job.Id}]({job.Name}): actions[{action.Id}]: Activate: 指定された名称({action.AutoTxJobName.Value})のjobが存在しません。");
                    }
                    break;

                default:
                    // チェック無し
                    break;
            }
        }

        private AutoTxJob MakeAutoTxJob(int id, Json.CommAutoTxJob job)
        {
            try
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
            catch (Exception ex)
            {
                throw new Exception($"auto_tx: jobs[{id}]({job.Name}): {ex.Message}");
            }
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
                    throw new Exception($"actions[{id}]: {action.Type} is unimplemented!");

                case "Jump":
                    return MakeAutoTxActionJump(id, action);

                case "Script":
                    throw new Exception($"actions[{id}]: {action.Type} is unimplemented!");

                case "Activate":
                    return MakeAutoTxActionActivate(id, action);

                default:
                    throw new Exception($"actions[{id}]: Undefined AutoTx Action Type: {action.Type}");
            }
        }
        private AutoTxAction MakeAutoTxActionSend(int id, Json.CommAutoTxAction action)
        {
            if (Object.ReferenceEquals(action.TxFrameName,string.Empty))
            {
                throw new Exception($"actions[{id}](Send): 送信対象となるframe名(tx_frame_name)を指定してください。");
            }
            if (action.TxFrameBuffIndex == -1)
            {
                throw new Exception($"actions[{id}](Send): 送信対象となるframeバッファ(tx_frame_buff_index)を指定してください。");
            }

            try
            {
                var frame_idx = TxNameDict[action.TxFrameName];
                var act = AutoTxAction.MakeSendAction(id, action.Alias, action.TxFrameName, frame_idx, action.TxFrameBuffIndex, action.TxFrameBuffOffset, action.TxFrameBuffLength, action.Immediate);

                return act;
            }
            catch
            {
                // FrameNameが存在しないとき
                throw new Exception($"actions[{id}](Send): 指定されたtx.frame({action.TxFrameName})が存在しません。");
            }
        }
        private AutoTxAction MakeAutoTxActionWait(int id, Json.CommAutoTxAction action)
        {
            if (action.WaitTime == -1)
            {
                throw new Exception($"actions[{id}](Wait): 待機時間(WaitTime)を指定してください。");
            }

            var act = AutoTxAction.MakeWaitAction(id, action.Alias, action.WaitTime, action.Immediate);

            return act;
        }
        private AutoTxAction MakeAutoTxActionJump(int id, Json.CommAutoTxAction action)
        {
            if (action.JumpTo == -1)
            {
                throw new Exception($"actions[{id}](Jump): JumpToを指定してください。");
            }

            var act = AutoTxAction.MakeJumpAction(id, action.Alias, action.JumpTo, action.Immediate);

            return act;
        }
        private AutoTxAction MakeAutoTxActionActivate(int id, Json.CommAutoTxAction action)
        {
            if (Object.ReferenceEquals(action.AutoTxJobName, string.Empty))
            {
                throw new Exception($"actions[{id}](Activate): 有効化対象となるauto_tx.job名(auto_tx_job_name)を指定してください。");
            }

            // 後ろで定義されるjobを指定できるように、AutoTxJobNameが存在するかは後でチェックする
            var act = AutoTxAction.MakeActivateAutoTxAction(id, action.Alias, action.AutoTxJobName, action.Immediate);

            return act;
        }


        /// <summary>
        /// JsonTxFrameからTxFrameを作成する
        /// </summary>
        /// <param name="frame"></param>
        private async Task<TxFrame> MakeTxFrameAsync(int id, Json.CommTxFrame frame)
        {
            if (Object.ReferenceEquals(frame.Name, string.Empty))
            {
                throw new Exception($"tx: frames[{id}]: nameを指定してください。");
            }

            try
            {
                // TxFrame作成
                var f = new TxFrame(id, frame.Name);
                if (!(frame.Fields is null))
                {
                    int i = 0;
                    foreach (var field in frame.Fields)
                    {
                        f.Fields.Add(await MakeFieldAsync(i, field));
                        i++;
                    }
                    f.Build();
                }
                // TxFrame作成後にBackupBuffer作成
                MakeTxBackupBuffers(frame, f);

                return f;
            }
            catch (Exception ex)
            {
                throw new Exception($"tx: frames[{id}]({frame.Name}): {ex.Message}");
            }
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

        private async Task<Field> MakeFieldAsync(int id, Json.CommField field)
        {
            switch (field.Type)
            {
                case "Checksum":
                    return await MakeFieldChecksumAsync(id, field);
                case "Dict":
                    return await MakeFieldDictAsync(id, field);
                case "Unit":
                    return await MakeFieldUnitAsync(id, field);
                case "Time":
                    return await MakeFieldTimeAsync(id, field);
                case "Script":
                    return await MakeFieldScriptAsync(id, field);
                case "Edit":
                    return await MakeFieldImplAsync(id, field, Field.InputModeType.Edit, null);
                case "Fix":
                default:
                    return await MakeFieldImplAsync(id, field, Field.InputModeType.Fix, null);
            }
        }

        private async Task<Field> MakeFieldChecksumAsync(int id, Json.CommField field)
        {
            // Checksumチェック
            if (field.Checksum is null)
            {
                throw new Exception($"fields[{id}]({field.Name}): type:ChecksumではChecksumオブジェクトを指定してください。");
            }
            // Nameチェック
            // Checksumの場合はmulti_nameは許可しない
            if (Object.ReferenceEquals(field.Name, string.Empty))
            {
                throw new Exception($"fields[{id}]({field.Name}): Checksumノードはname,bit_sizeを指定してください。");
            }
            // Checksum計算方法
            var method = new Field.ChecksumMethod();
            switch (field.Checksum.Method)
            {
                case "2compl":
                    // 2の補数
                    method = Field.ChecksumMethod.cmpl_2;
                    break;
                case "1compl":
                    // 1の補数
                    method = Field.ChecksumMethod.cmpl_1;
                    break;
                case "Sum":
                default:
                    // 総和
                    method = Field.ChecksumMethod.None;
                    break;
            }
            // Field生成
            var result = new Field(id, new Field.ChecksumNode
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

        private async Task<Field> MakeFieldDictAsync(int id, Json.CommField field)
        {
            // nullチェック
            var dict = field.Dict;
            if (dict is null)
            {
                throw new Exception($"fields[{id}]({field.Name}): type:DictではDictオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = new (UInt64, string)[dict.Count];
            int i = 0;
            foreach (var pair in dict)
            {
                selecter[i] = (pair.Value, pair.Disp);
                i++;
            }
            // Field生成
            return await MakeFieldImplAsync(id, field, Field.InputModeType.Dict, Field.MakeSelecterDict(selecter));
        }

        private async Task<Field> MakeFieldUnitAsync(int id, Json.CommField field)
        {
            // nullチェック
            var unit = field.Unit;
            if (unit is null)
            {
                throw new Exception($"fields[{id}]({field.Name}): type:UnitではUnitオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = Field.MakeSelecterUnit(unit.Unit, unit.Lsb, unit.DispMax, unit.DispMin, unit.ValueMin, unit.Format);
            // Field生成
            return await MakeFieldImplAsync(id, field, Field.InputModeType.Unit, selecter);
        }

        private async Task<Field> MakeFieldTimeAsync(int id, Json.CommField field)
        {
            // nullチェック
            var time = field.Time;
            if (time is null)
            {
                throw new Exception($"fields[{id}]({field.Name}): type:TimeではTimeオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = Field.MakeSelecterTime(time.Elapse, time.Begin, time.End, time.ValueMin);
            // Field生成
            return await MakeFieldImplAsync(id, field, Field.InputModeType.Time, selecter);
        }

        private async Task<Field> MakeFieldScriptAsync(int id, Json.CommField field)
        {
            // nullチェック
            var script = field.Script;
            if (script is null)
            {
                throw new Exception($"fields[{id}]({field.Name}): type:ScriptではScriptオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = Field.MakeSelecterScript(script.Mode, script.Count, script.Script);
            // Field生成
            return await MakeFieldImplAsync(id, field, Field.InputModeType.Script, selecter);
        }

        private async Task<Field> MakeFieldImplAsync(int id, Json.CommField field, Field.InputModeType type, Field.Selecter selecter)
        {
            Field result;
            // name, multi_name選択
            if (!(field.MultiNames is null))
            {
                // multi_name優先
                int i = 0;
                var multi_name = new Field.InnerField[field.MultiNames.Count];
                foreach (var pair in field.MultiNames)
                {
                    multi_name[i] = new Field.InnerField(pair.Name, pair.BitSize);
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
                // Field生成
                result = new Field(id, name, multi_name, field.Value, field.Base, type, selecter);
            }
            else
            {
                // multi_nameが指定されていないときはnameとbitsizeを使用
                // データチェック, nameとbitsizeの両方が有効である必要がある
                if (Object.ReferenceEquals(field.Name, string.Empty))
                {
                    throw new Exception($"fields[{id}]: nameかmulti_nameのどちらかを指定してください。");
                }
                if (field.BitSize <= 0)
                {
                    throw new Exception($"fields[{id}]({field.Name}): 有効なbit_sizeを指定してください。");
                }
                var multi_name = new Field.InnerField[1];
                multi_name[0] = new Field.InnerField(field.Name, field.BitSize);
                // Field生成
                result = new Field(id, field.Name, multi_name, field.Value, field.Base, type, selecter);
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
