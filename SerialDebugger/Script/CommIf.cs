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
        // WebView2向けI/F
        public CommTxFramesIf Tx { get; set; }
        public CommAutoTxJobsIf AutoTx { get; set; }


        public int DebugValue { get; set; } = 0;

        public CommIf()
        {
            Tx = new CommTxFramesIf();
            AutoTx = new CommAutoTxJobsIf();
        }

        public void Init(ReactiveCollection<Comm.TxFrame> tx, ReactiveCollection<Comm.RxFrame> rx, ReactiveCollection<Comm.AutoTxJob> autotx)
        {
            //
            TxFramesRef = tx;
            RxFramesRef = rx;
            AutoTxJobsRef = autotx;
            //
            Tx.TxFramesRef = tx;
            AutoTx.AutoTxJobsRef = autotx;
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




    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommAutoTxJobsIf
    {
        // Commデータへの参照
        // Comm: AutoTx
        public ReactiveCollection<Comm.AutoTxJob> AutoTxJobsRef { get; set; }
        // I/F: C# -> WebView2
        public CommAutoTxActionsIf CommAutoTxActionsIf { get; set; } = new CommAutoTxActionsIf();
        // I/F: WebView2 -> C#
        public bool Result { get; set; } = true;

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public CommAutoTxActionsIf this[int job_id]
        {
            get
            {
                return CommAutoTxActionsIf.AutoTxAction(AutoTxJobsRef[job_id].Actions);
            }
        }

        public CommAutoTxJobsIf AutoTxJobs(ReactiveCollection<Comm.AutoTxJob> autotx)
        {
            AutoTxJobsRef = autotx;
            return this;
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommAutoTxActionsIf
    {
        // Commデータへの参照
        // Comm: AutoTx
        public ReactiveCollection<Comm.AutoTxAction> AutoTxActionsRef { get; set; }
        //
        public CommAutoTxActionIf CommAutoTxActionIf { get; set; } = new CommAutoTxActionIf();

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public CommAutoTxActionIf this[int action_id]
        {
            get
            {
                return CommAutoTxActionIf.AutoTxAction(AutoTxActionsRef[action_id]);
            }
        }

        public CommAutoTxActionsIf AutoTxAction(ReactiveCollection<Comm.AutoTxAction> actions)
        {
            AutoTxActionsRef = actions;
            return this;
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommAutoTxActionIf
    {
        // Commデータへの参照
        // Comm: AutoTx
        public Comm.AutoTxAction ActionRef { get; set; }


        public CommAutoTxActionIf AutoTxAction(Comm.AutoTxAction action)
        {
            ActionRef = action;
            return this;
        }
    }



    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommTxFramesIf
    {
        // Commデータへの参照
        // Comm: Tx
        public ReactiveCollection<Comm.TxFrame> TxFramesRef { get; set; }
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
        
        public CommTxFramesIf TxFrames(ReactiveCollection<Comm.TxFrame> tx)
        {
            TxFramesRef = tx;
            return this;
        }

        public void Fix(int frame_id, int buffer_id)
        {
            TxFramesRef[frame_id].Buffers[buffer_id].BufferFix();
        }

    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommTxFieldBuffersIf
    {
        // Commデータへの参照
        // Comm: Tx
        public ReactiveCollection<Comm.TxFieldBuffer> TxFieldBuffersRef { get; set; }
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

        public CommTxFieldBuffersIf TxBuffer(ReactiveCollection<Comm.TxFieldBuffer> tx)
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
        public Comm.TxFieldBuffer TxFieldBufferRef { get; set; }
        
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

        public CommTxFieldBufferIf TxFieldBuffer(Comm.TxFieldBuffer tx)
        {
            TxFieldBufferRef = tx;
            return this;
        }
    }
}
