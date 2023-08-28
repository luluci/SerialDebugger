using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerialDebugger.Settings
{
    class Script
    {
        public List<string> Import { get; set; } = new List<string>();
        public string OnLoad { get; set; } = string.Empty;
        public bool HasOnLoad { get; set; } = false;

        public Script()
        {

        }

        public async Task AnalyzeJsonAsync(Json.Script json)
        {
            if (!(json is null))
            {
                // List取り込み
                foreach (var file in json.Import)
                {
                    Import.Add(file);
                }
                // Commロード前にjsファイルをロード
                await SerialDebugger.Script.Interpreter.Engine.LoadScriptFile(Import);


                if (!Object.ReferenceEquals(json.OnLoad, string.Empty))
                {
                    OnLoad = json.OnLoad;
                    HasOnLoad = true;
                }

            }

        }
    }

    partial class Json
    {

        public class Script
        {
            // jsファイル
            [JsonPropertyName("import")]
            public IList<string> Import { get; set; }

            [JsonPropertyName("onload")]
            public string OnLoad { get; set; } = string.Empty;
        }

    }
}
