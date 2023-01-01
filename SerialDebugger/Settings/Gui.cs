using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerialDebugger.Settings
{
    class Gui
    {
        public enum Col
        {
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

        public int[] ColOrder { get; }
        public int[] ColWidth { get; }

        public Gui()
        {
            // 初期値を入れておく
            ColOrder = new int[(int)Col.Size] { 0, 1, 2, 3, 4, 5, 6, 7, };
            ColWidth = new int[(int)Col.Size] { 25, 25, 40, 80, 80, 50, 10, 80 };
        }
        
        public void AnalyzeJson(Json.Gui json)
        {
            if (!(json.ColOrder is null))
            {
                ColOrder[(int)Col.ByteIndex] = json.ColOrder.ByteIndex;
                ColOrder[(int)Col.BitIndex] = json.ColOrder.BitIndex;
                ColOrder[(int)Col.FieldValue] = json.ColOrder.FieldValue;
                ColOrder[(int)Col.FieldName] = json.ColOrder.FieldName;
                ColOrder[(int)Col.FieldInput] = json.ColOrder.FieldInput;
                ColOrder[(int)Col.TxBytes] = json.ColOrder.TxBytes;
                ColOrder[(int)Col.Spacer] = json.ColOrder.Spacer;
                ColOrder[(int)Col.TxBuffer] = json.ColOrder.TxBuffer;
            }
            if (!(json.ColWidth is null))
            {
                ColWidth[(int)Col.ByteIndex] = json.ColWidth.ByteIndex;
                ColWidth[(int)Col.BitIndex] = json.ColWidth.BitIndex;
                ColWidth[(int)Col.FieldValue] = json.ColWidth.FieldValue;
                ColWidth[(int)Col.FieldName] = json.ColWidth.FieldName;
                ColWidth[(int)Col.FieldInput] = json.ColWidth.FieldInput;
                ColWidth[(int)Col.TxBytes] = json.ColWidth.TxBytes;
                ColWidth[(int)Col.Spacer] = json.ColWidth.Spacer;
                ColWidth[(int)Col.TxBuffer] = json.ColWidth.TxBuffer;
            }
        }

    }

    partial class Json
    {
        public class Gui
        {
            // 列並び
            [JsonPropertyName("column_order")]
            public GuiCol ColOrder { get; set; }

            // 列並び
            [JsonPropertyName("column_width")]
            public GuiCol ColWidth { get; set; }
        }

        public class GuiCol
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
