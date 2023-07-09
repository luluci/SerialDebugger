using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Runtime.InteropServices;

namespace SerialDebugger.Script
{
    using Logger = Log.Log;

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommIf
    {
        // Commデータへの参照
        // Comm: Tx
        public ReactiveCollection<SerialDebugger.Comm.TxFrame> TxFramesRef { get; set; }
        // Comm: Rx
        public ReactiveCollection<SerialDebugger.Comm.RxFrame> RxFramesRef { get; set; }
        // Comm: AutoTx
        public ReactiveCollection<SerialDebugger.Comm.AutoTxJob> AutoTxJobsRef { get; set; }
        // Serial: Protocol
        public Serial.Protocol ProtocolRef { get; set; }
        // WebView2向けI/F
        public CommTxFramesIf Tx { get; set; }
        public CommAutoTxJobsIf AutoTx { get; set; }
        public CommRxFramesIf Rx { get; set; }
        public SerialMatchResultsIf RxMatch { get; set; }

        public int DebugValue { get; set; } = 0;

        public CommIf()
        {
            Tx = new CommTxFramesIf();
            AutoTx = new CommAutoTxJobsIf();
            RxMatch = new SerialMatchResultsIf();
            Rx = new CommRxFramesIf();

            ProtocolRef = null;
        }

        public void Init(ReactiveCollection<SerialDebugger.Comm.TxFrame> tx, ReactiveCollection<SerialDebugger.Comm.RxFrame> rx, ReactiveCollection<SerialDebugger.Comm.AutoTxJob> autotx)
        {
            //
            TxFramesRef = tx;
            RxFramesRef = rx;
            AutoTxJobsRef = autotx;
            //
            Tx.TxFramesRef = tx;
            AutoTx.AutoTxJobsRef = autotx;
            Rx.RxFrames(rx);
        }
        public void Init(Serial.Protocol protocol)
        {
            ProtocolRef = protocol;
            Tx.ProtocolRef = protocol;
            RxMatch.ProtocolRef = protocol;
        }



        public Int64 TxField(int frame_id, int field_id)
        {
            return TxFramesRef[frame_id].Buffers[0].FieldValues[field_id].Value.Value;
        }

        public void SetTxField(int frame_id, int field_id, Int64 value)
        {

        }

        public void SetTxData(int frame_id, int field_id, Int64 value)
        {

        }
        
        public void Debug()
        {
            // WebView2/JavaScript側からのコールがGUIスレッドで呼ばれるっぽい
            DebugValue++;
        }

        public void Error(string msg)
        {
            DebugValue++;
        }
    }
    
}
