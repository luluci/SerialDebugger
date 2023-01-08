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

    public static class Interpreter
    {
        public static EngineWebView2 Engine;

        static public async Task Init()
        {
            Engine = new EngineWebView2();
            await Engine.Init();
        }
        
    }

    public class EngineWebView2
    {
        [ClassInterface(ClassInterfaceType.AutoDual)]
        [ComVisible(true)]
        public class EvalExecResult
        {
            public UInt64 key;
            public string value;

            public void result(UInt64 key, string value)
            {
                this.key = key;
                this.value = value;
            }
        };
        public EvalExecResult evalExecResult = new EvalExecResult();

        // WebView2
        public WebView2 wv;
        // json
        JsonSerializerOptions json_opt;

        public EngineWebView2()
        {
            string rootPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string SettingPath = rootPath + @"\Script";
            wv = new WebView2
            {
                Source = new Uri($"{SettingPath}/index.html"),
            };

            json_opt = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                //NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };
        }

        public async Task Init()
        {
            await wv.EnsureCoreWebView2Async();

            //
            wv.CoreWebView2.AddHostObjectToScript("evalExecResult", evalExecResult);

            // JavaScript側からの呼び出し
            wv.WebMessageReceived += webView_WebMessageReceived;
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

        public async Task<(UInt64, string)> EvalExec(int i)
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

        public async Task<(UInt64, string)> Call(string script)
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


        private void webView_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            var s = e.TryGetWebMessageAsString();
            //MessageBox.Show(s);
        }

    }


    public class Json
    {
        public class EvalExecResult
        {
            [JsonPropertyName("key")]
            public UInt64 Key { get; set; }

            [JsonPropertyName("value")]
            public string Value { get; set; }
        }
    }

}
