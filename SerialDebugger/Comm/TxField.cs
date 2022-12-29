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
    class TxField : BindableBase, IDisposable
    {
        // 
        public string Name { get; }
        public int BitSize { get; }
        //
        public ReactivePropertySlim<UInt64> Value { get; set; }
        /// <summary>
        /// 1バイト境界からのビット位置.
        /// Nバイト目のMビット目を示す.
        /// </summary>
        internal int BitPos { get; set; }
        /// <summary>
        /// TxFrame内の先頭からのバイト位置
        /// Nバイト目を示す.
        /// </summary>
        internal int BytePos { get; set; }

        internal bool IsByteDisp { get; set; } = false;

        public UInt64 Max { get; }
        public UInt64 Min { get; }
        public UInt64 Mask { get; }

        //
        public enum SelectModeType
        {
            Fix,
            Edit,
        };
        public SelectModeType SelectType { get; }

        public TxField(string name, int bitsize, UInt64 value = 0, SelectModeType type = SelectModeType.Fix)
        {
            Name = name;
            BitSize = bitsize;
            Max = (UInt64)1 << bitsize;
            Min = 0;
            Mask = Max - 1;
            //
            value = value & Mask;
            Value = new ReactivePropertySlim<UInt64>(value);
            Value.AddTo(Disposables);
            //
            SelectType = type;
            MakeSelectMode();
        }

        private void MakeSelectMode()
        {
            switch (SelectType)
            {
                case SelectModeType.Edit:
                case SelectModeType.Fix:
                default:
                    break;
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
