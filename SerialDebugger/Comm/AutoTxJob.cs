using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.IO.Ports;

namespace SerialDebugger.Comm
{
    using Logger = Log.Log;
    
    class AutoTxJob : BindableBase, IDisposable
    {
        public int Id { get; }
        public ReactivePropertySlim<string> Name { get; }
        public ReactivePropertySlim<bool> IsActive { get; set; }
        public bool IsDelayLog { get; set; }
        public (DateTime, AutoTxActionType, object)[] LogBuffer;
        public int LogBufferHead;
        public int LogBufferTail;

        public ReactiveCollection<AutoTxAction> Actions { get; set; }

        public int ActiveActionIndex { get; set; }

        public AutoTxJob(int id, string name, bool active = false)
        {
            Id = id;

            Name = new ReactivePropertySlim<string>(name);
            Name.AddTo(Disposables);
            IsActive = new ReactivePropertySlim<bool>(active);
            IsActive.AddTo(Disposables);
            Actions = new ReactiveCollection<AutoTxAction>();
            Actions.AddTo(Disposables);
            IsDelayLog = false;
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

                default:
                    throw new Exception("undefined type.");
            }
        }

        public void Log()
        {
            int i = LogBufferTail;
            while (i != LogBufferHead)
            {
                var log = LogBuffer[i];
                Logger.Add(log.Item1, $"Auto Send: {Logger.Byte2Str(log.Item3 as byte[])} (delay)");

                i++;
            }
            LogBufferHead = 0;
            LogBufferTail = LogBufferHead;
        }

        public void Exec(SerialPort serial, IList<Comm.TxFrame> TxFrames, int msec)
        {
            bool check = true;
            while (check)
            {
                check = false;

                switch (Actions[ActiveActionIndex].Type)
                {
                    case AutoTxActionType.Send:
                        ExecSend(serial, TxFrames);
                        check = NextAction();
                        break;

                    case AutoTxActionType.Wait:
                        var result = ExecWait(msec);
                        if (result)
                        {
                            check = NextAction();
                        }
                        break;

                    case AutoTxActionType.Jump:
                        ExecJump();
                        check = true;
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
                IsDelayLog = false;
            }
            // Actionをすべて実行してJobを終了したときも先頭Actionを有効にしておく。
            Actions[ActiveActionIndex].IsActive.Value = true;

            // 次Actionチェック
            if (IsActive.Value)
            {
                switch (Actions[ActiveActionIndex].Type)
                {
                    case AutoTxActionType.Wait:
                        return true;

                    default:
                        return false;
                }
            }
            return false;
        }

        private void ExecSend(SerialPort serial, IList<Comm.TxFrame> TxFrames)
        {
            var action = Actions[ActiveActionIndex];
            var buff_idx = action.TxFrameBuffIndex.Value;
            // バッファ選択
            byte[] buff = new byte[action.TxFrameLength];
            if (buff_idx == 0)
            {
                Buffer.BlockCopy(TxFrames[action.TxFrameIndex].TxData, action.TxFrameOffset, buff, 0, action.TxFrameLength);
            }
            else
            {
                Buffer.BlockCopy(TxFrames[action.TxFrameIndex].BackupBuffer[buff_idx - 1].TxData, action.TxFrameOffset, buff, 0, action.TxFrameLength);
            }
            // バッファ送信
            serial.Write(buff, 0, buff.Length);

            // ログ追加
            LogBuffer[LogBufferHead] = (DateTime.Now, AutoTxActionType.Send, buff);
            LogBufferHead++;
            if (LogBufferHead >= LogBuffer.Length)
            {
                LogBufferHead = 0;
            }
            if (LogBufferHead == LogBufferTail)
            {
                LogBufferTail++;
            }
            if (LogBufferTail >= LogBuffer.Length)
            {
                LogBufferHead = 0;
            }
            // ログ遅延出力チェック
            IsDelayLog = action.IsDelayLog;
        }

        private bool ExecWait(int msec)
        {
            bool result = false;

            var action = Actions[ActiveActionIndex];
            if (action.WaitTimeBegin == -1)
            {
                action.WaitTimeBegin = msec;
            }
            else
            {
                if (msec - action.WaitTimeBegin >= action.WaitTime.Value)
                {
                    result = true;
                    action.WaitTimeBegin = -1;
                }
            }
            // ログ遅延出力チェック
            IsDelayLog = action.IsDelayLog;

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
            // ログ遅延出力チェック
            // 暫定:Jumpで強制ログ出力
            IsDelayLog = false;
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
