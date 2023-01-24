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

            [JsonPropertyName("active")]
            public bool Active { get; set; } = false;

            [JsonPropertyName("actions")]
            public IList<CommAutoTxAction> Actions { get; set; }
        }

        public class CommAutoTxAction
        {
            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("delay_log")]
            public bool DelayLog { get; set; } = false;


            [JsonPropertyName("tx_frame_name")]
            public string TxFrameName { get; set; } = string.Empty;

            [JsonPropertyName("tx_frame_buff_index")]
            public int TxFrameBuffIndex { get; set; } = -1;

            [JsonPropertyName("tx_frame_buff_offset")]
            public int TxFrameBuffOffset { get; set; } = -1;

            [JsonPropertyName("tx_frame_buff_length")]
            public int TxFrameBuffLength { get; set; } = -1;


            [JsonPropertyName("wait_time")]
            public int WaitTime { get; set; } = -1;

            [JsonPropertyName("jump_to")]
            public int JumpTo { get; set; } = -1;


            // 0個から受理, 0個指定でany
            [JsonPropertyName("rx_frame_name")]
            public IList<string> RxName { get; set; }


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
            public IList<CommTxField> Fields { get; set; }

            [JsonPropertyName("backup_buffer_size")]
            public int BackupBufferSize { get; set; } = 0;

            [JsonPropertyName("backup_buffers")]
            public IList<CommTxBackupBuffer> BackupBuffers { get; set; }
        }

        public class CommTxField
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("multi_name")]
            public IList<CommTxFieldMultiName> MultiNames { get; set; }

            [JsonPropertyName("bit_size")]
            public int BitSize { get; set; } = 0;

            [JsonPropertyName("value")]
            public UInt64 Value { get; set; } = 0;

            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("unit")]
            public CommTxFieldUnit Unit { get; set; }

            [JsonPropertyName("dict")]
            public IList<CommTxFieldDict> Dict { get; set; }

            [JsonPropertyName("time")]
            public CommTxFieldTime Time { get; set; }

            [JsonPropertyName("script")]
            public CommTxFieldScript Script { get; set; }

            [JsonPropertyName("checksum")]
            public CommTxFieldChecksum Checksum { get; set; }
        }

        public class CommTxFieldMultiName
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("bit_size")]
            public int BitSize { get; set; } = 0;
        }

        public class CommTxFieldUnit
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

        public class CommTxFieldDict
        {
            [JsonPropertyName("value")]
            public UInt64 Value { get; set; } = 0;

            [JsonPropertyName("disp")]
            public string Disp { get; set; } = string.Empty;
        }

        public class CommTxFieldTime
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

        public class CommTxFieldScript
        {
            [JsonPropertyName("mode")]
            public string Mode { get; set; } = string.Empty;

            [JsonPropertyName("count")]
            public int Count { get; set; } = 0;

            [JsonPropertyName("script")]
            public string Script { get; set; } = string.Empty;

        }

        public class CommTxFieldChecksum
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
