using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Script
{
    using Logger = Log.Log;

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommTxFramesIf
    {
        public Serial.Protocol ProtocolRef { get; set; } = null;
        // Commデータへの参照
        // Comm: Tx
        public ReactiveCollection<SerialDebugger.Comm.TxFrame> TxFramesRef { get; set; }
        //
        public CommTxFieldBuffersIf CommTxBufferIf { get; set; } = new CommTxFieldBuffersIf();

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public CommTxFieldBuffersIf this[int frame_id]
        {
            get
            {
                return CommTxBufferIf.TxBuffer(TxFramesRef[frame_id].Buffers);
            }
        }

        public CommTxFramesIf TxFrames(ReactiveCollection<SerialDebugger.Comm.TxFrame> tx)
        {
            TxFramesRef = tx;
            return this;
        }

        public void Fix(int frame_id, int buffer_id)
        {
            TxFramesRef[frame_id].Buffers[buffer_id].BufferFix();
        }

        public bool Send(int frame_id, int buffer_id)
        {
            if (!ProtocolRef.IsSerialOpen)
            {
                return false;
            }

            var fb = ProtocolRef.GetTxBuffer(frame_id, buffer_id);
            string name = fb.Name;
            byte[] buff = fb.Data;
            // バッファ送信
            ProtocolRef.SendData(buff, 0, buff.Length);
            // Log出力
            Logger.Add($"[Tx][{name}] {Logger.Byte2Str(buff, 0, buff.Length)}");

            return true;
        }

        public bool SendPart(int frame_id, int buffer_id, int offset, int length)
        {
            if (!ProtocolRef.IsSerialOpen)
            {
                return false;
            }

            var fb = ProtocolRef.GetTxBuffer(frame_id, buffer_id);
            string name = fb.Name;
            byte[] buff = fb.Data;
            // バッファ送信
            ProtocolRef.SendData(buff, offset, length);
            // Log出力
            Logger.Add($"[Tx][{name}] {Logger.Byte2Str(buff, offset, length)}");

            return true;
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommTxFieldBuffersIf
    {
        // Commデータへの参照
        // Comm: Tx
        public ReactiveCollection<SerialDebugger.Comm.TxFieldBuffer> TxFieldBuffersRef { get; set; }
        //
        public CommTxFieldBufferIf CommTxFieldBufferIf { get; set; } = new CommTxFieldBufferIf();

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public CommTxFieldBufferIf this[int buffer_id]
        {
            get
            {
                return CommTxFieldBufferIf.TxFieldBuffer(TxFieldBuffersRef[buffer_id]);
            }
        }

        public CommTxFieldBuffersIf TxBuffer(ReactiveCollection<SerialDebugger.Comm.TxFieldBuffer> tx)
        {
            TxFieldBuffersRef = tx;
            return this;
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommTxFieldBufferIf
    {
        // Commデータへの参照
        // Comm: Tx
        public SerialDebugger.Comm.TxFieldBuffer TxFieldBufferRef { get; set; }

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public Int64 this[int field_id]
        {
            get
            {
                return TxFieldBufferRef.FieldValues[field_id].Value.Value;
            }
            set
            {
                var field = TxFieldBufferRef.FieldValues[field_id].FieldRef;
                TxFieldBufferRef.FieldValues[field_id].Value.Value = field.LimitValue(value);
            }
        }

        public CommTxFieldBufferIf TxFieldBuffer(SerialDebugger.Comm.TxFieldBuffer tx)
        {
            TxFieldBufferRef = tx;
            return this;
        }
    }
}
