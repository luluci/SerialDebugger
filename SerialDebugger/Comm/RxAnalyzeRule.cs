using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Comm
{
    enum RxAnalyzeRuleType
    {
        Value,
        Timeout,
        Script
    }

    class RxAnalyzeRule
    {
        public RxAnalyzeRuleType Type { get; set; }
        public byte Value { get; set; }
        public byte Mask { get; set; }
        public int Timeout { get; set; }
        public string Script { get; set; }

        public RxAnalyzeRule(byte value, byte mask)
        {
            Type = RxAnalyzeRuleType.Value;
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
    }
}
