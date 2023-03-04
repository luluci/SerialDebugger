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

    public enum RxMatchType
    {
        Value,
        Any,
        Timeout,
        Script,
        Activate,
        ActivateAutoTx,
        ActivateRx,
    }

    public class RxMatch : BindableBase, IDisposable
    {
        public Field FieldRef { get; set; }
        public ReactivePropertySlim<string> Disp { get; set; }
        public RxMatchType Type { get; set; }

        // MatchAction
        // PatternMatch
        public ReactivePropertySlim<Int64> Value { get; set; }
        // Timeout
        public ReactivePropertySlim<int> Msec { get; set; }
        // Script
        public string RxBegin { get; set; }
        public string RxRecieved { get; set; }
        // Activate AutoTx
        public string AutoTxJobName { get; set; }
        public int AutoTxJobIndex { get; set; }
        public bool AutoTxState { get; set; }
        public AutoTxJob AutoTxJobRef { get; set; }
        // Activate Rx
        public string RxPatternName { get; set; }
        public int RxFrameIndex { get; set; }
        public int RxPatternIndex { get; set; }
        public bool RxState { get; set; }
        public RxPattern RxPatternRef { get; set; }

        public RxMatch()
        {
            Disp = new ReactivePropertySlim<string>();
            Disp.AddTo(Disposables);
            Value = new ReactivePropertySlim<Int64>();
            Value.AddTo(Disposables);
            Msec = new ReactivePropertySlim<int>();
            Msec.AddTo(Disposables);
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
