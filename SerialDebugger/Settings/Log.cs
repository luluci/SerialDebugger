using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerialDebugger.Settings
{
    public class Log
    {
        public string Directory { get; set; }
        public int MaxSize { get; set; } = 100;
        public bool OutputFile { get; set; } = false;

        public Log()
        {

        }

        public void AnalyzeJson(Json.Log json)
        {
            // 設定なしの場合は初期値を使う
            if (json is null)
            {
                return;
            }

            if (!Object.ReferenceEquals(json.Directory, string.Empty))
            {
                Directory = json.Directory;
                OutputFile = true;
            }

            MaxSize = json.MaxSize;
        }
    }


    public partial class Json
    {
        public class Log
        {
            // ログ保存ディレクトリ
            [JsonPropertyName("directory")]
            public string Directory { get; set; } = string.Empty;

            // ログGUI上最大サイズ
            [JsonPropertyName("max_size")]
            public int MaxSize { get; set; } = 100;
        }

    }
}
