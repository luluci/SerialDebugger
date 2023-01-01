using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace SerialDebugger.Settings
{
    using Logger = SerialDebugger.Log.Log;

    class SettingInfo
    {
        // 設定ファイル情報
        public string FilePath { get; set; }
        public string Name { get; set; }
        // 設定内容
        public Gui Gui { get; set; }
        public Serial Serial { get; set; }
        public Comm Comm { get; set; }
    }

    static class Settings
    {
        static public SettingsImpl Impl = new SettingsImpl();
        static public ReactiveCollection<SettingInfo> DataList { get; set; }
        static public SettingInfo Data { get; set; }
        static public ReactivePropertySlim<int> DataIndex { get; set; }

        static public void Init(int idx)
        {
            DataList = Impl.DataList;
            DataIndex = Impl.DataIndex;
            Select(idx);
        }

        static public void Select(int idx)
        {
            if (DataList.Count > idx)
            {
                DataIndex.Value = idx;
                Data = DataList[idx];
            }
        }
    }

    class SettingsImpl : BindableBase, IDisposable
    {
        public ReactiveCollection<SettingInfo> DataList { get; set; }
        public ReactivePropertySlim<int> DataIndex { get; set; }

        public SettingsImpl()
        {
            DataList = new ReactiveCollection<SettingInfo>();
            DataList.AddTo(Disposables);
            DataIndex = new ReactivePropertySlim<int>(0);
            DataIndex.AddTo(Disposables);

            Load();
        }

        public void Load()
        {
            // デフォルトパス
            string rootPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string SettingPath = rootPath + @"\Settings";
            // 設定ファイルチェック
            if (Directory.Exists(SettingPath))
            {
                // ディレクトリ内ファイル取得
                var SettingFiles = Directory.GetFiles(SettingPath);
                //
                foreach (var file in SettingFiles)
                {
                    try
                    {
                        // 
                        var info = new SettingInfo
                        {
                            FilePath = file
                        };
                        LoadSettingFile(file, info);
                        DataList.Add(info);
                    }
                    catch (Exception e)
                    {
                        if (e.InnerException is null)
                        {
                            Logger.Add($"json解析エラー: {e.Message} : in file {file}");
                        }
                        else
                        {
                            Logger.Add($"json解析エラー: {e.InnerException.Message} : in file {file}");
                        }
                    }
                }
            }
        }

        private void LoadSettingFile(string path, SettingInfo info)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                //NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };
            //
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // jsonファイルパース
                // 呼び出し元でWait()している。ConfigureAwait(false)無しにawaitするとデッドロックで死ぬ。
                var json = JsonSerializer.DeserializeAsync<Json.Settings>(stream, options).AsTask();
                json.Wait();
                // json読み込み
                MakeSetting(json.Result, info);
            }
        }

        private void MakeSetting(Json.Settings json, SettingInfo info)
        {
            // 設定ファイル情報
            info.Name = json.Name;

            // GUI
            info.Gui = new Gui();
            info.Gui.AnalyzeJson(json.Gui);
            // Serial
            info.Serial = new Serial();
            info.Serial.AnalyzeJson(json.Serial);
            // Comm
            info.Comm = new Comm();
            info.Comm.AnalyzeJson(json.Comm);
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

    partial class Json
    {

        public class Settings
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            // GUI設定
            [JsonPropertyName("gui")]
            public Gui Gui { get; set; }

            // COMポート設定
            [JsonPropertyName("serial")]
            public Serial Serial { get; set; }

            // 通信フレーム設定
            [JsonPropertyName("comm")]
            public Comm Comm { get; set; }
        }

    }

}
