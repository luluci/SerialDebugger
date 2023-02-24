﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Comm
{
    public enum RxAnalyzeRuleType
    {
        Any,
        Value,
        Timeout,
        Script,
        ActivateAutoTx,
        ActivateRx,
    }

    public class RxAnalyzeRule
    {
        public RxAnalyzeRuleType Type { get; set; }
        public RxMatch MatchRef { get; set; }

        public byte Value { get; set; }
        public byte Mask { get; set; }
        public int Timeout { get; set; }

        // Script
        public string RxBegin { get; set; }
        public string RxRecieved { get; set; }

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

        public RxAnalyzeRule(int frame_id, int ptn_id, string begin, string recieved)
        {
            // Script
            Type = RxAnalyzeRuleType.Script;
            RxBegin = $"{begin}({frame_id}, {ptn_id})";
            RxRecieved = $"{recieved}({frame_id}, {ptn_id})";
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
