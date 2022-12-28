using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Serial
{
    class TxFrame : BindableBase, IDisposable
    {
        // 
        public string Name { get; }
        /// <summary>
        /// 送信フレーム: TxField集合体
        /// </summary>
        public ReactiveCollection<TxField> Frame { get; set; }
        /// <summary>
        /// 送信バッファ
        /// </summary>
        public ReactiveCollection<TxBuffer> Buffer { get; set; }
        /// <summary>
        /// 送信バッファ数
        /// </summary>
        public int BufferLength { get; }
        /// <summary>
        /// 送信フレームバイト長
        /// </summary>
        public int Length { get; set; } = 0;
        public int BitLength { get; set; } = 0;

        public TxFrame(string name, int buff_len)
        {
            Name = name;
            BufferLength = buff_len;

            Frame = new ReactiveCollection<TxField>();
            Frame.AddTo(Disposables);
            Buffer = new ReactiveCollection<TxBuffer>();
            Buffer.AddTo(Disposables);
        }

        public void Add(TxField field)
        {
            Frame.Add(field);
        }

        /// <summary>
        /// 登録したFieldからFrame情報を構築する。
        /// Build()は一度のみ行い、内部情報をクリアしてのインスタンス再利用はしない。
        /// *特にブロックはかけていないので運用でカバー
        /// </summary>
        public void Build()
        {
            // Fieldから情報収集
            int bit_pos = 0;
            int byte_pos = 0;
            foreach (var f in Frame)
            {
                // Field位置セット
                f.BitPos = bit_pos;
                f.BytePos = byte_pos;
                // Frame情報更新
                BitLength += f.BitSize;
                // Fiels位置更新
                bit_pos += f.BitSize;
                if (bit_pos >= 8)
                {
                    byte_pos += bit_pos / 8;
                    bit_pos %= 8;
                }
            }
            // バイトサイズ計算
            Length = (BitLength / 8);
            if ((BitLength % 8) != 0)
            {
                Length++;
            }
            // 送信データバッファ作成
            for (int i=0; i<BufferLength; i++)
            {
                Buffer.Add(new TxBuffer($"buffer_{i}", Length));
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
