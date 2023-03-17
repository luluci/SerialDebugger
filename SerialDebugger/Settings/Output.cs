using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerialDebugger.Settings
{
    public class Output
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
            public DragDropNode InnerFieldName { get; set; }
            public DragDropNode InnerFieldValue { get; set; }
            public DragDropValueFormat ValueFormat { get; set; } = DragDropValueFormat.Hex;

            // DragDrop操作有効フラグ
            // Body
            public bool EnableBodyBegin { get; set; } = false;
            public bool EnableBodyEnd { get; set; } = false;
            // Item
            public bool EnableItemBegin { get; set; } = false;
            public bool EnableItemEnd { get; set; } = false;
            // Frame
            public bool EnableFrame { get; set; } = false;
            public bool EnableFrameBegin { get; set; } = false;
            public bool EnableFrameEnd { get; set; } = false;
            // Field
            public bool EnableField { get; set; } = false;
            public bool EnableFieldName { get; set; } = false;
            public bool EnableFieldNameBegin { get; set; } = false;
            public bool EnableFieldNameEnd { get; set; } = false;
            public bool EnableFieldValue { get; set; } = false;
            public bool EnableFieldValueBegin { get; set; } = false;
            public bool EnableFieldValueEnd { get; set; } = false;
            // InnerField
            public bool EnableInnerField { get; set; } = false;
            public bool EnableInnerFieldName { get; set; } = false;
            public bool EnableInnerFieldNameBegin { get; set; } = false;
            public bool EnableInnerFieldNameEnd { get; set; } = false;
            public bool EnableInnerFieldValue { get; set; } = false;
            public bool EnableInnerFieldValueBegin { get; set; } = false;
            public bool EnableInnerFieldValueEnd { get; set; } = false;
            public DragDropInfo()
            {

            }
            
        }


        public DragDropInfo DragDrop { get; set; }

        public void AnalyzeJson(Json.Output json)
        {
            if (json is null)
            {
                DragDrop = new DragDropInfo();
                return;
            }

            DragDrop = MakeDragDropInfo(json.DragDrop);
            if (DragDrop is null)
            {
                DragDrop = new DragDropInfo();
            }
        }

        static public DragDropInfo MakeDragDropInfo(Json.OutputDragDrop json)
        {
            // 定義なしの場合は空Infoを返す
            if (json is null)
            {
                return null;
            }

            // 
            var dd = new DragDropInfo();
            // json解析
            // Body修飾
            if (!(json.Body is null))
            {
                dd.Body = new DragDropNode();

                if (!(json.Body.Begin is null))
                {
                    dd.Body.Begin = json.Body.Begin;
                    dd.EnableBodyBegin = true;
                }
                if (!(json.Body.End is null))
                {
                    dd.Body.End = json.Body.End;
                    dd.EnableBodyEnd = true;
                }
            }
            // Item修飾
            if (!(json.Item is null))
            {
                dd.Item = new DragDropNode();

                if (!(json.Item.Begin is null))
                {
                    dd.Item.Begin = json.Item.Begin;
                    dd.EnableItemBegin = true;
                }
                if (!(json.Item.End is null))
                {
                    dd.Item.End = json.Item.End;
                    dd.EnableItemEnd = true;
                }
            }
            // FrameName修飾
            if (!(json.FrameName is null))
            {
                dd.FrameName = new DragDropNode();
                dd.EnableFrame = true;

                if (!(json.FrameName.Begin is null))
                {
                    dd.FrameName.Begin = json.FrameName.Begin;
                    dd.EnableFrameBegin = true;
                }
                if (!(json.FrameName.End is null))
                {
                    dd.FrameName.End = json.FrameName.End;
                    dd.EnableFrameEnd = true;
                }
            }
            // FieldName修飾
            if (!(json.FieldName is null))
            {
                dd.FieldName = new DragDropNode();
                dd.EnableFieldName = true;

                if (!(json.FieldName.Begin is null))
                {
                    dd.FieldName.Begin = json.FieldName.Begin;
                    dd.EnableFieldNameBegin = true;
                }
                if (!(json.FieldName.End is null))
                {
                    dd.FieldName.End = json.FieldName.End;
                    dd.EnableFieldNameEnd = true;
                }
            }
            // FieldValue修飾
            if (!(json.FieldValue is null))
            {
                dd.FieldValue = new DragDropNode();
                dd.EnableFieldValue = true;

                if (!(json.FieldValue.Begin is null))
                {
                    dd.FieldValue.Begin = json.FieldValue.Begin;
                    dd.EnableFieldValueBegin = true;
                }
                if (!(json.FieldValue.End is null))
                {
                    dd.FieldValue.End = json.FieldValue.End;
                    dd.EnableFieldValueEnd = true;
                }
            }
            // Field修飾
            if (!(dd.FieldName is null) || !(dd.FieldValue is null))
            {
                dd.EnableField = true;
            }
            // InnerFieldName修飾
            if (!(json.InnerFieldName is null))
            {
                dd.InnerFieldName = new DragDropNode();
                dd.EnableInnerFieldName = true;

                if (!(json.InnerFieldName.Begin is null))
                {
                    dd.InnerFieldName.Begin = json.InnerFieldName.Begin;
                    dd.EnableInnerFieldNameBegin = true;
                }
                if (!(json.InnerFieldName.End is null))
                {
                    dd.InnerFieldName.End = json.InnerFieldName.End;
                    dd.EnableInnerFieldNameEnd = true;
                }
            }
            // InnerFieldValue修飾
            if (!(json.InnerFieldValue is null))
            {
                dd.InnerFieldValue = new DragDropNode();
                dd.EnableInnerFieldValue = true;

                if (!(json.InnerFieldValue.Begin is null))
                {
                    dd.InnerFieldValue.Begin = json.InnerFieldValue.Begin;
                    dd.EnableInnerFieldValueBegin = true;
                }
                if (!(json.InnerFieldValue.End is null))
                {
                    dd.InnerFieldValue.End = json.InnerFieldValue.End;
                    dd.EnableInnerFieldValueEnd = true;
                }
            }
            // InnerField修飾
            if (!(dd.InnerFieldName is null) || !(dd.InnerFieldValue is null))
            {
                dd.EnableInnerField = true;
            }
            // ValueFormat
            if (!(json.ValueFormat is null))
            {
                switch (json.ValueFormat)
                {
                    case "Input":
                        dd.ValueFormat = DragDropValueFormat.Input;
                        break;

                    default:
                        dd.ValueFormat = DragDropValueFormat.Hex;
                        break;
                }
            }

            return dd;
        }
    }

}
