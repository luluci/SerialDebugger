using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.IO.Ports;

namespace SerialDebugger.Comm
{
    using System.Windows;
    using Utility;
    using Logger = Log.Log;
    
    public class AutoTxJob : BindableBase, IDisposable
    {
        public int Id { get; }
        public string Name { get; private set; }
        public string Alias { get; private set; }
        public ReactivePropertySlim<bool> IsActive { get; set; }
        public bool IsEditable { get; set; }
        public (DateTime, AutoTxActionType, object)[] LogBuffer;
        public int LogBufferHead;
        public int LogBufferTail;
        public Utility.CycleTimer WaitTimer;

        public ReactiveCollection<AutoTxAction> Actions { get; set; }

        public ReactiveCollection<int> ActionIdList { get; set; }

        public int ActiveActionIndex { get; set; }

        public ReactiveCommand OnClickReset { get; set; }

        // GUI: 
        public UIElement UiElemRef;

        public AutoTxJob(int id, string name, string alias, bool active, bool editable)
        {
            Id = id;
            IsEditable = editable;
            WaitTimer = new Utility.CycleTimer();

            Name = name;
            IsActive = new ReactivePropertySlim<bool>(active);
            IsActive.Subscribe((x) =>
            {
                if (x)
                {
                    WaitTimer.Start();
                }
            });
            IsActive.AddTo(Disposables);
            Actions = new ReactiveCollection<AutoTxAction>();
            Actions.AddTo(Disposables);

            ActionIdList = new ReactiveCollection<int>();
            ActionIdList.AddTo(Disposables);

            Alias = alias;
            if (object.ReferenceEquals(Alias, string.Empty))
            {
                Alias = Name;
            }

            OnClickReset = new ReactiveCommand();
            OnClickReset
                .Subscribe(x =>
                {
                    Reset();
                })
                .AddTo(Disposables);
        }

        public void Add(AutoTxAction action)
        {
            Actions.Add(action);
        }

        public void Build(IList<Comm.TxFrame> TxFrames)
        {
            // 整合性チェック
            foreach (var action in Actions)
            {
                CheckAction(TxFrames, action);
            }
            // 初期化
            ActiveActionIndex = 0;

            if (Actions.Count > 0)
            {
                Actions[0].IsActive.Value = true;
            }

            // ログバッファ初期化
            LogBuffer = new (DateTime, AutoTxActionType, object)[Actions.Count];
            LogBufferTail = 0;
            LogBufferHead = 0;
        }

        private void CheckAction(IList<Comm.TxFrame> TxFrames, AutoTxAction action)
        {
            switch (action.Type)
            {
                case AutoTxActionType.Send:
                    // action.TxFrameIndex 上流でチェック済み
                    var frame = TxFrames[action.TxFrameIndex];
                    if (action.TxFrameBuffIndex < 0)
                    {
                        throw new Exception($"AutoTx: SendAction: TxFrameBuffIndex: 0より大きな値を指定してください。({action.TxFrameBuffIndex})");
                    }
                    else if (action.TxFrameBuffIndex >= frame.BufferSize)
                    {
                        throw new Exception($"AutoTx: SendAction: TxFrameBuffIndex: BackupBufferの定義数より大きな値が指定されました。({action.TxFrameBuffIndex})");
                    }
                    var fb = frame.Buffers[action.TxFrameBuffIndex];
                    var send_fix = 0;
                    // Offset
                    if (action.TxFrameOffset == -1)
                    {
                        action.TxFrameOffset = 0;
                        send_fix++;
                    }
                    // Length
                    if (action.TxFrameLength == -1)
                    {
                        action.TxFrameLength = frame.Length;
                        send_fix++;

                        // 省略時はLengthをOffsetに合わせて自動調整
                        if ((action.TxFrameOffset + action.TxFrameLength) > frame.Length)
                        {
                            action.TxFrameLength = frame.Length - action.TxFrameOffset;
                        }
                    }
                    if (action.TxFrameOffset >= frame.Length || (action.TxFrameOffset + action.TxFrameLength) > frame.Length)
                    {
                        throw new Exception("AutoTx: SendAction: TxFrameOffset/TxFrameLength.");
                    }
                    // Aliasチェック
                    if (Object.ReferenceEquals(action.Alias, string.Empty))
                    {
                        if (send_fix == 2)
                        {
                            action.Alias = $"Send [{fb.Name}]";
                        }
                        else
                        {
                            // Offset/Lengthがデフォルト値でなければ表示作成
                            action.Alias = $"Send [{fb.Name}[{action.TxFrameOffset}~{action.TxFrameLength}]]";
                        }
                    }
                    // ASCIIチェック
                    if (frame.AsAscii)
                    {
                        action.TxFrameOffset *= 2;
                        action.TxFrameLength *= 2;
                    }
                    break;

                case AutoTxActionType.Wait:
                    break;

                case AutoTxActionType.Recv:
                    break;

                case AutoTxActionType.AnyRecv:
                    break;

                case AutoTxActionType.Jump:
                    if (action.JumpTo.Value >= Actions.Count)
                    {
                        throw new Exception("AutoTx: SendAction: JumpTo.");
                    }
                    break;

                case AutoTxActionType.Script:
                    break;

                case AutoTxActionType.ActivateAutoTx:
                    break;

                case AutoTxActionType.ActivateRx:
                    break;

                case AutoTxActionType.Log:
                    break;

                default:
                    throw new Exception("undefined type.");
            }
        }

