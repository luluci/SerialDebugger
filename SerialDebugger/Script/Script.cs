using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Script
{
    using Microsoft.Web.WebView2.Core;
    using Microsoft.Web.WebView2.WinForms;
    using System.Runtime.InteropServices;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Unicode;

    using Logger = Log.Log;

    public static class Interpreter
    {
        public static EngineWebView2 Engine;

        static public async Task Init()
        {
            string rootPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            CoreWebView2Environment.SetLoaderDllFolderPath(rootPath);
            Engine = new EngineWebView2();
            await Engine.Init();
        }
        
    }

    public class EngineWebView2
    {
        // WebView2
        public WebView2 wv;
        // json
        JsonSerializerOptions json_opt;
        // WebView2用通信操作I/F
        public CommIf Comm { get; set; }

        // Load済みScriptDict
        Dictionary<string, bool> LoadedScript;

        public EngineWebView2()
        {
            string rootPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string SettingPath = rootPath + @"\Script";
            wv = new WebView2
            {
                Source = new Uri($@"{SettingPath}\index.html"),
            };

            json_opt = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                //NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };

            //
            Comm = new CommIf();
            //
            LoadedScript = new Dictionary<string, bool>();
        }

        public async Task Init()
        {
            // WebView2初期化
            await wv.EnsureCoreWebView2Async();

            // ツール側インターフェース登録
            wv.CoreWebView2.AddHostObjectToScript("Comm", Comm);
            // Delayを入れないと次のExecuteScriptAsyncが失敗する
            await Task.Delay(10);
            //
            int limit = 0;
            while (await wv.ExecuteScriptAsync("Comm_Loaded()") != "true")
            {
                limit++;
                if (limit > 50)
                {
                    throw new Exception("WebView2の初期化に失敗したようです。");
                }
                await Task.Delay(100);
            }

            //
            wv.CoreWebView2.AddHostObjectToScript("evalExecResult", evalExecResult);

            // JavaScript側からの呼び出し
            wv.WebMessageReceived += webView_WebMessageReceived;


        }

        public async Task LoadSettingsScript(List<string> scripts)
        {
            foreach (var script in scripts)
            {
                if (!LoadedScript.TryGetValue(script, out bool value))
                {
                    var load_script = $@"
(() => {{
    var sc = document.createElement('script');
    sc.src = '../Settings/{script}';
    document.body.appendChild(sc);
    return true;
}})();
";
                    var result = await wv.CoreWebView2.ExecuteScriptAsync(load_script);
                    if (result == "true")
                    {
                        LoadedScript.Add(script, true);
                    }
                    else
                    {
                        Logger.Add($"script: {script}の読み込みでエラーが発生しました。");
                    }
                }
            }
        }


        private void webView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            var s = e.TryGetWebMessageAsString();
            //MessageBox.Show(s);
        }


        [ClassInterface(ClassInterfaceType.AutoDual)]
        [ComVisible(true)]
        public class EvalExecResult
        {
            [System.Runtime.CompilerServices.IndexerName("Items")]
            public string this[int index]
            {
                get { return list[index]; }
                set { list[index] = value; }
            }
            public Dictionary<int, string> list = new Dictionary<int, string>();

            public Int64 Key { get; set; }
            public string Value { get; set; }

            public void result(Int64 key, string value)
            {
                this.Key = key;
                this.Value = value;
            }

            public void Test(string value)
            {
                this.Value = value;
            }
        };
        public EvalExecResult evalExecResult = new EvalExecResult();

        public async Task EvalTest()
        {
            evalExecResult.Value = "test_setted";
            await wv.CoreWebView2.ExecuteScriptAsync($@"
                var eval_test = (i) => {{
                    const test = chrome.webview.hostObjects.sync.evalExecResult;
                    test.Key = 55;
                    test.Value = 'test';
                    test[100] = 'test 100';
                    test.Test('test_');
                    return test.Value;
                }}
            ");
            var result = await wv.CoreWebView2.ExecuteScriptAsync($"eval_test(0)");
            return;
        }


        public async Task EvalInit(string script)
        {
            await wv.ExecuteScriptAsync($@"
                var eval_exec_func = (i) => {{
                    {script};
                    return {{key: key, value: value}};
                }}
            ");
        }

        public async Task<(Int64, string)> EvalExec(int i)
        {
            try
            {
                var json = await wv.ExecuteScriptAsync($"eval_exec_func({i})");
                var value = JsonSerializer.Deserialize<Json.EvalExecResult>(json, json_opt);
                return (value.Key, value.Value);
            }
            catch
            {
                return (0, null);
            }
        }

        public async Task<(Int64, string)> Call(string script)
        {
            try
            {
                var json = await wv.ExecuteScriptAsync(script);
                var value = JsonSerializer.Deserialize<Json.EvalExecResult>(json, json_opt);
                return (value.Key, value.Value);
            }
            catch
            {
                return (0, null);
            }
        }



    }


    public class Json
    {
        public class EvalExecResult
        {
            [JsonPropertyName("key")]
            public Int64 Key { get; set; }

            [JsonPropertyName("value")]
            public string Value { get; set; }
        }
    }

}
