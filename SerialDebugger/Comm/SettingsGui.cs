using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace SerialDebugger.Comm
{
    using Logger = SerialDebugger.Log.Log;

    static class SettingsGui
    {
        public enum Col {
            ByteIndex,      // Byteインデックス表示列
            BitIndex,       // Bitインデックス表示列
            FieldValue,     // Field設定値表示列
            FieldName,      // Field名表示列
            FieldInput,     // Field値入力列
            TxBytes,        // 送信データシーケンス表示列
            Spacer,         // スペース列
            TxBuffer,       // 送信データバッファ

            //
            Size,           // 列数
        }

        public static SettingsGuiImpl Data { get; } = new SettingsGuiImpl();
    }

    public class SettingsGuiImpl
    {
        public int[] ColOrder;
        public int[] ColWidth;

        public SettingsGuiImpl()
        {
            // 初期値を入れておく
            ColOrder = new int[(int)SettingsGui.Col.Size] { 0, 1, 2, 3, 4, 5, 6, 7, };
            ColWidth = new int[(int)SettingsGui.Col.Size] { 25, 25, 40, 80, 80, 50, 10, 80 };
            // 設定ファイル読み込み
            Load();
        }

        private void Load()
        {
            // デフォルトパス
            string rootPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string SettingPath = rootPath + @"\Settings\gui.json";
            if (File.Exists(SettingPath))
            {
                // ファイルが存在すれば読み出し
                try
                {
                    var json = LoadJson(SettingPath);
                    AnalyzeJson(json);
                }
                catch (Exception e)
                {
                    Logger.Add($"json解析エラー: {e.InnerException.Message} : in file {SettingPath}\n");
                }
            }
            
        }

        private JsonCommGui LoadJson(string path)
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
                var json = JsonSerializer.DeserializeAsync<JsonCommGui>(stream, options).AsTask();
                json.Wait();
                return json.Result;
            }
        }

        private void AnalyzeJson(JsonCommGui json)
        {
            if (!(json.ColOrder is null))
            {
                ColOrder[(int)SettingsGui.Col.ByteIndex] = json.ColOrder.ByteIndex;
                ColOrder[(int)SettingsGui.Col.BitIndex] = json.ColOrder.BitIndex;
                ColOrder[(int)SettingsGui.Col.FieldValue] = json.ColOrder.FieldValue;
                ColOrder[(int)SettingsGui.Col.FieldName] = json.ColOrder.FieldName;
                ColOrder[(int)SettingsGui.Col.FieldInput] = json.ColOrder.FieldInput;
                ColOrder[(int)SettingsGui.Col.TxBytes] = json.ColOrder.TxBytes;
                ColOrder[(int)SettingsGui.Col.Spacer] = json.ColOrder.Spacer;
                ColOrder[(int)SettingsGui.Col.TxBuffer] = json.ColOrder.TxBuffer;
            }
            if (!(json.ColWidth is null))
            {
                ColWidth[(int)SettingsGui.Col.ByteIndex] = json.ColWidth.ByteIndex;
                ColWidth[(int)SettingsGui.Col.BitIndex] = json.ColWidth.BitIndex;
                ColWidth[(int)SettingsGui.Col.FieldValue] = json.ColWidth.FieldValue;
                ColWidth[(int)SettingsGui.Col.FieldName] = json.ColWidth.FieldName;
                ColWidth[(int)SettingsGui.Col.FieldInput] = json.ColWidth.FieldInput;
                ColWidth[(int)SettingsGui.Col.TxBytes] = json.ColWidth.TxBytes;
                ColWidth[(int)SettingsGui.Col.Spacer] = json.ColWidth.Spacer;
                ColWidth[(int)SettingsGui.Col.TxBuffer] = json.ColWidth.TxBuffer;
            }
        }


        public class JsonCommGui
        {
            // 列並び
            [JsonPropertyName("column_order")]
            public JsonGuiCol ColOrder { get; set; }

            // 列並び
            [JsonPropertyName("column_width")]
            public JsonGuiCol ColWidth { get; set; }
        }

        public class JsonGuiCol
        {
            [JsonPropertyName("byte_index")]
            public int ByteIndex { get; set; } = 0;

            [JsonPropertyName("bit_index")]
            public int BitIndex { get; set; } = 0;

            [JsonPropertyName("field_value")]
            public int FieldValue { get; set; } = 0;

            [JsonPropertyName("field_name")]
            public int FieldName { get; set; } = 0;

            [JsonPropertyName("field_input")]
            public int FieldInput { get; set; } = 0;

            [JsonPropertyName("tx_bytes")]
            public int TxBytes { get; set; } = 0;

            [JsonPropertyName("spacer")]
            public int Spacer { get; set; } = 0;

            [JsonPropertyName("tx_buffer")]
            public int TxBuffer { get; set; } = 0;
        }

    }
}