        public void Reset()
        {
            // Action実行状況をリセット
            Actions[ActiveActionIndex].IsActive.Value = false;
            ActiveActionIndex = 0;
            Actions[ActiveActionIndex].IsActive.Value = true;
            // Waitタイマリスタート
            WaitTimer.Start();
        }
        
        public async Task ExecCycle(Serial.Protocol protocol)
        {
            bool check = true;
            while (check)
            {
                check = false;

                switch (Actions[ActiveActionIndex].Type)
                {
                    case AutoTxActionType.Send:
                        ActSend(protocol);
                        // 次のActionに移行
                        check = NextAction();
                        break;

                    case AutoTxActionType.Wait:
                        // 時間経過判定
                        if (ActWait())
                        {
                            // 時間経過していたら次のActionに移行
                            check = NextAction();
                        }
                        break;

                    case AutoTxActionType.Jump:
                        if (ActJump(protocol.AutoTxJobs))
                        {
                            // 自分自身のJumpToのとき、対象アクションが次に移動しているので実行する。
                            check = true;
                        }
                        else
                        {
                            // 自分以外のJumpToのとき、次のActionに移行
                            check = NextAction();
                        }
                        break;

                    case AutoTxActionType.Script:
                        if (await ActScriptCycle())
                        {
                            check = NextAction();
                        }
                        break;

                    case AutoTxActionType.ActivateAutoTx:
                        ActActivateAutoTx(protocol.AutoTxJobs);
                        // 次のActionに移行
                        check = NextAction();
                        break;

                    case AutoTxActionType.ActivateRx:
                        ActActivateRx(protocol.RxFrames);
                        // 次のActionに移行
                        check = NextAction();
                        break;

                    case AutoTxActionType.Recv:
                        // Recvは周期判定では変化しない
                        break;

                    case AutoTxActionType.AnyRecv:
                        // Recvは周期判定では変化しない
                        break;

                    case AutoTxActionType.Log:
                        ActLog();
                        // 次のActionに移行
                        check = NextAction();
                        break;

                    default:
                        throw new Exception("undefined type.");
                }
            }
        }

        public async Task ExecRecv(Serial.Protocol protocol)
        {
            switch (Actions[ActiveActionIndex].Type)
            {
                case AutoTxActionType.Recv:
                    var result = ActRecv(protocol);
                    if (result)
                    {
                        // 処理終了からの時間を計測
                        WaitTimer.StartBy(protocol.Result.TimeStamp);
                        // 受信判定一致していたら次のActionに移行
                        if (NextAction())
                        {
                            // 通常Execを実施
                            await ExecCycle(protocol);
                        }
                    }
                    break;

                case AutoTxActionType.AnyRecv:
                    // 受信解析成功または受信タイムアウト(1バイト以上受信している)によりこのパスに到達
                    // AnyRecvはあらゆる受信を受理するのでパス到達で条件成立
                    // 処理終了からの時間を計測
                    WaitTimer.StartBy(protocol.Result.TimeStamp);
                    // 受信判定一致していたら次のActionに移行
                    if (NextAction())
                    {
                        // 通常Execを実施
                        await ExecCycle(protocol);
                    }
                    break;

                case AutoTxActionType.Script:
                    if (await ActScriptRx())
                    {
                        // 処理終了からの時間を計測
                        WaitTimer.StartBy(protocol.Result.TimeStamp);
                        // 受信判定一致していたら次のActionに移行
                        if (NextAction())
                        {
                            // 通常Execを実施
                            await ExecCycle(protocol);
                        }
                    }
                    break;

                default:
                    // 受信イベント以外は終了
                    return;
            }
        }

        private bool NextAction()
        {
            // 現在Action終了
            Actions[ActiveActionIndex].IsActive.Value = false;
            // 次のActionに移行
            ActiveActionIndex++;
            // 上限到達で自動終了
            if (ActiveActionIndex >= Actions.Count)
            {
                ActiveActionIndex = 0;
                IsActive.Value = false;
            }

            // 
            var next_action = Actions[ActiveActionIndex];
            // Actionをすべて実行してJobを終了したときも先頭Actionを有効にしておく。
            next_action.IsActive.Value = true;

            if (IsActive.Value)
            {
                // 次Actionチェック
                // 即時実行判定
                if (next_action.Immediate)
                {
                    return true;
                }
            }
            return false;
        }

