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

    public class TxFieldBuffer : BindableBase, IDisposable
    {
        public string Name { get; }
        public int Id { get; }
        public TxFrame FrameRef { get; }

        public ReactiveCollection<FieldValue> FieldValues { get; set; }
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
            FieldValues = new ReactiveCollection<FieldValue>();
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

        /// <summary>
        /// Bufferに変更があれば送信Dataに反映する
        /// </summary>
        /// <returns></returns>
        public bool BufferFix()
        {
            switch (ChangeState.Value)
            {
                case Comm.Field.ChangeStates.Changed:
                    // 変更内容をシリアル通信データに反映
                    BufferToData();
                    return true;

                default:
                    // 変更なし
                    return false;
            }
        }

        public void BufferToData()
        {
            // バッファを送信データにコピー
            if (FrameRef.AsAscii)
            {
                for (int i = 0; i < Buffer.Count; i++)
                {
                    // HEXをASCII化
                    var ch = Utility.HexAscii.AsciiTbl[Buffer[i]];
                    // little-endianで格納
                    Data[i * 2 + 0] = (byte)ch[1];
                    Data[i * 2 + 1] = (byte)ch[0];
                }
            }
            else
            {
                Buffer.CopyTo(Data, 0);
            }
            // 変更フラグを下す
            foreach (var field in FieldValues)
            {
                field.ChangeState.Value = Comm.Field.ChangeStates.Fixed;
            }
            //
            ChangeState.Value = Comm.Field.ChangeStates.Fixed;
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
