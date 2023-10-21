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
    using System.Windows;
    using Utility;

    public class InputStringViewModel : BindableBase, IDisposable
    {
        public ReactivePropertySlim<string> InputString { get; set; }
        public ReactivePropertySlim<string> Caption { get; set; }
        public ReactiveCommand OnLostFocus { get; set; }

        public Comm.TxFrame TxFrameRef { get; set; }
        public Comm.Field FieldRef { get; set; }
        public Comm.TxFieldBuffer TxFieldBufferRef { get; set; }

        Window window_;

        public InputStringViewModel(Window window)
        {
            window_ = window;

            InputString = new ReactivePropertySlim<string>();
            InputString.AddTo(Disposables);
            Caption = new ReactivePropertySlim<string>();
            Caption.AddTo(Disposables);

            OnLostFocus = new ReactiveCommand();
            //OnLostFocus.Subscribe(x =>
            //{
            //    window.Hide();
            //})
            OnLostFocus.AddTo(Disposables);
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