        private void ActSend(Serial.Protocol protocol)
        {
            var action = Actions[ActiveActionIndex];
            var fb = protocol.GetTxBuffer(action.TxFrameIndex, action.TxFrameBuffIndex);
            string name = fb.Name;
            byte[] buff = fb.Data;
            // バッファ送信
            protocol.SendData(buff, action.TxFrameOffset, action.TxFrameLength);
            // 処理終了からの時間を計測
            WaitTimer.Start();
            // Log出力
            Logger.Add($"[Tx][{name}] {Logger.Byte2Str(buff, action.TxFrameOffset, action.TxFrameLength)}");
        }

        private bool ActWait()
        {
            bool result = false;

            var action = Actions[ActiveActionIndex];
            if (action.Immediate)
            {
                // 即時実行の場合はスレッドを止める
                WaitTimer.WaitThread(action.WaitTime.Value);
                WaitTimer.Start();
                result = true;
            }
            else
            {
                // 即時実行でない場合はポーリング周期で時間計測
                if (WaitTimer.WaitForMsec(action.WaitTime.Value) <= 0)
                {
                    WaitTimer.Start();
                    result = true;
                }
            }
            
            return result;
        }

        private bool ActJump(IList<Comm.AutoTxJob> AutoTxJobs)
        {
            // 現在Action終了
            var action = Actions[ActiveActionIndex];
            action.IsActive.Value = false;
            WaitTimer.Start();
            if (action.AutoTxJobIndex.Value == -1)
            {
                // 自分自身のJumpTo
                ActiveActionIndex = action.JumpTo.Value;
                // 次Actionを有効にする
                Actions[ActiveActionIndex].IsActive.Value = true;
                // 自分自身のJumpToのときtrue
                return true;
            }
            else
            {
                // 指定したジョブのJumpTo
                // Waitタイマはクリアスタートしておく
                var tgt = AutoTxJobs[action.AutoTxJobIndex.Value];
                tgt.Actions[tgt.ActiveActionIndex].IsActive.Value = false;
                tgt.ActiveActionIndex = action.JumpTo.Value;
                tgt.Actions[tgt.ActiveActionIndex].IsActive.Value = true;

                tgt.WaitTimer.Start();
                // 指定したジョブのJumpToのときfalse
                return false;
            }
        }

        private async Task<bool> ActScriptCycle()
        {
            bool result = true;
            var action = Actions[ActiveActionIndex];

            if (action.HasAutoTxHandler)
            {
                // AutoTxイベントハンドラを持っていたらScript実行
                Script.Interpreter.Engine.Comm.AutoTx.Result = true;
                await Script.Interpreter.Engine.ExecuteScriptAsync(action.AutoTxHandler);
                // false時のみ再判定
                if (!Script.Interpreter.Engine.Comm.AutoTx.Result)
                {
                    result = false;
                }
                else
                {
                    // 処理終了からの時間を計測
                    WaitTimer.Start();
                }
            }
            else
            {
                // AutoTxイベントハンドラを持っていない場合、必ずRxイベントハンドラを持っている
                // Rxイベントハンドラにより状態遷移するため、AutoTxは常にfalse
                result = false;
            }

            return result;
        }

        private async Task<bool> ActScriptRx()
        {
            bool result = true;
            var action = Actions[ActiveActionIndex];

            if (action.HasRxHandler)
            {
                // Rxイベントハンドラを持っていたらScript実行
                await Script.Interpreter.Engine.ExecuteScriptAsync(action.RxHandler);
                // false時のみ再判定
                if (!Script.Interpreter.Engine.Comm.RxMatch.Result)
                {
                    result = false;
                }
            }
            else
            {
                // Rxイベントハンドラを持っていない場合、必ずAutoTxイベントハンドラを持っている
                // AutoTxイベントハンドラにより状態遷移するため、Rxは常にfalse
                result = false;
            }

            return result;
        }

        private void ActActivateAutoTx(IList<Comm.AutoTxJob> AutoTxJobs)
        {
            var action = Actions[ActiveActionIndex];
            AutoTxJobs[action.AutoTxJobIndex.Value].IsActive.Value = action.AutoTxState;
            WaitTimer.Start();
        }

        private void ActActivateRx(IList<Comm.RxFrame> RxFrames)
        {
            var action = Actions[ActiveActionIndex];
            RxFrames[action.RxFrameIndex].Patterns[action.RxPatternIndex].IsActive.Value = action.RxState;
            WaitTimer.Start();
        }

        private bool ActRecv(Serial.Protocol protocol)
        {
            var action = Actions[ActiveActionIndex];

            // 要素ゼロはなんでも受信でOK
            if (action.Recvs.Count == 0)
            {
                return true;
            }
            //
            foreach (var item in action.Recvs)
            {
                for (int a_idx = 0; a_idx < protocol.MatchResultPos; a_idx++)
                {
                    var match = protocol.MatchResult[a_idx];
                    if (item.FrameId == match.FrameId && item.PatternId == match.PatternId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ActLog()
        {
            // Log出力
            var action = Actions[ActiveActionIndex];
            Logger.Add(action.Log);
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
