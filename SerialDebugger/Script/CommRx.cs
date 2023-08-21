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

    public class CommRxResult
    {
        public const int MatchProgress = 0;
        public const int MatchFailed = 1;
        public const int MatchSuccess = 2;
    }

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
        //public int FrameId { get; set; } = 0;
        //public int PatternId { get; set; } = 0;
        // I/F: WebView2 -> C#
        // Rxの受信解析結果の応答に使う
        public int MatchProgress { get; } = CommRxResult.MatchProgress;
        public int MatchFailed { get; } = CommRxResult.MatchFailed;
        public int MatchSuccess { get; } = CommRxResult.MatchSuccess;
        public int Result { get; set; } = 0;
        public int Debug { get; set; } = 0;
        public bool Sync { get; set; } = false;
        public List<List<List<string>>> Log { get; set; } = new List<List<List<string>>>();
        //
        public CommRxFrameIf CommRxFrameIf { get; set; } = new CommRxFrameIf();

        public void Init()
        {
            foreach (var frame_log in Log)
            {
                foreach (var pattern_log in frame_log)
                {
                    pattern_log.Clear();
                }
            }
        }

        public void AddLog(int frame_id, int pattern_id, string log)
        {
            try
            {
                Log[frame_id][pattern_id].Add(log);
            }
            catch (Exception ex)
            {
                Logger.AddException(ex);
            }
        }

        public CommRxFramesIf RxFrames(ReactiveCollection<SerialDebugger.Comm.RxFrame> rx)
        {
            RxFramesRef = rx;

            // Framesの分のキャパシティを確保
            if (Log.Capacity < rx.Count)
            {
                Log.Capacity = rx.Count;
            }
            for (int frame_id = 0; frame_id<rx.Count; frame_id++)
            {
                var frame = rx[frame_id];
                // Frame用のログ領域を追加
                if (Log.Count <= frame_id)
                {
                    Log.Add(new List<List<string>>());
                }
                var log_frame = Log[frame_id];
                // Patterns分のキャパシティ確保
                if (log_frame.Count < frame.Patterns.Count && log_frame.Capacity < frame.Patterns.Count)
                {
                    log_frame.Capacity = frame.Patterns.Count;
                }
                for (int pattern_id = 0; pattern_id < frame.Patterns.Count; pattern_id++)
                {
                    // Pattern用のログ領域を追加
                    if (log_frame.Count <= pattern_id)
                    {
                        log_frame.Add(new List<string>());
                    }
                    var log_pattern = log_frame[pattern_id];
                    // field分のキャパシティ確保
                    if (log_pattern.Capacity < frame.Fields.Count)
                    {
                        log_pattern.Capacity = frame.Fields.Count;
                    }
                }
            }

            return this;
        }
        
        public CommRxFrameIf this[int idx]
        {
            get
            {
                return CommRxFrameIf.RxFrame(RxFramesRef[idx]);
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommRxFrameIf
    {
        // Commデータへの参照
        // Comm: Rx
        public SerialDebugger.Comm.RxFrame RxFrameRef { get; set; }
        //
        public CommRxFieldsIf CommRxFieldsIf { get; set; } = new CommRxFieldsIf();
        
        public CommRxFieldsIf Fields
        {
            get
            {
                return CommRxFieldsIf.RxFields(RxFrameRef.Fields);
            }
        }

        public CommRxFrameIf RxFrame(SerialDebugger.Comm.RxFrame node)
        {
            RxFrameRef = node;
            return this;
        }
    }


    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommRxFieldsIf
    {
        // Commデータへの参照
        // Comm: Rx
        public ReactiveCollection<SerialDebugger.Comm.Field> RxFieldsRef { get; set; }
        //
        public CommRxFieldIf CommRxFieldIf { get; set; } = new CommRxFieldIf();

        public CommRxFieldsIf RxFields(ReactiveCollection<SerialDebugger.Comm.Field> node)
        {
            RxFieldsRef = node;
            return this;
        }

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public CommRxFieldIf this[int idx]
        {
            get
            {
                return CommRxFieldIf.RxField(RxFieldsRef[idx]);
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommRxFieldIf
    {
        // Commデータへの参照
        // Comm: Tx
        public SerialDebugger.Comm.Field RxFieldRef { get; set; }
        
        public CommRxFieldIf RxField(SerialDebugger.Comm.Field node)
        {
            RxFieldRef = node;
            return this;
        }

        public CommRxFieldDictIf GetDict()
        {
            var result = new CommRxFieldDictIf();
            
            foreach (var select in RxFieldRef.Selects)
            {
                result.Dict.Add(new CommRxFieldDictNodeIf { Key=select.Value, Value=select.Disp });
                result.Count++;
            }
            
            return result;
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommRxFieldDictIf
    {
        public List<CommRxFieldDictNodeIf> Dict = new List<CommRxFieldDictNodeIf>();
        public int Count = 0;

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public CommRxFieldDictNodeIf this[int idx]
        {
            get
            {
                return Dict[idx];
            }
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommRxFieldDictNodeIf
    {
        public Int64 Key { get; set; } = 0;

        // stringをプロパティでJS側から取得しようとするとうまくいかない
        // よくわからないオブジェクトになる
        public string Value { get; set; } = string.Empty;

        public string GetValue()
        {
            // stringを関数で取得するようにするとJS側からもstringになる
            return Value;
        }
    }



    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class SerialMatchResultsIf
    {
        public Serial.Protocol ProtocolRef { get; set; } = null;
        public RxMatchResultIf RxMatchResultIf { get; set; } = new RxMatchResultIf();
        // I/F: WebView2 -> C#
        // AutoTxのRxMatchResultが条件と一致したかの判定結果応答に使う
        public bool Result { get; set; } = true;

        public bool IsTimeout
        {
            get
            {
                return ProtocolRef.MatchResultIsTimeout;
            }
        }
        public int Count
        {
            get
            {
                // RunResultCheckの注意書きを参照
                // 無理矢理マッチ結果を除いているので、
                // Script内で次の受信が無いと分かっている状況で使用する。
                return ProtocolRef.MatchResultCount;
            }
        }
        public bool HasAnyRecv
        {
            get
            {
                // 何かしらの受信有無を判定
                //   (1)タイムアウトによる受信確定
                //   (2)受信解析あり
                return ProtocolRef.MatchResultIsTimeout || (ProtocolRef.MatchResultCount > 0);
            }
        }

        public bool IsEnable
        {
            get
            {
                // ？多分使ってないので名前変える
                return ProtocolRef.MatchResultPos == 0;
            }
        }
        public void Clear()
        {
            // マッチ結果を無効化する
            ProtocolRef.MatchResultIsTimeout = false;
            ProtocolRef.MatchResultCount = 0;
        }

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public RxMatchResultIf this[int match_idx]
        {
            get
            {
                return RxMatchResultIf.RxMatchResultNode(ProtocolRef.MatchResult[match_idx]);
            }
        }

    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class RxMatchResultIf
    {
        public Serial.RxMatchResult RxMatchResultRef { get; set; }
        public RxMatchResultPatternMatchIf RxMatchResultPatternMatchIf { get; set; } = new RxMatchResultPatternMatchIf();

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

        [System.Runtime.CompilerServices.IndexerName("Items")]
        public RxMatchResultPatternMatchIf this[int idx]
        {
            get
            {
                return RxMatchResultPatternMatchIf.RxMatchNode(RxMatchResultRef.PatternRef.Matches[idx]);
            }
        }
    }
    
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class RxMatchResultPatternMatchIf
    {
        public Comm.RxMatch RxMatchResultPatternMatchRef { get; set; }

        public Int64 Value
        {
            get
            {
                return RxMatchResultPatternMatchRef.Value.Value;
            }
        }

        public string Disp
        {
            get
            {
                return RxMatchResultPatternMatchRef.Disp.Value;
            }
        }

        public RxMatchResultPatternMatchIf RxMatchNode(Comm.RxMatch node)
        {
            RxMatchResultPatternMatchRef = node;
            return this;
        }
    }

}
