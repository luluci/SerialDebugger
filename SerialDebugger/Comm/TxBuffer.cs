using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Comm
{
    class TxBuffer : BindableBase, IDisposable
    {
        // 
        public string Name { get; }

        /// <summary>
        /// 表示データ
        /// </summary>
        public ReactiveCollection<string> Disp { get; set; }
        /// <summary>
        /// 送信データバイトシーケンス
        /// </summary>
        public List<byte> Buffer { get; set; }

        public ReactiveCommand OnClickSave { get; set; }

        public TxBuffer(string name, int disp_size, int size)
        {
            Name = name;

            Disp = new ReactiveCollection<string>();
            Disp.AddTo(Disposables);
            Buffer = new List<byte>(size);

            OnClickSave = new ReactiveCommand();
            OnClickSave.AddTo(Disposables);
            
            for (int i = 0; i < disp_size; i++)
            {
                Disp.Add("<None>");
            }
            for (int i=0; i<size; i++)
            {
                Buffer.Add(0);
            }
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
