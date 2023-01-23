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
            public DragDropNode FrameName { get; set; }
            public DragDropNode FieldName { get; set; }
            public DragDropNode FieldValue { get; set; }
            public DragDropNode FieldInnerName { get; set; }
            public DragDropNode FieldInnerValue { get; set; }
            public DragDropValueFormat ValueFormat { get; set; } = DragDropValueFormat.Hex;

            public DragDropInfo()
            {

            }

            public void AnalyzeJson(Json.OutputDragDrop json)
            {
                // Body修飾
                if (!(json.Body is null))
                {
                    Body = new DragDropNode();

                    if (!(json.Body.Begin is null))
                    {
                        Body.Begin = json.Body.Begin;
                    }
                    if (!(json.Body.End is null))
                    {
                        Body.End = json.Body.End;
                    }
                }
                // Item修飾
                if (!(json.Item is null))
                {
                    Item = new DragDropNode();

                    if (!(json.Item.Begin is null))
                    {
                        Item.Begin = json.Item.Begin;
                    }
                    if (!(json.Item.End is null))
                    {
                        Item.End = json.Item.End;
                    }
                }
                // FrameName修飾
                if (!(json.FrameName is null))
                {
                    FrameName = new DragDropNode();

                    if (!(json.FrameName.Begin is null))
                    {
                        FrameName.Begin = json.FrameName.Begin;
                    }
                    if (!(json.FrameName.End is null))
                    {
                        FrameName.End = json.FrameName.End;
                    }
                }
                // FieldName修飾
                if (!(json.FieldName is null))
                {
                    FieldName = new DragDropNode();

                    if (!(json.FieldName.Begin is null))
                    {
                        FieldName.Begin = json.FieldName.Begin;
                    }
                    if (!(json.FieldName.End is null))
                    {
                        FieldName.End = json.FieldName.End;
                    }
                }
                // Value修飾
                if (!(json.FieldValue is null))
                {
                    FieldValue = new DragDropNode();

                    if (!(json.FieldValue.Begin is null))
                    {
                        FieldValue.Begin = json.FieldValue.Begin;
                    }
                    if (!(json.FieldValue.End is null))
                    {
                        FieldValue.End = json.FieldValue.End;
                    }
                }
                // InnerName修飾
                if (!(json.FieldInnerName is null))
                {
                    FieldInnerName = new DragDropNode();

                    if (!(json.FieldInnerName.Begin is null))
                    {
                        FieldInnerName.Begin = json.FieldInnerName.Begin;
                    }
                    if (!(json.FieldInnerName.End is null))
                    {
                        FieldInnerName.End = json.FieldInnerName.End;
                    }
                }
                // InnerValue修飾
                if (!(json.FieldInnerValue is null))
                {
                    FieldInnerValue = new DragDropNode();

                    if (!(json.FieldInnerValue.Begin is null))
                    {
                        FieldInnerValue.Begin = json.FieldInnerValue.Begin;
                    }
                    if (!(json.FieldInnerValue.End is null))
                    {
                        FieldInnerValue.End = json.FieldInnerValue.End;
                    }
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

        public class OutputDragDropTag
        {
            [JsonPropertyName("begin")]
            public string Begin { get; set; } = null;

            [JsonPropertyName("end")]
            public string End { get; set; } = null;

        }

        public class OutputDragDrop
        {
            [JsonPropertyName("body")]
            public OutputDragDropTag Body { get; set; } = null;

            [JsonPropertyName("item")]
            public OutputDragDropTag Item { get; set; } = null;

            [JsonPropertyName("frame_name")]
            public OutputDragDropTag FrameName { get; set; } = null;

            [JsonPropertyName("field_name")]
            public OutputDragDropTag FieldName { get; set; } = null;

            [JsonPropertyName("field_value")]
            public OutputDragDropTag FieldValue { get; set; } = null;

            [JsonPropertyName("field_inner_name")]
            public OutputDragDropTag FieldInnerName { get; set; } = null;

            [JsonPropertyName("field_inner_value")]
            public OutputDragDropTag FieldInnerValue { get; set; } = null;
            
            [JsonPropertyName("value_format")]
            public string ValueFormat { get; set; } = null;
        }
    }

}
