using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerialDebugger.Settings
{
    class Output
    {
        // Value出力形式
        public enum DragDropValueFormat
        {
            Hex,        // hex出力
            Input,      // Input形式
        }
        public class DragDropNode
        {
            public string Begin { get; set; }
            public string End { get; set; }

            public DragDropNode()
            {
            }
        }
        public class DragDropInfo
        {
            public DragDropNode Body { get; set; }
            public DragDropNode Item { get; set; }
            public DragDropNode Name { get; set; }
            public DragDropNode Value { get; set; }
            public DragDropNode InnerName { get; set; }
            public DragDropNode InnerValue { get; set; }
            public DragDropValueFormat ValueFormat { get; set; } = DragDropValueFormat.Hex;

            public DragDropInfo()
            {

            }

            public void AnalyzeJson(Json.OutputDragDrop json)
            {
                // Body修飾
                if (!(json.BodyBegin is null) || !(json.BodyEnd is null))
                {
                    Body = new DragDropNode();
                }
                if (!(json.BodyBegin is null))
                {
                    Body.Begin = json.BodyBegin;
                }
                if (!(json.BodyEnd is null))
                {
                    Body.End = json.BodyEnd;
                }
                // Item修飾
                if (!(json.ItemBegin is null) || !(json.ItemEnd is null))
                {
                    Item = new DragDropNode();
                }
                if (!(json.ItemBegin is null))
                {
                    Item.Begin = json.ItemBegin;
                }
                if (!(json.ItemEnd is null))
                {
                    Item.End = json.ItemEnd;
                }
                // Name修飾
                if (!(json.NameBegin is null) || !(json.NameEnd is null))
                {
                    Name = new DragDropNode();
                }
                if (!(json.NameBegin is null))
                {
                    Name.Begin = json.NameBegin;
                }
                if (!(json.NameEnd is null))
                {
                    Name.End = json.NameEnd;
                }
                // Value修飾
                if (!(json.ValueBegin is null) || !(json.ValueEnd is null))
                {
                    Value = new DragDropNode();
                }
                if (!(json.ValueBegin is null))
                {
                    Value.Begin = json.ValueBegin;
                }
                if (!(json.ValueEnd is null))
                {
                    Value.End = json.ValueEnd;
                }
                // InnerName修飾
                if (!(json.InnerNameBegin is null) || !(json.InnerNameEnd is null))
                {
                    InnerName = new DragDropNode();
                }
                if (!(json.InnerNameBegin is null))
                {
                    InnerName.Begin = json.InnerNameBegin;
                }
                if (!(json.InnerNameEnd is null))
                {
                    InnerName.End = json.InnerNameEnd;
                }
                // InnerValue修飾
                if (!(json.ValueBegin is null) || !(json.ValueEnd is null))
                {
                    InnerValue = new DragDropNode();
                }
                if (!(json.ValueBegin is null))
                {
                    InnerValue.Begin = json.InnerValueBegin;
                }
                if (!(json.ValueEnd is null))
                {
                    InnerValue.End = json.InnerValueEnd;
                }
                // ValueFormat
                if (!(json.ValueFormat is null))
                {
                    switch (json.ValueFormat)
                    {
                        case "Input":
                            ValueFormat = DragDropValueFormat.Input;
                            break;

                        default:
                            ValueFormat = DragDropValueFormat.Hex;
                            break;
                    }
                }
            }

        }


        public DragDropInfo DragDrop { get; set; }

        public void AnalyzeJson(Json.Output json)
        {
            if (json is null)
            {
                return;
            }

            if (!(json.DragDrop is null))
            {
                DragDrop = new DragDropInfo();
                DragDrop.AnalyzeJson(json.DragDrop);
            }
        }

    }


    partial class Json
    {
        public class Output
        {
            // Window設定
            [JsonPropertyName("drag_drop")]
            public OutputDragDrop DragDrop { get; set; }
        }

        public class OutputDragDrop
        {
            [JsonPropertyName("body_begin")]
            public string BodyBegin { get; set; } = null;

            [JsonPropertyName("body_end")]
            public string BodyEnd { get; set; } = null;

            [JsonPropertyName("item_begin")]
            public string ItemBegin { get; set; } = null;

            [JsonPropertyName("item_end")]
            public string ItemEnd { get; set; } = null;

            [JsonPropertyName("name_begin")]
            public string NameBegin { get; set; } = null;

            [JsonPropertyName("name_end")]
            public string NameEnd { get; set; } = null;

            [JsonPropertyName("value_begin")]
            public string ValueBegin { get; set; } = null;

            [JsonPropertyName("value_end")]
            public string ValueEnd { get; set; } = null;

            [JsonPropertyName("inner_name_begin")]
            public string InnerNameBegin { get; set; } = null;

            [JsonPropertyName("inner_name_end")]
            public string InnerNameEnd { get; set; } = null;

            [JsonPropertyName("inner_value_begin")]
            public string InnerValueBegin { get; set; } = null;

            [JsonPropertyName("inner_value_end")]
            public string InnerValueEnd { get; set; } = null;

            [JsonPropertyName("value_format")]
            public string ValueFormat { get; set; } = null;
        }
    }

}
