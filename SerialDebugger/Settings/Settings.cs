using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.IO;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Windows;

namespace SerialDebugger.Settings
{
    using Utility;
    using Logger = SerialDebugger.Log.Log;

    public class SettingInfo
    {
        // 設定ファイル情報
        public string FilePath { get; set; }
        public string Name { get; set; }
        // ログ設定
        public Log Log { get; set; } = new Log();
        // 設定読み込み遅延処理
        public bool IsLoaded { get; set; } = false;
        // 設定内容
        public Output Output { get; set; } = new Output();
        public Gui Gui { get; set; } = new Gui();
        public Serial Serial { get; set; } = new Serial();
        public Comm Comm { get; set; } = new Comm();
        public Script Script { get; set; } = new Script();
    }

    public static class Settings
    {
        static public SettingsImpl Impl = new SettingsImpl();
        //static public ReactiveCollection<SettingInfo> DataList { get; set; }
        static public SettingInfo Data { get; set; }
        //static public ReactivePropertySlim<int> DataIndex { get; set; }

        static public async Task InitAsync(ReactiveCollection<SettingInfo> list)
        {
            await Impl.InitAsync(list);
            //DataList = Impl.DataList;
            //DataIndex = Impl.DataIndex;
            //Select(0);
        }

        static public async Task<bool> LoadAsync(SettingInfo info)
        {
            var result = await Impl.LoadAsync(info);
            if (result)
            {
                Data = info;
            }
            return result;
        }

        static public void Select(int idx)
        {
            /*
            if (DataList.Count > idx)
            {
                DataIndex.Value = idx;
                Data = DataList[idx];
            }
            */
        }
    }

    public class SettingsImpl : BindableBase, IDisposable
    {
        //public ReactiveCollection<SettingInfo> DataList { get; set; }
        //public ReactivePropertySlim<int> DataIndex { get; set; }
        private JsonSerializerOptions jsonOptions;

        public SettingsImpl()
        {
            //DataList = new ReactiveCollection<SettingInfo>();
            //DataList.AddTo(Disposables);
            //DataIndex = new ReactivePropertySlim<int>(0);
            //DataIndex.AddTo(Disposables);

            //Load();
        }

        public async Task InitAsync(ReactiveCollection<SettingInfo> list)
        {
            // JSON読み込みオプション
            jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                //NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };

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
                        if (Path.GetExtension(file) == ".json")
                        {
                            // 
                            var info = new SettingInfo
                            {
                                FilePath = file
                            };
                            await InitSettingFileAsync(file, info);
                            list.Add(info);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.AddException(e, $"json解析エラー: in file {file}");
                    }
                }
            }
        }

        private async Task InitSettingFileAsync(string path, SettingInfo info)
        {
            // jsonファイル解析
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // jsonファイルパース
                var json = await JsonSerializer.DeserializeAsync<Json.Settings>(stream, jsonOptions);
                // json読み込み
                InitSetting(json, info);
            }
        }

        private void InitSetting(Json.Settings json, SettingInfo info)
        {
            // 設定ファイル情報
            info.Name = json.Name;
            // ログ設定
            info.Log.AnalyzeJson(json.Log);
        }

        public async Task<bool> LoadAsync(SettingInfo info)
        {
            try
            {
                // jsonファイル解析
                using (var stream = new FileStream(info.FilePath, FileMode.Open, FileAccess.Read))
                {
                    // jsonファイルパース
                    var json = await JsonSerializer.DeserializeAsync<Json.Settings>(stream, jsonOptions);
                    // json読み込み
                    await MakeSettingAsync(json, info);
                }
                //
                info.IsLoaded = true;
                return true;
            }
            catch (Exception ex)
            {
                info.Comm.Clear();
                Logger.AddException(ex, "設定ファイル読み込みエラー:");
            }
            return false;
        }

        private async Task MakeSettingAsync(Json.Settings json, SettingInfo info)
        {
            // Script
            // 最初にjsをロードしておく
            await info.Script.AnalyzeJsonAsync(json.Script);
            // Output
            info.Output.AnalyzeJson(json.Output);
            // GUI
            info.Gui.AnalyzeJson(json.Gui);
            // Serial
            info.Serial.AnalyzeJson(json.Serial);
            // Comm
            await info.Comm.AnalyzeJsonAsync(json.Comm);
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

            // Log設定
            [JsonPropertyName("log")]
            public Log Log { get; set; }

            // Output設定
            [JsonPropertyName("output")]
            public Output Output { get; set; }

            // GUI設定
            [JsonPropertyName("gui")]
            public Gui Gui { get; set; }

            // COMポート設定
            [JsonPropertyName("serial")]
            public Serial Serial { get; set; }

            // 通信フレーム設定
            [JsonPropertyName("comm")]
            public Comm Comm { get; set; }

            // jsファイル
            [JsonPropertyName("script")]
            public Script Script { get; set; }
        }

    }

}
