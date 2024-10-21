using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Script
{
    using Microsoft.Web.WebView2.Core;
    using Microsoft.Web.WebView2.Wpf;
    using System.Runtime.InteropServices;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Unicode;
    using System.Windows.Forms;
    using System.Reactive.Disposables;
    using Reactive.Bindings;
    using Reactive.Bindings.Extensions;

    using Logger = Log.Log;
    using Utility;

    static class Interpreter
    {
        public static EngineWebView2 Engine;

        static public async Task Init(MainWindowViewModel tool)
        {
            // WebView2インスタンスの初期化前に実施する
            // dllをexeファイル内に取り込むのと相性が悪い。
            // WebView2Loader.dllの場所を明示する。
            string rootPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            CoreWebView2Environment.SetLoaderDllFolderPath(rootPath);
            // WebView2表示ウインドウ初期化
            Engine = new EngineWebView2();
            await Engine.Init(tool);
        }

        static public void ChangeSettingFile(Settings.SettingInfo data)
        {
            Script.Interpreter.Engine.ChangeSettingFile(data);
        }

        static public void UpdateProtocol(Serial.Protocol protocol)
        {
            Engine.UpdateProtocol(protocol);
        }

        static public IDisposable GetDisposable()
        {
            return Engine;
        }
    }

    class EngineWebView2 : BindableBase, IDisposable
    {
        //
        public View View { get; set; }
        // WebView2
        public WebView2 WebView2;
        // json
        JsonSerializerOptions json_opt;
        // WebView2用通信操作I/F
        public WebView2Interface WebView2If { get; set; }

        //
        public bool NavigationCompleted { get; set; } = false;

        // Load済みScriptDict
        Dictionary<string, bool> LoadedScript;
        // 実行済みScript
        List<string> ExecedScript;

        public EngineWebView2()
        {
            View = new View();
            WebView2 = View.WebView2;

            // Microsoft.Web.WebView2.Wpf.WebView2 は画面表示を行わないと初期化が始まらず EnsureCoreWebView2Async() が終了しない。
            // ここで一度画面表示を行い、すぐ隠す。画面がちらつくので画面外で実施する。
            var dispHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            View.Top = dispHeight + 1;
            View.Show();
            View.Hide();

            // jsonパーサオプション初期化
            json_opt = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                //NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };

            //
            WebView2If = new WebView2Interface();

            //
            LoadedScript = new Dictionary<string, bool>();
            ExecedScript = new List<string>();
        }

        public async Task Init(MainWindowViewModel tool)
        {
            // 初期化完了ハンドラ登録
            WebView2.CoreWebView2InitializationCompleted += webView_CoreWebView2InitializationCompleted;
            WebView2.NavigationCompleted += webView_NavigationCompleted;
            // JavaScript側からの呼び出し
            WebView2.WebMessageReceived += webView_WebMessageReceived;

            //
            WebView2If.Init(tool, View);

            // WebView2初期化
            await WebView2.EnsureCoreWebView2Async();

            // 機能無効化設定
            // F5無効化が主目的
            //WebView2.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
        }

        public void UpdateProtocol(Serial.Protocol protocol)
        {
            WebView2If.UpdateProtocol(protocol);
        }

        public async Task Reset()
        {
            // settingファイル個別scriptをロードしていた場合、
            // WebView2をリセットして初期状態にする
            if (LoadedScript.Count > 0)
            {
                // スクリプト情報クリア
                LoadedScript.Clear();
                ExecedScript.Clear();
                WebView2If.IO.Reset();
                // WebView2リロード
                NavigationCompleted = false;
                WebView2.CoreWebView2.Reload();
                await WaitWebView2NavigationCompleted();
            }
        }

        public void ChangeSettingFile(Settings.SettingInfo data)
        {
            WebView2If.IO.Reset();
            WebView2If.Settings.Init();
            WebView2If.Comm.Init(data.Comm.Tx, data.Comm.Rx, data.Comm.AutoTx);
        }

        public void ShowView(MainWindow window)
        {
            if (!View.IsVisible)
            {
                View.Top = window.Top + 10;
                View.Show();
            }
        }

        public async Task<string> ExecuteScriptAsync(string script)
        {
            return await WebView2.ExecuteScriptAsync(script);
        }

        public async Task RunScriptLoaded(string handler)
        {
            int limit = 0;
            while (await WebView2.ExecuteScriptAsync(handler) != "true")
            {
                limit++;
                if (limit > 50)
                {
                    throw new Exception("WebView2の初期化に失敗したようです。");
                }
                await Task.Delay(100);
            }
        }

        public async Task LoadScriptFiles(List<string> scripts)
        {
            foreach (var script in scripts)
            {
                if (!LoadedScript.TryGetValue(script, out bool value))
                {
                    WebView2If.Settings.ScriptLoaded = false;
                    var result = await LoadScriptFile(script);
                    if (result)
                    {
                        // Script読み込み完了まで待機, 5秒でタイムアウト
                        for (int timeup = 0; !WebView2If.Settings.ScriptLoaded && timeup < 50; timeup++)
                        {
                            await Task.Delay(100);
                        }
                        // ロード済みファイルに登録
                        LoadedScript.Add(script, true);
                    }
                    else
                    {
                        Logger.Add($"script: {script}の読み込みでエラーが発生しました。");
                    }
                }
            }
        }

        public async Task ReloadScriptFiles()
        {
            // WebView2がリセットされたときに前状態を復旧するために
            // 読み込んでいたjsファイルを再読み込みする
            foreach (var script in LoadedScript)
            {
                WebView2If.Settings.ScriptLoaded = false;
                var result = await LoadScriptFile(script.Key);
                if (result)
                {
                    // Script読み込み完了まで待機, 5秒でタイムアウト
                    for (int timeup = 0; !WebView2If.Settings.ScriptLoaded && timeup < 50; timeup++)
                    {
                        await Task.Delay(100);
                    }
                }
                else
                {
                    Logger.Add($"script: {script}の読み込みでエラーが発生しました。");
                }
            }
        }

        public async Task<bool> LoadScriptFile(string script)
        {
            var load_script = $@"
(() => {{
    var sc = document.createElement('script');
    sc.src = '../Settings/{script}';
    sc.onload = () => {{
        Settings.ScriptLoaded = true;
    }};
    document.body.appendChild(sc);
    return true;
}})();
";
            var result = await WebView2.CoreWebView2.ExecuteScriptAsync(load_script);
            return result == "true";
        }

        public async Task<int> MakeFieldSelecter(Comm.Field field)
        {
            // field参照登録
            WebView2If.Settings.Init(field);
            // Script作成
            var script = MakeFieldSelecterScript(field.selecter);
            // Script実行
            await WebView2.ExecuteScriptAsync(script);
            //
            if (WebView2If.Settings.Field.Result)
            {
                return WebView2If.Settings.Field.SelectIndex;
            }
            else
            {
                throw new Exception(WebView2If.Settings.Field.Message);
            }
        }

        public string MakeFieldSelecterScript(Comm.Field.Selecter selecter)
        {
            string parts;
            switch (selecter.Mode)
            {
                case "Exec":
                    parts = $@"
const exec_func = (i) => {{
    let key; let value;
    {selecter.Script};
    return {{key: key, value: value}};
}}
MakeFieldExecScript(exec_func, {selecter.Count});
";
                    break;

                case "Call":
                    parts = $@"{selecter.Script}({selecter.Count})";
                    break;

                default:
                    throw new Exception("invalid Script Mode!");
            }

            string comm = $@"
(() => {{
    try {{
        {parts}
        Settings.Field.Result = true;
    }}
    catch (e) {{
        Settings.Field.Message = e.message;
        Settings.Field.Result = false;
    }}
}})()
";
            return comm;
        }

        public async Task ExecOnLoadAsync(string onload)
        {
            await WebView2.ExecuteScriptAsync(onload);
            ExecedScript.Add(onload);
        }
        public async Task ReExecOnLoadAsync()
        {
            foreach (var onload in ExecedScript)
            {
                await WebView2.ExecuteScriptAsync(onload);
            }
        }

        public async Task<bool> WaitWebView2NavigationCompleted()
        {
            // 暫定でC#側フラグで管理。C#側からリロード(Navigation)をかけたときしか使えない。
            // WebView2側でNavigationが起きたことを検出するためには、
            // 動的に変数を追加してC#側で例外を使って参照できるかどうかで判定する？

            // NavigationCompletedまで待機, 5秒でタイムアウト
            int timeup;
            int timeup_count = 50;
            for (timeup = 0; !NavigationCompleted && timeup < timeup_count; timeup++)
            {
                await Task.Delay(100);
            }
            //
            return timeup < timeup_count;
        }

        private void webView_CoreWebView2InitializationCompleted(object sender, EventArgs e)
        {
            // WebView2起動時の初期化完了後に1回だけコールされる
        }

        private async void webView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            //webView.CoreWebView2.PostWebMessageAsString("C#からのデータ送信");

            // 表示完了時にコールされる。F5による更新時にもコールされる。
            // このときObjectの登録等も初期化されるため、毎回登録する。
            // API登録
            // 登録するインスタンスのclassはアクセスレベルをpublicにしないとエラー
            //WebView2.CoreWebView2.AddHostObjectToScript("wri", EntryPoint);
            try
            {
                // リロード時向け対応
                WebView2If.IO.Reset();

                // ツール側インターフェース登録
                // Commオブジェクト登録
                WebView2.CoreWebView2.AddHostObjectToScript("SerialDebugger", WebView2If);
                WebView2.CoreWebView2.AddHostObjectToScript("Utility", WebView2If.Utility);
                WebView2.CoreWebView2.AddHostObjectToScript("Settings", WebView2If.Settings);
                WebView2.CoreWebView2.AddHostObjectToScript("Comm", WebView2If.Comm);
                WebView2.CoreWebView2.AddHostObjectToScript("IO", WebView2If.IO);
                // デフォルトスクリプトをLoad
                await RunScriptLoaded("csLoaded()");

                // 初期化完了を通知
                //string webview2_init_fin = $@"var WebView2_SerialDebugger_Interface_NavigationCompleted = 1;";
                //await WebView2.CoreWebView2.ExecuteScriptAsync(webview2_init_fin);

                // WebView2側でNavigationが発生したときのみ以下のリロードが発生する想定
                // Settingファイルスクリプトをリロード
                await ReloadScriptFiles();
                // OnLoadスクリプトを再実行
                await ReExecOnLoadAsync();

                //
                NavigationCompleted = true;
            }
            catch (Exception ex)
            {
                Logger.AddException(ex, "WebView2初期化処理エラー:");
            }
        }
        private void webView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            var s = e.TryGetWebMessageAsString();
            //MessageBox.Show(s);
        }

        public void Close()
        {
            (this as IDisposable)?.Dispose();
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
                    (View.DataContext as IDisposable)?.Dispose();
                    WebView2If.IO.Reset();
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
