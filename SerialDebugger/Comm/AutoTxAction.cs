using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Comm
{
    using Utility;

    enum AutoTxActionType
    {
        Send,       // シリアル送信
        Wait,       // 時間待機
        Recv,       // 受信待機
        Jump,       // ジャンプ
        Script,     // スクリプト実行
    }

    class AutoTxAction : BindableBase, IDisposable
    {
        public int Id { get; }
        public string Alias { get; private set; }

        public AutoTxActionType Type { get; private set; }
        public ReactivePropertySlim<bool> IsActive { get; set; }
        public bool Immediate { get; set; }

        public ReactivePropertySlim<string> TxFrameName { get; private set; }
        public int TxFrameIndex { get; private set; }
        public int TxFrameOffset { get; set; }
        public int TxFrameLength { get; set; }
        public ReactivePropertySlim<int> TxFrameBuffIndex { get; private set; }

        /// <summary>
        /// 待機時間(milli sec)
        /// </summary>
        public ReactivePropertySlim<int> WaitTime { get; private set; }

        public ReactivePropertySlim<string> RxFrameName { get; private set; }
        public int RxAnalyzeIndex { get; private set; }

        public ReactivePropertySlim<int> JumpTo { get; private set; }

        public ReactivePropertySlim<string> ScriptName { get; private set; }


        public AutoTxAction(int id, string alias)
        {
            Id = id;
            Alias = alias;

            IsActive = new ReactivePropertySlim<bool>(false);
            IsActive.AddTo(Disposables);
        }

        /// <summary>
        /// 時間待機ノード作成
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wait"></param>
        /// <returns></returns>
        public static AutoTxAction MakeWaitAction(int id, string alias, int wait, bool immediate)
        {
            var action = new AutoTxAction(id, alias)
            {
                Type = AutoTxActionType.Wait,
                Immediate = immediate,
            };
            action.WaitTime = new ReactivePropertySlim<int>(wait);
            action.WaitTime.AddTo(action.Disposables);

            if (Object.ReferenceEquals(action.Alias, string.Empty))
            {
                action.Alias = $"Wait [{action.WaitTime} ms]";
            }

            return action;
        }

        public static AutoTxAction MakeJumpAction(int id, string alias, int jumpto, bool immediate)
        {
            var action = new AutoTxAction(id, alias)
            {
                Type = AutoTxActionType.Jump,
                Immediate = immediate,
            };
            action.JumpTo = new ReactivePropertySlim<int>(jumpto);
            action.JumpTo.AddTo(action.Disposables);

            if (Object.ReferenceEquals(action.Alias, string.Empty))
            {
                action.Alias = $"JumpTo [{action.JumpTo}]";
            }

            return action;
        }

        public static AutoTxAction MakeSendAction(int id, string alias, string tx_frame_name, int tx_frame_idx, int buff_idx, int buff_offset, int buff_length, bool immediate)
        {
            var action = new AutoTxAction(id, alias)
            {
                Type = AutoTxActionType.Send,
                Immediate = immediate,
            };
            action.TxFrameName = new ReactivePropertySlim<string>(tx_frame_name);
            action.TxFrameName.AddTo(action.Disposables);
            action.TxFrameIndex = tx_frame_idx;
            action.TxFrameOffset = buff_offset;
            action.TxFrameLength = buff_length;
            action.TxFrameBuffIndex = new ReactivePropertySlim<int>(buff_idx);
            action.TxFrameBuffIndex.AddTo(action.Disposables);

            if (Object.ReferenceEquals(action.Alias, string.Empty))
            {
                action.Alias = $"Send [{action.TxFrameName.Value}]";
            }

            return action;
        }

        public static AutoTxAction MakeRecvAction(int id, string alias, string rx_name, int rx_idx, bool immediate)
        {
            var action = new AutoTxAction(id, alias)
            {
                Type = AutoTxActionType.Recv,
                Immediate = immediate,
            };
            action.RxFrameName = new ReactivePropertySlim<string>(rx_name);
            action.RxFrameName.AddTo(action.Disposables);
            action.RxAnalyzeIndex = rx_idx;

            return action;
        }

        public static AutoTxAction MakeScriptAction(int id, string alias, string script_func, bool immediate)
        {
            var action = new AutoTxAction(id, alias)
            {
                Type = AutoTxActionType.Script,
                Immediate = immediate,
            };
            action.ScriptName = new ReactivePropertySlim<string>(script_func);
            action.ScriptName.AddTo(action.Disposables);

            return action;
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
