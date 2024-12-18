﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerialDebugger.Settings
{
    partial class Json
    {

        public class Comm
        {
            [JsonPropertyName("display_id")]
            public bool DisplayId { get; set; } = false;

            // 送信設定
            [JsonPropertyName("tx")]
            public CommTx Tx { get; set; }

            // 受信解析設定
            [JsonPropertyName("rx")]
            public CommRx Rx { get; set; }

            // 自動送信
            [JsonPropertyName("auto_tx")]
            public CommAutoTx AutoTx { get; set; }
        }

        public class CommAutoTx
        {
            [JsonPropertyName("jobs")]
            public IList<CommAutoTxJob> Jobs { get; set; }
        }

        public class CommAutoTxJob
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("alias")]
            public string Alias { get; set; } = string.Empty;

            [JsonPropertyName("active")]
            public bool Active { get; set; } = false;

            [JsonPropertyName("editable")]
            public bool Editable { get; set; } = false;

            [JsonPropertyName("actions")]
            public IList<CommAutoTxAction> Actions { get; set; }
        }

        public class CommAutoTxAction
        {
            [JsonPropertyName("alias")]
            public string Alias { get; set; } = string.Empty;

            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("immediate")]
            public bool Immediate { get; set; } = false;

            // Tx送信
            [JsonPropertyName("tx_frame")]
            public string TxFrameName { get; set; } = string.Empty;

            [JsonPropertyName("tx_frame_buff_index")]
            public int TxFrameBuffIndex { get; set; } = 0;

            [JsonPropertyName("tx_frame_buff_offset")]
            public int TxFrameBuffOffset { get; set; } = -1;

            [JsonPropertyName("tx_frame_buff_length")]
            public int TxFrameBuffLength { get; set; } = -1;

            // Wait
            [JsonPropertyName("wait_time")]
            public int WaitTime { get; set; } = -1;

            // AutoTx Jump
            [JsonPropertyName("jump_to")]
            public int JumpTo { get; set; } = -1;

            // Activate
            [JsonPropertyName("auto_tx_job")]
            public string AutoTxJobName { get; set; } = string.Empty;

            [JsonPropertyName("rx_pattern")]
            public string RxPatternName { get; set; } = string.Empty;

            [JsonPropertyName("state")]
            public bool State { get; set; } = true;

            // Log
            [JsonPropertyName("log")]
            public string Log { get; set; } = string.Empty;


            // 0個から受理, 0個指定でany
            [JsonPropertyName("rx_patterns")]
            public IList<string> RxPatternNames { get; set; }

            // Script
            // 
            [JsonPropertyName("auto_tx_handler")]
            public string AutoTxHandler { get; set; } = string.Empty;
            // 受信イベントハンドラ
            [JsonPropertyName("rx_handler")]
            public string RxHandler { get; set; } = string.Empty;

        }



        public class CommRx
        {
            [JsonPropertyName("invert_bit")]
            public bool InvertBit { get; set; } = false;

            [JsonPropertyName("bit_order")]
            public string BitOrder { get; set; } = string.Empty;

            [JsonPropertyName("enable_multi_match")]
            public bool MultiMatch { get; set; } = false;

            [JsonPropertyName("frames")]
            public IList<CommRxFrame> Frames { get; set; }
        }

        public class CommRxFrame
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("fields")]
            public IList<CommField> Fields { get; set; }

            [JsonPropertyName("patterns")]
            public IList<CommRxPattern> Patterns { get; set; }

        }

        public class CommRxPattern
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("active")]
            public bool Active { get; set; } = false;

            [JsonPropertyName("log_visualize")]
            public bool LogVisualize { get; set; } = false;

            [JsonPropertyName("matches")]
            public IList<CommRxMatch> Matches { get; set; }
        }

        public class CommRxMatch
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            // PatternMatch
            [JsonPropertyName("value")]
            public Int64 Value { get; set; } = Int64.MinValue;

            // Timeout
            [JsonPropertyName("msec")]
            public int Msec { get; set; } = -1;

            // Script
            [JsonPropertyName("rx_begin")]
            public string RxBegin { get; set; } = string.Empty;

            [JsonPropertyName("rx_recieved")]
            public string RxRecieved { get; set; } = string.Empty;

            // Activate
            [JsonPropertyName("auto_tx_job")]
            public string AutoTxJobName { get; set; } = string.Empty;

            [JsonPropertyName("rx_pattern")]
            public string RxPatternName { get; set; } = string.Empty;

            [JsonPropertyName("state")]
            public bool State { get; set; } = true;

        }



        public class CommTx
        {
            [JsonPropertyName("invert_bit")]
            public bool InvertBit { get; set; } = false;

            [JsonPropertyName("bit_order")]
            public string BitOrder { get; set; } = string.Empty;

            [JsonPropertyName("frames")]
            public IList<CommTxFrame> Frames { get; set; }
        }

        public class CommTxFrame
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("as_ascii")]
            public bool AsAscii { get; set; } = false;

            [JsonPropertyName("log_visualize")]
            public bool LogVisualize { get; set; } = false;

            [JsonPropertyName("fields")]
            public IList<CommField> Fields { get; set; }

            [JsonPropertyName("groups")]
            public IList<CommGroup> Groups { get; set; }

            [JsonPropertyName("backup_buffer_size")]
            public int BackupBufferSize { get; set; } = 0;

            [JsonPropertyName("backup_buffers")]
            public IList<CommTxBackupBuffer> BackupBuffers { get; set; }
        }

        public class CommField
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("multi_name")]
            public IList<CommFieldMultiName> MultiNames { get; set; }

            [JsonPropertyName("bit_size")]
            public int BitSize { get; set; } = 0;

            [JsonPropertyName("value")]
            public Int64 Value { get; set; } = 0;

            [JsonPropertyName("base")]
            public int Base { get; set; } = 16;

            [JsonPropertyName("min")]
            public Int64 Min { get; set; } = Int64.MaxValue;

            [JsonPropertyName("max")]
            public Int64 Max { get; set; } = Int64.MinValue;

            [JsonPropertyName("endian")]
            public string Endian { get; set; } = string.Empty;



            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("unit")]
            public CommFieldUnit Unit { get; set; }

            [JsonPropertyName("dict")]
            public IList<CommFieldDict> Dict { get; set; }

            [JsonPropertyName("time")]
            public CommFieldTime Time { get; set; }

            [JsonPropertyName("script")]
            public CommFieldScript Script { get; set; }

            [JsonPropertyName("checksum")]
            public CommFieldChecksum Checksum { get; set; }

            [JsonPropertyName("char")]
            public string Char { get; set; } = string.Empty;

            [JsonPropertyName("string")]
            public string String { get; set; } = string.Empty;

            // DragDrop個別設定
            [JsonPropertyName("drag_drop")]
            public OutputDragDrop DragDrop { get; set; }
        }

        public class CommFieldMultiName
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("bit_size")]
            public int BitSize { get; set; } = 0;
        }

        public class CommFieldUnit
        {
            [JsonPropertyName("unit")]
            public string Unit { get; set; } = string.Empty;

            [JsonPropertyName("lsb")]
            public double Lsb { get; set; } = 0;

            [JsonPropertyName("disp_max")]
            public double DispMax { get; set; } = 0;

            [JsonPropertyName("disp_min")]
            public double DispMin { get; set; } = 0;

            [JsonPropertyName("value_min")]
            public Int64 ValueMin { get; set; } = 0;

            [JsonPropertyName("format")]
            public string Format { get; set; } = string.Empty;
        }

        public class CommFieldDict
        {
            [JsonPropertyName("value")]
            public Int64 Value { get; set; } = 0;

            [JsonPropertyName("disp")]
            public string Disp { get; set; } = string.Empty;
        }

        public class CommFieldTime
        {
            [JsonPropertyName("elapse")]
            public double Elapse { get; set; } = 0;

            [JsonPropertyName("begin")]
            public string Begin { get; set; } = string.Empty;

            [JsonPropertyName("end")]
            public string End { get; set; } = string.Empty;

            [JsonPropertyName("value_min")]
            public Int64 ValueMin { get; set; } = 0;
        }

        public class CommFieldScript
        {
            [JsonPropertyName("mode")]
            public string Mode { get; set; } = string.Empty;

            [JsonPropertyName("count")]
            public int Count { get; set; } = 0;

            [JsonPropertyName("script")]
            public string Script { get; set; } = string.Empty;

        }

        public class CommFieldChecksum
        {
            [JsonPropertyName("begin")]
            public int Begin { get; set; } = 0;

            [JsonPropertyName("end")]
            public int End { get; set; } = -1;

            [JsonPropertyName("method")]
            public string Method { get; set; } = string.Empty;

            [JsonPropertyName("word_size")]
            public int WordSize { get; set; } = 1;

            [JsonPropertyName("word_endian")]
            public string WordEndian { get; set; } = string.Empty;

        }

        public class CommTxBackupBuffer
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("value")]
            public IList<Int64> Values { get; set; }

            [JsonPropertyName("value_ascii")]
            public string ValueAscii { get; set; } = string.Empty;
        }

        public class CommGroup
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("color")]
            public string Color { get; set; } = string.Empty;

            [JsonPropertyName("bgcolor")]
            public string BackgroundColor { get; set; } = string.Empty;

            [JsonPropertyName("begin")]
            public int Begin { get; set; } = 0;

            [JsonPropertyName("end")]
            public int End { get; set; } = -1;

            [JsonPropertyName("id_begin")]
            public int IdBegin { get; set; } = 0;

        }

    }
}
