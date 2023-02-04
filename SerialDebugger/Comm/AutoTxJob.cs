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
    
    class AutoTxJob : BindableBase, IDisposable
    {
        public int Id { get; }
        public ReactivePropertySlim<string> Name { get; }
        public ReactivePropertySlim<bool> IsActive { get; set; }
        public (DateTime, AutoTxActionType, object)[] LogBuffer;
        public int LogBufferHead;
        public int LogBufferTail;
        public Utility.CycleTimer CycleTimer;

        public ReactiveCollection<AutoTxAction> Actions { get; set; }

        public int ActiveActionIndex { get; set; }

        public AutoTxJob(int id, string name, bool active = false)
        {
            Id = id;
            CycleTimer = new Utility.CycleTimer();

            Name = new ReactivePropertySlim<string>(name);
            Name.AddTo(Disposables);
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
                    if (action.TxFrameBuffIndex.Value > 0 && (action.TxFrameBuffIndex.Value - 1) >= TxFrames[action.TxFrameIndex].BackupBufferLength)
                    {
                        throw new Exception("AutoTx: SendAction: TxFrameBuffIndex.");
                    }
                    // Offset/Length
                    var frame = TxFrames[action.TxFrameIndex];
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
                    break;

                case AutoTxActionType.Wait:
                    break;

                case AutoTxActionType.Jump:
                    if (action.JumpTo.Value >= Actions.Count)
                    {
                        throw new Exception("AutoTx: SendAction: JumpTo.");
                    }
                    break;

                case AutoTxActionType.ActivateAutoTx:
                    break;

                default:
                    throw new Exception("undefined type.");
            }
        }
        
        public void Exec(SerialPort serial, IList<Comm.TxFrame> TxFrames, IList<Comm.AutoTxJob> AutoTxJobs)
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
                        var result = ExecWait();
                        if (result)
                        {
                            // 時間経過していたら次のActionに移行
                            check = NextAction();
                        }
                        break;

                    case AutoTxActionType.Jump:
                        ExecJump();
                        check = true;
                        break;

                    case AutoTxActionType.ActivateAutoTx:
                        ExecActivateAutoTx(AutoTxJobs);
                        // 次のActionに移行
                        check = NextAction();
                        break;

                    default:
                        throw new Exception("undefined type.");
                }
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
            var buff_idx = action.TxFrameBuffIndex.Value;
            // バッファ選択
            byte[] buff;
            if (buff_idx == 0)
            {
                //Buffer.BlockCopy(TxFrames[action.TxFrameIndex].TxData, action.TxFrameOffset, buff, 0, action.TxFrameLength);
                buff = TxFrames[action.TxFrameIndex].TxData;
            }
            else
            {
                //Buffer.BlockCopy(TxFrames[action.TxFrameIndex].BackupBuffer[buff_idx - 1].TxData, action.TxFrameOffset, buff, 0, action.TxFrameLength);
                buff = TxFrames[action.TxFrameIndex].BackupBuffer[buff_idx - 1].TxData;
            }
            // バッファ送信
            serial.Write(buff, action.TxFrameOffset, action.TxFrameLength);
            // 処理終了からの時間を計測
            CycleTimer.Start();
            // Log出力
            Logger.Add($"Auto Send: {Logger.Byte2Str(buff, action.TxFrameOffset, action.TxFrameLength)}");
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

        private void ExecActivateAutoTx(IList<Comm.AutoTxJob> AutoTxJobs)
        {
            var action = Actions[ActiveActionIndex];

            AutoTxJobs[action.AutoTxJobIndex.Value].IsActive.Value = true;
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
