using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Comm
{
    using Utility;

    class RxAnalyzer
    {

        public List<RxAnalyzeRule> Rules { get; set; }
        public int Pos { get; set; }
        public bool IsActive { get; set; }

        public RxAnalyzer()
        {
            Rules = new List<RxAnalyzeRule>();
            Pos = 0;
            IsActive = true;
        }

        public bool Match(byte data)
        {
            var rule = Rules[Pos];
            switch (rule.Type)
            {
                case RxAnalyzeRuleType.Any:
                    return MatchAny();

                case RxAnalyzeRuleType.Value:
                    return MatchValue(rule, data);

                case RxAnalyzeRuleType.Timeout:
                    // タイムアウト待機中に何か受信したらルールアンマッチとする
                    IsActive = false;
                    return false;

                case RxAnalyzeRuleType.Script:
                    return false;

                default:
                    return false;
            }
        }

        private bool Next()
        {
            // 
            Pos++;
            //
            if (Rules.Count <= Pos)
            {
                return true;
            }
            //
            return false;
        }

        private bool MatchAny()
        {
            return Next();
        }

        private bool MatchValue(RxAnalyzeRule rule, byte data)
        {
            if ((rule.Mask & data) == rule.Value)
            {
                // データマッチしたら次の解析へ
                return Next();
            }
            else
            {
                // マッチしなかったら終了
                IsActive = false;
                return false;
            }

        }


        public bool Match(CycleTimer timer)
        {
            var rule = Rules[Pos];
            switch (rule.Type)
            {
                case RxAnalyzeRuleType.Timeout:
                    return MatchTimeout(rule, timer);

                case RxAnalyzeRuleType.Any:
                case RxAnalyzeRuleType.Value:
                case RxAnalyzeRuleType.Script:
                default:
                    return false;
            }
        }
        private bool MatchTimeout(RxAnalyzeRule rule, CycleTimer timer)
        {
            if (timer.WaitTimeElapsed(rule.Timeout))
            {
                // 時間経過したら次の解析へ
                return Next();
            }
            else
            {
                // 時間未経過で待機継続
                return false;
            }
        }
    }
}
