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
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommIf
    {
        // Commデータへの参照
        // Comm: Tx
        public ReactiveCollection<Comm.TxFrame> TxFramesRef { get; set; }
        // Comm: Rx
        public ReactiveCollection<Comm.RxFrame> RxFramesRef { get; set; }
        // Comm: AutoTx
        public ReactiveCollection<Comm.AutoTxJob> AutoTxJobsRef { get; set; }

        public int DebugValue { get; set; } = 0;

        public CommIf()
        {
        }

        public void Init(ReactiveCollection<Comm.TxFrame> tx, ReactiveCollection<Comm.RxFrame> rx, ReactiveCollection<Comm.AutoTxJob> autotx)
        {
            TxFramesRef = tx;
            RxFramesRef = rx;
            AutoTxJobsRef = autotx;
        }


        public void SetTxField(int frame_id, int field_id, Int64 value)
        {

        }

        public void SetTxData(int frame_id, int field_id, Int64 value)
        {

        }

        public void SetTxFix()
        {

        }

        public void Debug()
        {
            // WebView2/JavaScript側からのコールがGUIスレッドで呼ばれるっぽい
            DebugValue++;
        }
    }
}
