using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Log
{
    using System.IO;
    using Utility;

    static class Log
    {
        static private LogImpl Impl = new LogImpl();
        static private MainWindow MainWindow;

        static public string Directory { get; set; }
        static public int LogMax { get; set; } = 100;
        static public bool OutputFile { get; set; } = false;

        static public ReactiveCollection<string> GetLogData()
        {
            return Impl.Log;
        }

        static public void Add(string log)
        {
            Add(DateTime.Now, log);
        }

        static public void Add(DateTime date, string log)
        {
            // 上限以上ならログを先頭から削除
            if (Impl.Log.Count >= LogMax)
            {
                Impl.Log.RemoveAt(0);
            }
            // ログメッセージ作成
            var msg = $"{GetTimestamp(date)} {log}";
            // GUI表示
            Impl.Log.Add(msg);
            MainWindow.log_scrl.ScrollToBottom();
            // ファイル出力
            if (OutputFile)
            {
                Impl.Writer.WriteLine(msg);
            }
        }

        static public void AddException(Exception e, string prefix = "")
        {
            if (e.InnerException is null)
            {
                //MessageBox.Show($"init exception: {e.Message}");
                Add($"{prefix}\n  {e.Message}");
            }
            else
            {
                //MessageBox.Show($"init exception: {e.Message}");
                Add($"{prefix}\n  {e.InnerException.Message}");
            }
        }

        static public void AddException(AggregateException e, string prefix = "")
        {
            if (e.InnerException is null)
            {
                //MessageBox.Show($"init exception: {e.Message}");
                Add($"{prefix}\n  {e.Message}");
            }
            else
            {
                //MessageBox.Show($"init exception: {e.Message}");
                var sb = new StringBuilder();
                sb.AppendLine($"{prefix}");
                foreach (var ie in e.InnerExceptions)
                {
                    sb.AppendLine($"  {ie.Message}");
                }
                Add(sb.ToString());
            }
        }

        static public void Init(MainWindow window)
        {
            MainWindow = window;
        }

        static public void Clear()
        {
            Impl.Log.Clear();
        }

        static public void UpdateSetting(Settings.Log setting)
        {
            // 設定更新
            Directory = setting.Directory;
            LogMax = setting.MaxSize;
            OutputFile = setting.OutputFile;
            // ログファイルを閉じる
            Impl.Close();
            // ログファイル設定
            if (OutputFile)
            {
                // ディレクトリ存在チェック
                if (!System.IO.Directory.Exists(Directory))
                {
                    System.IO.Directory.CreateDirectory(Directory);
                }
                // ログファイル作成
                var dt = DateTime.Now;
                var filepath = $"{Directory}/log_{dt.ToString("yyyyMMddHHmm")}.txt";
                Impl.Open(filepath);
            }
        }

        static public string Last()
        {
            return Impl.Log.Last();
        }

        static public string GetTimestamp()
        {
            return GetTimestamp(DateTime.Now);
        }
        static public string GetTimestamp(DateTime time)
        {
            //return time.ToString("yyyy/MM/dd/HH:mm:ss.FFFF");
            return time.ToString("HH:mm:ss.fff");
        }

        static public string Byte2Str(byte[] data)
        {
            return BitConverter.ToString(data);
        }
        static public string Byte2Str(byte[] data, int offset, int length)
        {
            return BitConverter.ToString(data, offset, length);
        }

    }

    class LogImpl : BindableBase, IDisposable
    {
        public ReactiveCollection<string> Log { get; set; }
        public StreamWriter Writer { get; set; }

        public LogImpl()
        {
            Log = new ReactiveCollection<string>();
            Log.AddTo(Disposables);

            Writer = null;
        }

        public void Close()
        {
            //
            if (!(Writer is null))
            {
                Writer.Close();
            }
        }

        public void Open(string path)
        {
            //
            Writer = new StreamWriter(path);
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
                //if (!(Writer is null))
                //{
                //    Writer.Dispose();
                //    Writer = null;
                //}
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        ~LogImpl()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(false);
        }

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
