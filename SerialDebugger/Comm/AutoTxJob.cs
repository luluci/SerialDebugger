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
    using Utility;
    using Logger = Log.Log;
    
    public class AutoTxJob : BindableBase, IDisposable
    {
        public int Id { get; }
        public string Name { get; private set; }
        public string Alias { get; private set; }
        public ReactivePropertySlim<bool> IsActive { get; set; }
        public (DateTime, AutoTxActionType, object)[] LogBuffer;
        public int LogBufferHead;
        public int LogBufferTail;
        public Utility.CycleTimer CycleTimer;

        public ReactiveCollection<AutoTxAction> Actions { get; set; }

        public int ActiveActionIndex { get; set; }

        public AutoTxJob(int id, string name, string alias, bool active = false)
        {
            Id = id;
            CycleTimer = new Utility.CycleTimer();

            Name = name;
            IsActive = new ReactivePropertySlim<bool>(active);
            IsActive.Subscribe((x) =>
            {
                if (x)
                {
                    CycleTimer.Start();
                }
            });
            IsActive.AddTo(Disposables);
            Actions = new ReactiveCollection<AutoTxAction>();
            Actions.AddTo(Disposables);

            Alias = alias;
            if (object.ReferenceEquals(Alias, string.Empty))
            {
                Alias = Name;
            }
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
                    if (action.TxFrameBuffIndex.Value < 0)
                    {
                        throw new Exception($"AutoTx: SendAction: TxFrameBuffIndex: 0より大きな値を指定してください。({action.TxFrameBuffIndex.Value})");
                    }
                    else if (action.TxFrameBuffIndex.Value >= frame.BufferSize)
                    {
                        throw new Exception($"AutoTx: SendAction: TxFrameBuffIndex: BackupBufferの定義数より大きな値が指定されました。({action.TxFrameBuffIndex.Value})");
                    }
                    var fb = frame.Buffers[action.TxFrameBuffIndex.Value];
                    // Offset/Length
                    if (action.TxFrameOffset == -1)
                    {
                        action.TxFrameOffset = 0;
                    }
                    if (action.TxFrameLength == -1)
                    {
                        action.TxFrameLength = frame.Length;
                    }
                    if (action.TxFrameOffset >= frame.Length || (action.TxFrameOffset + action.TxFrameLength) > frame.Length)
                    {
                        throw new Exception("AutoTx: SendAction: TxFrameOffset/TxFrameLength.");
                    }
                    // Aliasチェック
                    if (Object.ReferenceEquals(action.Alias, string.Empty))
                    {
                        action.Alias = $"Send [{fb.Name}]";
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
        
        public async Task Exec(SerialPort serial, IList<Comm.TxFrame> TxFrames, IList<Comm.RxFrame> RxFrames, IList<Comm.AutoTxJob> AutoTxJobs)
        {
            bool check = true;
            while (check)
            {
                check = false;

                switch (Actions[ActiveActionIndex].Type)
                {
                    case AutoTxActionType.Send:
                        ExecSend(serial, TxFrames);
                        // 次のActionに移行
                        check = NextAction();
                        break;

                    case AutoTxActionType.Wait:
                        // 時間経過判定
                        if (ExecWait())
                        {
                            // 時間経過していたら次のActionに移行
                            check = NextAction();
                        }
                        break;

                    case AutoTxActionType.Jump:
                        ExecJump();
                        check = true;
                        break;

                    case AutoTxActionType.Script:
                        if (await ExecScript())
                        {
                            check = NextAction();
                        }
                        break;

                    case AutoTxActionType.ActivateAutoTx:
                        ExecActivateAutoTx(AutoTxJobs);
                        // 次のActionに移行
                        check = NextAction();
                        break;

                    case AutoTxActionType.ActivateRx:
                        ExecActivateRx(RxFrames);
                        // 次のActionに移行
                        check = NextAction();
                        break;

                    case AutoTxActionType.Recv:
                        // Recvは周期判定では変化しない
                        break;

                    case AutoTxActionType.Log:
                        ExecLog();
                        // 次のActionに移行
                        check = NextAction();
                        break;

                    default:
                        throw new Exception("undefined type.");
                }
            }
        }

        public async Task Exec(SerialPort serial, IList<Comm.TxFrame> TxFrames, IList<Comm.RxFrame> RxFrames, IList<Comm.AutoTxJob> AutoTxJobs, Serial.RxAnalyzer analyzer)
        {
            switch (Actions[ActiveActionIndex].Type)
            {
                case AutoTxActionType.Recv:
                    var result = ExecRecv(analyzer);
                    if (result)
                    {
                        // 処理終了からの時間を計測
                        CycleTimer.StartBy(analyzer.Result.TimeStamp);
                        // 受信判定一致していたら次のActionに移行
                        if (NextAction())
                        {
                            // 通常Execを実施
                            await Exec(serial, TxFrames, RxFrames, AutoTxJobs);
                        }
                    }
                    break;

                case AutoTxActionType.Script:
                    if (await ExecScriptRx())
                    {
                        // 処理終了からの時間を計測
                        CycleTimer.StartBy(analyzer.Result.TimeStamp);
                        // 受信判定一致していたら次のActionに移行
                        if (NextAction())
                        {
                            // 通常Execを実施
                            await Exec(serial, TxFrames, RxFrames, AutoTxJobs);
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

        private void ExecSend(SerialPort serial, IList<Comm.TxFrame> TxFrames)
        {
            var action = Actions[ActiveActionIndex];
            var frame = TxFrames[action.TxFrameIndex];
            var fb = frame.Buffers[action.TxFrameBuffIndex.Value];
            var buff_idx = action.TxFrameBuffIndex.Value;
            string name;
            // バッファ選択
            //Buffer.BlockCopy(TxFrames[action.TxFrameIndex].TxData, action.TxFrameOffset, buff, 0, action.TxFrameLength);
            name = fb.Name;
            byte[] buff = fb.Data;
            // バッファ送信
            serial.Write(buff, action.TxFrameOffset, action.TxFrameLength);
            // 処理終了からの時間を計測
            CycleTimer.Start();
            // Log出力
            Logger.Add($"[Tx][{name}] {Logger.Byte2Str(buff, action.TxFrameOffset, action.TxFrameLength)}");
        }

        private bool ExecWait()
        {
            bool result = false;

            var action = Actions[ActiveActionIndex];
            if (action.Immediate)
            {
                // 即時実行の場合はスレッドを止める
                CycleTimer.WaitThread(action.WaitTime.Value);
                CycleTimer.Start();
                result = true;
            }
            else
            {
                // 即時実行でない場合はポーリング周期で時間計測
                if (CycleTimer.WaitForMsec(action.WaitTime.Value) <= 0)
                {
                    CycleTimer.Start();
                    result = true;
                }
            }
            
            return result;
        }

        private void ExecJump()
        {
            // 現在Action終了
            Actions[ActiveActionIndex].IsActive.Value = false;
            // JumpToに移行
            ActiveActionIndex = Actions[ActiveActionIndex].JumpTo.Value;
            // Actionを有効にする
            Actions[ActiveActionIndex].IsActive.Value = true;
        }

        private async Task<bool> ExecScript()
        {
            bool result = true;
            var action = Actions[ActiveActionIndex];

            if (action.HasAutoTxHandler)
            {
                // AutoTxイベントハンドラを持っていたらScript実行
                Script.Interpreter.Engine.Comm.AutoTx.Result = true;
                await Script.Interpreter.Engine.wv.CoreWebView2.ExecuteScriptAsync(action.AutoTxHandler);
                // false時のみ再判定
                if (!Script.Interpreter.Engine.Comm.AutoTx.Result)
                {
                    result = false;
                }
                else
                {
                    // 処理終了からの時間を計測
                    CycleTimer.Start();
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

        private async Task<bool> ExecScriptRx()
        {
            bool result = true;
            var action = Actions[ActiveActionIndex];

            if (action.HasRxHandler)
            {
                // AutoTxイベントハンドラを持っていたらScript実行
                Script.Interpreter.Engine.Comm.AutoTx.Result = true;
                await Script.Interpreter.Engine.wv.CoreWebView2.ExecuteScriptAsync(action.RxHandler);
                // false時のみ再判定
                if (!Script.Interpreter.Engine.Comm.AutoTx.Result)
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

        private void ExecActivateAutoTx(IList<Comm.AutoTxJob> AutoTxJobs)
        {
            var action = Actions[ActiveActionIndex];

            AutoTxJobs[action.AutoTxJobIndex].IsActive.Value = action.AutoTxState;
        }

        private void ExecActivateRx(IList<Comm.RxFrame> RxFrames)
        {
            var action = Actions[ActiveActionIndex];

            RxFrames[action.RxFrameIndex].Patterns[action.RxPatternIndex].IsActive.Value = action.RxState;
        }

        private bool ExecRecv(Serial.RxAnalyzer analyzer)
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
                for (int a_idx = 0; a_idx < analyzer.MatchResultPos; a_idx++)
                {
                    var match = analyzer.MatchResult[a_idx];
                    if (item.FrameId == match.FrameId && item.PatternId == match.PatternId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ExecLog()
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
