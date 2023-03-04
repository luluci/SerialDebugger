
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
        public bool DisplayId { get; set; }

        public ReactiveCollection<TxFrame> Tx { get; set; }
        public Dictionary<string, int> TxNameDict { get; set; }
        public bool TxInvertBit { get; set; }
        // 受信解析
        public ReactiveCollection<RxFrame> Rx { get; set; }
        public Dictionary<string, int> RxNameDict { get; set; }
        public class RxPatternInfo
        {
            public int FrameId { get; set; } = -1;
            public int PatternId { get; set; } = -1;
        }
        public Dictionary<string, RxPatternInfo> RxPatternDict { get; set; }
        public bool RxInvertBit { get; set; }
        public bool RxMultiMatch { get; set; }
        public bool RxHasScriptMatch { get; set; } = false;
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

        public void ClearDict()
        {
            TxNameDict.Clear();
            RxNameDict.Clear();
            RxPatternDict.Clear();
            AutoTxJobNameDict.Clear();
        }

        public async Task AnalyzeJsonAsync(Json.Comm json)
        {
            if (json is null)
            {
                return;
            }

            // 全体オプション
            DisplayId = json.DisplayId;

            if (!(json.Tx is null))
            {
                if (!(json.Tx.Frames is null))
                {
                    //
                    TxInvertBit = json.Tx.InvertBit;
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
            }

            // RxFrame作成
            // TxFrame作成後に実施する
            if (!(json.Rx is null))
            {
                // 
                RxInvertBit = json.Rx.InvertBit;
                RxMultiMatch = json.Rx.MultiMatch;
                // Frames解析
                if (!(json.Rx.Frames is null))
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
                    if (AutoTxJobNameDict.TryGetValue(j.Name, out int idx))
                    {
                        // Frame.NameはAutoTxからの参照に使うためユニークである必要がある。
                        throw new Exception($"auto_tx: jobs[{j.Id}]: 同じ名前({j.Name})が存在します。jobs.nameにはユニークな名前を設定してください。");
                    }
                    AutoTxJobNameDict.Add(j.Name, id);
                    //
                    AutoTx.Add(j);
                    id++;
                }
                // AutoTx整合性チェック
                ValidateAutoTx();
            }


            // AutoTx整合性チェック
            ValidateRx();
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
                    int char_id = 0;
                    bool prev_is_char = false;
                    foreach (var json_field in frame.Fields)
                    {
                        // Stringチェック
                        if (json_field.Type == "String")
                        {
                            if (json_field.String.Length > 0)
                            {
                                int char_pos = 0;
                                foreach (var ch in json_field.String)
                                {
                                    var field = await MakeFieldStringNodeAsync(i, json_field, ch, char_id);
                                    field.InnerFields[0].Name = $"{json_field.Name}[{char_pos}]";
                                    f.Fields.Add(field);
                                    i++;
                                    char_pos++;
                                }
                                char_id++;
                            }
                        }
                        else
                        {

                            // Field作成
                            var field = await MakeFieldAsync(i, json_field, char_id);
                            // Charチェック
                            if (field.InputType == Field.InputModeType.Char)
                            {
                                prev_is_char = true;
                            }
                            else
                            {
                                // Char -> その他 でID更新
                                if (prev_is_char)
                                {
                                    char_id++;
                                }
                                prev_is_char = false;
                            }
                            //
                            f.Fields.Add(field);
                            i++;
                        }
                    }
                    f.Build();

                    // PatternMatch作成
                    // patternsありのとき
                    i = 0;
                    foreach (var pattern in frame.Patterns)
                    {
                        var p = MakeRxPattern(i, pattern);
                        f.Patterns.Add(p);
                        i++;
                    }
                    f.BuildPattern();
                    if (f.HasScriptMatch)
                    {
                        RxHasScriptMatch = true;
                    }
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
                var p = new RxPattern(id, pattern.Name, pattern.Active, pattern.LogVisualize);

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
                case "Value":
                    type = RxMatchType.Value;
                    break;
                case "Any":
                    type = RxMatchType.Any;
                    break;
                case "Timeout":
                    type = RxMatchType.Timeout;
                    break;
                case "Script":
                    type = RxMatchType.Script;
                    break;
                case "Activate":
                    type = RxMatchType.Activate;
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

                case RxMatchType.Activate:
                    return MakeRxMatchActivate(id, match);

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

            if (!Object.ReferenceEquals(match.RxBegin, string.Empty) || !Object.ReferenceEquals(match.RxRecieved, string.Empty))
            {
                return RxMatchType.Script;
            }

            if (!Object.ReferenceEquals(match.AutoTxJobName, string.Empty) || !Object.ReferenceEquals(match.RxPatternName, string.Empty))
            {
                return RxMatchType.Activate;
            }

            if (match.Msec >= 0)
            {
                return RxMatchType.Timeout;
            }

            if (match.Value != Int64.MinValue)
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
            if (match.Value == Int64.MinValue)
            {
                throw new Exception($"matches[{id}]: 不正なvalue指定です: {match.Value}");
            }

            var m = new RxMatch
            {
                Type = RxMatchType.Value,
            };
            m.Value.Value = (Int64)match.Value;
            return m;
        }

        private RxMatch MakeRxMatchTimeout(int id, Json.CommRxMatch match)
        {
            if (match.Msec < 0)
            {
                throw new Exception($"matches[{id}]: 不正なtimeout指定です: {match.Msec}");
            }

            var m = new RxMatch
            {
                Type = RxMatchType.Timeout,
            };
            m.Msec.Value = match.Msec;
            return m;
        }

        private RxMatch MakeRxMatchScript(int id, Json.CommRxMatch match)
        {
            // Beginは必須ではない
            // Recievedは必須
            if (Object.ReferenceEquals(match.RxRecieved, string.Empty))
            {
                throw new Exception($"matches[{id}]: 不正なscript指定です: {match.RxRecieved}");
            }

            return new RxMatch
            {
                Type = RxMatchType.Script,
                RxBegin = match.RxBegin,
                RxRecieved = match.RxRecieved,
            };
        }

        private RxMatch MakeRxMatchActivate(int id, Json.CommRxMatch match)
        {
            // 後ろで定義されるものを指定できるように、該当設定が存在するかは後でチェックする
            if (!Object.ReferenceEquals(match.AutoTxJobName, string.Empty))
            {
                return new RxMatch
                {
                    Type = RxMatchType.ActivateAutoTx,
                    AutoTxJobName = match.AutoTxJobName,
                    AutoTxState = match.State,
                };
            }
            else if (!Object.ReferenceEquals(match.RxPatternName, string.Empty))
            {
                return new RxMatch
                {
                    Type = RxMatchType.ActivateRx,
                    RxPatternName = match.RxPatternName,
                    RxState = match.State,
                };
            }
            else
            {
                throw new Exception($"matches[{id}](Activate): 有効化対象となるauto_tx.job名(auto_tx_job)またはrx.pattern名(rx_pattern)を指定してください。");
            }
        }

        private void ValidateRx()
        {
            foreach (var frame in Rx)
            {
                foreach (var pattern in frame.Patterns)
                {
                    foreach (var match in pattern.Matches)
                    {
                        ValidateRxMatch(frame, pattern, match);
                    }
                }
            }
        }
        private void ValidateRxMatch(RxFrame frame, RxPattern pattern, RxMatch match)
        {
            switch (match.Type)
            {
                case RxMatchType.ActivateAutoTx:
                    // AutoTx参照チェック
                    if (AutoTxJobNameDict.TryGetValue(match.AutoTxJobName, out int index))
                    {
                        match.AutoTxJobIndex = index;
                        match.AutoTxJobRef = AutoTx[index];
                    }
                    else
                    {
                        throw new Exception($"rx: frame[{frame.Id}].pattern[{pattern.Id}].match[]: Activate AutoTx: 指定された名称({match.AutoTxJobName})のjobが存在しません。");
                    }
                    break;

                case RxMatchType.ActivateRx:
                    // Rx参照チェック
                    if (RxPatternDict.TryGetValue(match.RxPatternName, out RxPatternInfo info))
                    {
                        match.RxFrameIndex = info.FrameId;
                        match.RxPatternIndex = info.PatternId;
                        match.RxPatternRef = Rx[info.FrameId].Patterns[info.PatternId];
                    }
                    else
                    {
                        throw new Exception($"rx: frame[{frame.Id}].pattern[{pattern.Id}].match[]: Activate Rx: 指定された名称({match.RxPatternName})のpatternが存在しません。");
                    }
                    break;

                case RxMatchType.Activate:
                    throw new Exception($"rx: frame[{frame.Id}].pattern[{pattern.Id}].match[]: Logic error, Activate が出現するのはバグです。。");

                default:
                    break;
            }
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
                case AutoTxActionType.Jump:
                    bool set_jobname = false;
                    // JumpToチェック
                    if (AutoTxJobNameDict.TryGetValue(action.AutoTxJobName, out int jmpidx))
                    {
                        // 自分以外のジョブを指定したときインデックスを記憶しておく
                        if (job.Id != jmpidx)
                        {
                            action.AutoTxJobIndex = jmpidx;
                            set_jobname = true;
                        }
                        // else action.AutoTxJobIndex = -1;
                    }
                    else
                    {
                        // 未指定のときは自分を指定したと判定
                        // action.AutoTxJobIndex = -1;
                    }

                    if (Object.ReferenceEquals(action.Alias, string.Empty))
                    {
                        if (set_jobname)
                        {
                            action.Alias = $"JumpTo {action.AutoTxJobName}[{action.JumpTo}]";
                        }
                        else
                        {
                            action.Alias = $"JumpTo [{action.JumpTo}]";
                        }
                    }
                    break;

                case AutoTxActionType.ActivateAutoTx:
                    // Activateチェック
                    if (AutoTxJobNameDict.TryGetValue(action.AutoTxJobName, out int index))
                    {
                        // 指定された名称のjobが存在すればOK
                        // インデックスを記憶しておく
                        action.AutoTxJobIndex = index;
                    }
                    else
                    {
                        // 指定された名称のjobが存在しない場合は不正
                        throw new Exception($"auto_tx: jobs[{job.Id}]({job.Name}): actions[{action.Id}]: Activate: 指定された名称({action.AutoTxJobName})のjobが存在しません。");
                    }
                    break;

                case AutoTxActionType.ActivateRx:
                    // Activateチェック
                    if (RxPatternDict.TryGetValue(action.RxPatternName, out RxPatternInfo info))
                    {
                        // 指定された名称のjobが存在すればOK
                        // インデックスを記憶しておく
                        action.RxFrameIndex = info.FrameId;
                        action.RxPatternIndex = info.PatternId;
                    }
                    else
                    {
                        // 指定された名称のjobが存在しない場合は不正
                        throw new Exception($"auto_tx: jobs[{job.Id}]({job.Name}): actions[{action.Id}]: Activate: 指定された名称({action.RxPatternName})のRxPatternが存在しません。");
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
                var j = new AutoTxJob(id, job.Name, job.Alias, job.Active);
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
                    return MakeAutoTxActionRecv(id, action);

                case "Jump":
                    return MakeAutoTxActionJump(id, action);

                case "Script":
                    return MakeAutoTxActionScript(id, action);

                case "Activate":
                    return MakeAutoTxActionActivate(id, action);

                case "Log":
                    return MakeAutoTxActionLog(id, action);

                default:
                    throw new Exception($"actions[{id}]: Undefined AutoTx Action Type: {action.Type}");
            }
        }
        private AutoTxAction MakeAutoTxActionSend(int id, Json.CommAutoTxAction action)
        {
            if (Object.ReferenceEquals(action.TxFrameName,string.Empty))
            {
                throw new Exception($"actions[{id}](Send): 送信対象となるframe名(tx_frame)を指定してください。");
            }

            // action.TxFrameBuffIndex: 省略時は0指定とする

            if (TxNameDict.TryGetValue(action.TxFrameName, out int frame_idx))
            {
                var act = AutoTxAction.MakeSendAction(id, action.Alias, action.TxFrameName, frame_idx, action.TxFrameBuffIndex, action.TxFrameBuffOffset, action.TxFrameBuffLength, action.Immediate);

                return act;
            }
            else
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

            var act = AutoTxAction.MakeJumpAction(id, action.Alias, action.JumpTo, action.AutoTxJobName, action.Immediate);

            return act;
        }
        private AutoTxAction MakeAutoTxActionScript(int id, Json.CommAutoTxAction action)
        {
            if (Object.ReferenceEquals(action.AutoTxHandler, string.Empty) && Object.ReferenceEquals(action.RxHandler, string.Empty))
            {
                throw new Exception($"actions[{id}](Script): イベントハンドラ(auto_tx_handler/rx_handler)を指定してください。");
            }

            var act = AutoTxAction.MakeScriptAction(id, action.Alias, action.AutoTxHandler, action.RxHandler, action.Immediate);

            return act;
        }
        private AutoTxAction MakeAutoTxActionActivate(int id, Json.CommAutoTxAction action)
        {
            AutoTxAction act;

            if (!Object.ReferenceEquals(action.AutoTxJobName, string.Empty))
            {
                // 後ろで定義されるjobを指定できるように、AutoTxJobNameが存在するかは後でチェックする
                act = AutoTxAction.MakeActivateAutoTxAction(id, action.Alias, action.AutoTxJobName, action.State, action.Immediate);
            }
            else if (!Object.ReferenceEquals(action.RxPatternName, string.Empty))
            {
                // Rxはこの場でチェックできるが、AutoTxと同時にチェックする
                act = AutoTxAction.MakeActivateRxAction(id, action.Alias, action.RxPatternName, action.State, action.Immediate);
            }
            else
            {
                throw new Exception($"actions[{id}](Activate): 有効化対象となるauto_tx.job名(auto_tx_job)またはrx.pattern名(rx_pattern)を指定してください。");
            }
            
            return act;
        }
        private AutoTxAction MakeAutoTxActionRecv(int id, Json.CommAutoTxAction action)
        {
            // RxFrame/Patternとひもづけ
            var Recvs = new List<AutoTxRecvItem>();
            if (!(action.RxPatternNames is null))
            {
                foreach (var name in action.RxPatternNames)
                {
                    if (RxPatternDict.TryGetValue(name, out RxPatternInfo info))
                    {
                        Recvs.Add(new AutoTxRecvItem
                        {
                            PatternName = name,
                            FrameId = info.FrameId,
                            PatternId = info.PatternId,
                        });
                    }
                    else
                    {
                        throw new Exception($"actions[{id}](Recv): 指定された受信パターン({name})が存在しません。");
                    }
                }
            }

            var act = AutoTxAction.MakeRecvAction(id, action.Alias, Recvs, action.Immediate);

            return act;
        }
        private AutoTxAction MakeAutoTxActionLog(int id, Json.CommAutoTxAction action)
        {
            // check
            if (Object.ReferenceEquals(action.Log, string.Empty))
            {
                throw new Exception($"actions[{id}](Log): ログメッセージ(log)を指定してください。");
            }
            // Action作成
            var act = AutoTxAction.MakeLogAction(id, action.Alias, action.Log, action.Immediate);

            return act;
        }


        /// <summary>
        /// JsonTxFrameからTxFrameを作成する
        /// </summary>
        /// <param name="frame"></param>
        private async Task<TxFrame> MakeTxFrameAsync(int id, Json.CommTxFrame frame)
        {
            // 必須設定チェック
            // Nameチェック
            if (Object.ReferenceEquals(frame.Name, string.Empty))
            {
                throw new Exception($"tx: frames[{id}]: nameを指定してください。");
            }
            // Bufferサイズ決定
            int buffer_size = 1 + MakeTxBufferSize(frame);

            try
            {
                // TxFrame作成
                var f = new TxFrame(id, frame.Name, buffer_size, frame.AsAscii);
                // f.Build()でバッファもまとめて作成するため、必要なバッファインスタンスを先に作成する
                MakeTxBuffers(frame, f);
                // Field作成
                if (!(frame.Fields is null))
                {
                    int i = 0;
                    int char_id = 0;
                    bool prev_is_char = false;
                    foreach (var json_field in frame.Fields)
                    {

                        // Stringチェック
                        if (json_field.Type == "String")
                        {
                            if (json_field.String.Length > 0)
                            {
                                int char_pos = 0;
                                foreach (var ch in json_field.String)
                                {
                                    var field = await MakeFieldStringNodeAsync(i, json_field, ch, char_id);
                                    field.InnerFields[0].Name = $"{json_field.Name}[{char_pos}]";
                                    f.Fields.Add(field);
                                    i++;
                                    char_pos++;
                                }
                                char_id++;
                            }
                        }
                        else
                        {

                            // Field作成
                            var field = await MakeFieldAsync(i, json_field, char_id);
                            // Charチェック
                            if (field.InputType == Field.InputModeType.Char)
                            {
                                prev_is_char = true;
                            }
                            else
                            {
                                // Char -> その他 でID更新
                                if (prev_is_char)
                                {
                                    char_id++;
                                }
                                prev_is_char = false;
                            }
                            //
                            f.Fields.Add(field);
                            i++;
                        }
                    }
                    f.Build();
                }
                else
                {
                    // Field定義が空のときのケア
                    foreach (var buff in f.Buffers)
                    {
                        buff.Data = buff.Buffer.ToArray();
                    }
                }
                // TxFrame作成後にBackupBuffer作成
                InitTxBuffers(frame, f);

                return f;
            }
            catch (Exception ex)
            {
                throw new Exception($"tx: frames[{id}]({frame.Name}): {ex.Message}");
            }
        }

        private int MakeTxBufferSize(Json.CommTxFrame frame)
        {
            // バッファサイズ作成
            int size = 0;
            //
            if (frame.BackupBufferSize > 0)
            {
                size = frame.BackupBufferSize;
            }
            if (!(frame.BackupBuffers is null))
            {
                if (size < frame.BackupBuffers.Count)
                {
                    size = frame.BackupBuffers.Count;
                }
            }
            //
            return size;
        }

        private void MakeTxBuffers(Json.CommTxFrame frame, TxFrame f)
        {
            // BackupBuffers定義数を取得
            int buffer_def_size = 0;
            if (!(frame.BackupBuffers is null))
            {
                buffer_def_size = frame.BackupBuffers.Count;
            }
            // 必須の1個より多くのバッファを持つ場合、インスタンスを作成しておく
            string name;
            for (int i = 1, bk_idx = 0; i < f.BufferSize; i++, bk_idx++)
            {
                // バッファ名称作成
                if (bk_idx < buffer_def_size)
                {
                    // BackupBuffers定義から作成
                    if (Object.ReferenceEquals(frame.BackupBuffers[bk_idx].Name, string.Empty))
                    {
                        name = $"buffer[{bk_idx}]";
                    }
                    else
                    {
                        name = frame.BackupBuffers[bk_idx].Name;
                    }
                }
                else
                {
                    // 定型名称を作成
                    name = $"buffer[{bk_idx}]";
                }

                f.Buffers.Add(new TxFieldBuffer(i, name, f));
            }
        }

        private void InitTxBuffers(Json.CommTxFrame frame, TxFrame f)
        {

            // 送信データバックアップバッファ初期化
            if (!(frame.BackupBuffers is null))
            {
                // バックアップバッファは[1]から開始
                // Jsonバックアップバッファ設定は[0]から開始
                for (int i = 0; i < frame.BackupBuffers.Count; i++)
                {
                    InitTxBuffer(frame.BackupBuffers[i], f, i+1);
                }
            }
        }

        private void InitTxBuffer(Json.CommTxBackupBuffer json, TxFrame f, int idx)
        {
            // 初期値指定があれば反映する
            var fb = f.Buffers[idx];
            if (!(json.Values is null))
            {
                // json設定値反映
                for (int i = 0; i < json.Values.Count && i < f.Fields.Count; i++)
                {
                    // valueをバッファに反映
                    var value = json.Values[i];
                    var fv = fb.FieldValues[i];
                    fv.SetValue(value);
                    f.UpdateBuffer(fb, fv);
                }
            }
            else if (!Object.ReferenceEquals(json.ValueAscii, string.Empty))
            {
                // json設定値反映
                for (int i = 0; i < json.ValueAscii.Length && i < f.Fields.Count; i++)
                {
                    // valueをバッファに反映
                    var value = json.ValueAscii[i];
                    var fv = fb.FieldValues[i];
                    fv.SetValue(value);
                    f.UpdateBuffer(fb, fv);
                }
            }
            else
            {
                return;
            }

            // チェックサム
            if (f.HasChecksum)
            {
                var i = f.ChecksumIndex;
                var field = f.Fields[i];
                f.UpdateChecksum(fb);
            }
        }

        private async Task<Field> MakeFieldAsync(int id, Json.CommField field, int char_id)
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
                case "Char":
                    return await MakeFieldCharAsync(id, field, char_id);
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
            var selecter = new (Int64, string)[dict.Count];
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
            if (time.Elapse <= 0.0)
            {
                throw new Exception($"fields[{id}]({field.Name}): Time: elapse({time.Elapse})は1以上を指定してください。");
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

        private async Task<Field> MakeFieldCharAsync(int id, Json.CommField field, int char_id)
        {
            // Valueチェック
            if (!Object.ReferenceEquals(field.Char, string.Empty) && field.Char.Length > 0)
            {
                field.Value = field.Char[0];
            }
            //
            if (!(field.MultiNames is null))
            {
                throw new Exception($"fields[{id}]({field.Name}): type:CharではMultiNamesを指定できません");
            }
            // BitSizeは固定
            field.BitSize = 8;
            // Selecter
            var selecter = Field.MakeSelecterChar(char_id);
            // Field生成
            return await MakeFieldImplAsync(id, field, Field.InputModeType.Char, selecter);
        }

        private async Task<Field> MakeFieldStringNodeAsync(int id, Json.CommField field, char ch, int char_id)
        {
            //
            if (!(field.MultiNames is null))
            {
                throw new Exception($"fields[{id}]({field.Name}): type:CharではMultiNamesを指定できません");
            }
            // Value
            field.Value = ch;
            // BitSizeは固定
            field.BitSize = 8;
            // Selecter
            var selecter = Field.MakeSelecterChar(char_id);
            // Field生成
            return await MakeFieldImplAsync(id, field, Field.InputModeType.Char, selecter);
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
