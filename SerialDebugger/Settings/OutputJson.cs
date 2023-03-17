using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerialDebugger.Settings
{
    partial class Json
    {
        public class Output
        {
            // DragDrop共通設定
            [JsonPropertyName("drag_drop")]
            public OutputDragDrop DragDrop { get; set; }
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

            [JsonPropertyName("inner_field_name")]
            public OutputDragDropTag InnerFieldName { get; set; } = null;

            [JsonPropertyName("inner_field_value")]
            public OutputDragDropTag InnerFieldValue { get; set; } = null;

            [JsonPropertyName("value_format")]
            public string ValueFormat { get; set; } = null;
        }

        public class OutputDragDropTag
        {
            [JsonPropertyName("begin")]
            public string Begin { get; set; } = null;

            [JsonPropertyName("end")]
            public string End { get; set; } = null;

        }

    }
}
