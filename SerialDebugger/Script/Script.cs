﻿using System;
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
        public SettingsIf Settings { get; set; }

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
            Settings = new SettingsIf();
            //
            LoadedScript = new Dictionary<string, bool>();
        }

        public async Task Init()
        {
            // WebView2初期化
            await wv.EnsureCoreWebView2Async();

            // ツール側インターフェース登録
            // Commオブジェクト登録
            wv.CoreWebView2.AddHostObjectToScript("Comm", Comm);
            wv.CoreWebView2.AddHostObjectToScript("Settings", Settings);
            // デフォルトスクリプトをLoad
            await RunScriptLoaded("Comm_Loaded()");
            await RunScriptLoaded("Settings_Loaded()");
            
            // JavaScript側からの呼び出し
            wv.WebMessageReceived += webView_WebMessageReceived;
        }

        public async Task RunScriptLoaded(string handler)
        {
            int limit = 0;
            while (await wv.ExecuteScriptAsync(handler) != "true")
            {
                limit++;
                if (limit > 50)
                {
                    throw new Exception("WebView2の初期化に失敗したようです。");
                }
                await Task.Delay(100);
            }
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

        public async Task<int> MakeFieldSelecter(Comm.Field field)
        {
            // field参照登録
            Settings.Init(field);
            // Script作成
            var script = MakeFieldSelecterScript(field.selecter);
            // Script実行
            await wv.ExecuteScriptAsync(script);
            //
            if (Settings.Field.Result)
            {
                return Settings.Field.SelectIndex;
            }
            else
            {
                throw new Exception(Settings.Field.Message);
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


        private void webView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            var s = e.TryGetWebMessageAsString();
            //MessageBox.Show(s);
        }

    }
    
}
