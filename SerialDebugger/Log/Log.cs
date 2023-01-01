﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Log
{
    static class Log
    {
        static private LogImpl Impl = new LogImpl();
        static private MainWindow MainWindow;

        static public ReactiveCollection<string> GetLogData()
        {
            return Impl.Log;
        }

        static public void Add(string log)
        {
            if (Impl.Log.Count >= Impl.LogMax)
            {
                Impl.Log.RemoveAt(0);
            }
            Impl.Log.Add(log);
            MainWindow.log_scrl.ScrollToBottom();
        }

        static public void Init(MainWindow window)
        {
            MainWindow = window;
        }

        static public string Last()
        {
            return Impl.Log.Last();
        }
    }

    class LogImpl : BindableBase, IDisposable
    {
        public ReactiveCollection<string> Log { get; set; }
        public int LogMax { get; } = 100;

        public LogImpl()
        {
            Log = new ReactiveCollection<string>();
            Log.AddTo(Disposables);
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