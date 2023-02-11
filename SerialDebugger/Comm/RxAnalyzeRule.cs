using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Comm
{
    enum RxAnalyzeRuleType
    {
        Any,
        Value,
        Timeout,
        Script,
        ActivateAutoTx,
        ActivateRx,
    }

    class RxAnalyzeRule
    {
        public RxAnalyzeRuleType Type { get; set; }
        public RxMatch MatchRef { get; set; }

        public byte Value { get; set; }
        public byte Mask { get; set; }
        public int Timeout { get; set; }
        public string Script { get; set; }

        public RxAnalyzeRule(byte value, byte mask)
        {
            // Type判定
            // maskがゼロなら無条件マッチなのでAny
            if (mask == 0)
            {
                Type = RxAnalyzeRuleType.Any;
            }
            else
            {
                Type = RxAnalyzeRuleType.Value;
            }

            Value = value;
            Mask = mask;
        }

        public RxAnalyzeRule(int msec)
        {
            // Timeout
            Type = RxAnalyzeRuleType.Timeout;
            Timeout = msec;
        }

        public RxAnalyzeRule(string script)
        {
            // Script
            Type = RxAnalyzeRuleType.Script;
            Script = script;
        }

        public RxAnalyzeRule(RxMatch match)
        {
            MatchRef = match;

            switch (MatchRef.Type)
            {
                case RxMatchType.ActivateAutoTx:
                    Type = RxAnalyzeRuleType.ActivateAutoTx;
                    break;
                case RxMatchType.ActivateRx:
                    Type = RxAnalyzeRuleType.ActivateRx;
                    break;
            }
        }
    }
}
