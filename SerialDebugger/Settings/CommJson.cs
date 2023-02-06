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

        public class Comm
        {
            // 送信設定
            [JsonPropertyName("tx")]
            public CommTx Tx { get; set; }

            // 受信解析設定
            [JsonPropertyName("rx")]
            public CommRx Rx { get; set; }

            // 自動送信
            [JsonPropertyName("auto_tx")]
            public CommAutoTx AutoTx { get; set; }

            // rx_autoresp
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
            [JsonPropertyName("tx_frame_name")]
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
            [JsonPropertyName("auto_tx_job_name")]
            public string AutoTxJobName { get; set; } = string.Empty;

            [JsonPropertyName("rx_pattern_name")]
            public string RxPatternName { get; set; } = string.Empty;

            [JsonPropertyName("state")]
            public bool State { get; set; } = true;


            // 0個から受理, 0個指定でany
            [JsonPropertyName("rx_pattern_names")]
            public IList<string> RxPatternNames { get; set; }


            [JsonPropertyName("script")]
            public string Script { get; set; } = string.Empty;

        }



        public class CommRx
        {
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

            [JsonPropertyName("value")]
            public int Value { get; set; } = -1;

            [JsonPropertyName("msec")]
            public int Msec { get; set; } = -1;

            [JsonPropertyName("script")]
            public string Script { get; set; } = string.Empty;
        }



        public class CommTx
        {
            [JsonPropertyName("frames")]
            public IList<CommTxFrame> Frames { get; set; }
        }

        public class CommTxFrame
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("fields")]
            public IList<CommField> Fields { get; set; }

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
            public UInt64 Value { get; set; } = 0;

            [JsonPropertyName("base")]
            public int Base { get; set; } = 16;

            [JsonPropertyName("min")]
            public UInt64 Min { get; set; } = UInt64.MaxValue;

            [JsonPropertyName("max")]
            public UInt64 Max { get; set; } = UInt64.MinValue;



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
            public UInt64 ValueMin { get; set; } = 0;

            [JsonPropertyName("format")]
            public string Format { get; set; } = string.Empty;
        }

        public class CommFieldDict
        {
            [JsonPropertyName("value")]
            public UInt64 Value { get; set; } = 0;

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
            public UInt64 ValueMin { get; set; } = 0;
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
            public int End { get; set; } = 0;

            [JsonPropertyName("method")]
            public string Method { get; set; } = string.Empty;
        }

        public class CommTxBackupBuffer
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("value")]
            public IList<UInt64> Values { get; set; }
        }

    }
}
