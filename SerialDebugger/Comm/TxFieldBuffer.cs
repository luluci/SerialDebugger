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

    class TxFieldBuffer : BindableBase, IDisposable
    {
        public string Name { get; }
        public int Id { get; }
        public TxFrame FrameRef { get; }

        public ReactiveCollection<TxFieldValue> FieldValues { get; set; }
        public ReactivePropertySlim<Field.ChangeStates> ChangeState { get; set; }

        public ReactiveCommand OnClickSave { get; set; }
        public ReactiveCommand OnClickStore { get; set; }

        // 送信データバイトシーケンス
        public ReactiveCollection<byte> Buffer { get; set; }
        // 確定送信データ
        public byte[] Data { get; set; }

        public TxFieldBuffer(int id, string name, TxFrame frame)
        {
            //
            Id = id;
            Name = name;
            FrameRef = frame;
            //
            ChangeState = new ReactivePropertySlim<Field.ChangeStates>();
            ChangeState.AddTo(Disposables);
            Buffer = new ReactiveCollection<byte>();
            Buffer.AddTo(Disposables);
            FieldValues = new ReactiveCollection<TxFieldValue>();
            FieldValues
                .ObserveElementObservableProperty(x => x.Value).Subscribe(x =>
                {
                    FrameRef.Update(this, x.Instance);
                });
            FieldValues
                .ObserveElementObservableProperty(x => x.SelectIndex).Subscribe(x =>
                {
                    FrameRef.Update(this, x.Instance);
                });
            FieldValues
                .ObserveElementObservableProperty(x => x.ChangeState).Subscribe(x =>
                {
                    ChangeState.Value = Field.ChangeStates.Changed;
                });
            FieldValues.AddTo(Disposables);
            //
            OnClickSave = new ReactiveCommand();
            OnClickSave.AddTo(Disposables);
            OnClickStore = new ReactiveCommand();
            OnClickStore.AddTo(Disposables);
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
