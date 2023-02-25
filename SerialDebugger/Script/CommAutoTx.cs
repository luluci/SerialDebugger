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
    public class CommAutoTxJobsIf
    {
        // Commデータへの参照
        // Comm: AutoTx
        public ReactiveCollection<SerialDebugger.Comm.AutoTxJob> AutoTxJobsRef { get; set; }
        // I/F: C# -> WebView2
        public CommAutoTxJobIf AutoTxJobIf { get; set; } = new CommAutoTxJobIf();
        // I/F: WebView2 -> C#
        public bool Result { get; set; } = true;

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public CommAutoTxJobIf this[int job_id]
        {
            get
            {
                return AutoTxJobIf.AutoTxJob(AutoTxJobsRef[job_id]);
            }
        }

        public CommAutoTxJobsIf AutoTxJobs(ReactiveCollection<SerialDebugger.Comm.AutoTxJob> autotx)
        {
            AutoTxJobsRef = autotx;
            return this;
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommAutoTxJobIf
    {
        // Commデータへの参照
        // Comm: AutoTx
        public SerialDebugger.Comm.AutoTxJob AutoTxJobRef { get; set; }
        //
        public CommAutoTxActionsIf Actions { get; set; } = new CommAutoTxActionsIf();

        public void JumpTo(int id)
        {
            if (id < AutoTxJobRef.Actions.Count)
            {
                AutoTxJobRef.ActiveActionIndex = id;
            }
        }

        public CommAutoTxJobIf AutoTxJob(SerialDebugger.Comm.AutoTxJob job)
        {
            AutoTxJobRef = job;
            Actions.AutoTxActions(job.Actions);
            return this;
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommAutoTxActionsIf
    {
        // Commデータへの参照
        // Comm: AutoTx
        public ReactiveCollection<SerialDebugger.Comm.AutoTxAction> AutoTxActionsRef { get; set; }
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

        public CommAutoTxActionsIf AutoTxActions(ReactiveCollection<SerialDebugger.Comm.AutoTxAction> actions)
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
        public SerialDebugger.Comm.AutoTxAction ActionRef { get; set; }


        public CommAutoTxActionIf AutoTxAction(SerialDebugger.Comm.AutoTxAction action)
        {
            ActionRef = action;
            return this;
        }
    }

}
