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
    public class CommRxFramesIf
    {
        // Commデータへの参照
        // Comm: Rx
        public ReactiveCollection<SerialDebugger.Comm.RxFrame> RxFramesRef { get; set; }

        // I/F: C# -> WebView2
        // 受信解析対象 受信データ
        // script文字列に埋め込む場合は毎回string生成になるのでI/Fで渡した方が早いか？
        public byte Data { get; set; } = 0;
        // I/F: WebView2 -> C#
        // Rxの受信解析結果の応答に使う
        public const int MatchProgress = 0;
        public const int MatchFailed = 1;
        public const int MatchSuccess = 2;
        public int Result { get; set; } = 0;
        public int Debug { get; set; } = 0;
        public bool Sync { get; set; } = false;
        public List<List<string>> Log { get; set; } = new List<List<string>>();

        public void Init()
        {
            foreach (var log in Log)
            {
                log.Clear();
            }
        }

        public void AddLog(int id, string log)
        {
            Log[id].Add(log);
        }

        public CommRxFramesIf RxFrames(ReactiveCollection<SerialDebugger.Comm.RxFrame> rx)
        {
            RxFramesRef = rx;

            Log.Capacity = rx.Count;
            for (int i = 0; i<rx.Count; i++)
            {
                if (Log.Count <= i)
                {
                    Log.Add(new List<string>());
                }
                var frame = rx[i];
                var log = Log[i];
                for (int j = 0; j < frame.Fields.Count; j++)
                {
                    if (log.Count <= j)
                    {
                        log.Add(string.Empty);
                    }
                }
            }

            return this;
        }
    }



    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class SerialMatchResultsIf
    {
        public Serial.RxAnalyzer RxAnalyzerRef { get; set; }
        public RxMatchResultIf RxMatchResultIf { get; set; } = new RxMatchResultIf();
        // I/F: WebView2 -> C#
        // AutoTxのRxMatchResultが条件と一致したかの判定結果応答に使う
        public bool Result { get; set; } = true;

        public int Count
        {
            get
            {
                return RxAnalyzerRef.MatchResultPos;
            }
        }

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public RxMatchResultIf this[int match_idx]
        {
            get
            {
                return RxMatchResultIf.RxMatchResultNode(RxAnalyzerRef.MatchResult[match_idx]);
            }
        }

    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class RxMatchResultIf
    {
        public Serial.RxMatchResult RxMatchResultRef { get; set; }

        public int FrameId
        {
            get
            {
                return RxMatchResultRef.PatternId;
            }
        }
        public int PatternId
        {
            get
            {
                return RxMatchResultRef.PatternId;
            }
        }

        public RxMatchResultIf RxMatchResultNode(Serial.RxMatchResult node)
        {
            RxMatchResultRef = node;
            return this;
        }
    }

}
