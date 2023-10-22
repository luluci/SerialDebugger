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
    using System.Windows.Media;
    using Utility;

    public class Group : IDisposable
    {
        // 設定ファイル項目
        // ID
        public int Id { get; }
        // group名
        public string Name { get; }
        // 対象field開始位置
        public int Begin { get; }
        // 対象field終了位置
        public int End { get; }
        // group内表示ID開始値
        public int IdBegin { get; }
        //
        public Brush BackgroundColor { get; }

        // 表示用データ
        public int BitBegin { get; set; }
        public int BitEnd { get; set; }
        public int BitLen { get; set; }

        public Group(int id, string name, int begin, int end, int id_begin, Brush bgcolor)
        {
            Id = id;
            Name = name;
            Begin = begin;
            End = end;
            IdBegin = id_begin;
            BackgroundColor = bgcolor;
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
