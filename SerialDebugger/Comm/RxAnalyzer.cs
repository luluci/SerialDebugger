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
        public delegate void DelegateActivate();
        public static System.Windows.Threading.Dispatcher Dispatcher { get; set; }

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
            if (Rules.Count <= Pos)
            {
                return false;
            }

            bool match = false;
            bool check_next = true;
            while (check_next)
            {
                var rule = Rules[Pos];
                // ルールチェック
                switch (rule.Type)
                {
                    case RxAnalyzeRuleType.Any:
                        // Any
                        match = MatchAny();
                        break;

                    case RxAnalyzeRuleType.Value:
                        // 特定パターンとマッチング
                        match = MatchValue(rule, data);
                        break;

                    case RxAnalyzeRuleType.Timeout:
                        // タイムアウト待機中に何か受信したらルールアンマッチとする
                        IsActive = false;
                        return false;

                    case RxAnalyzeRuleType.Script:
                        // 未実装: 常に失敗
                        IsActive = false;
                        return false;

                    case RxAnalyzeRuleType.ActivateAutoTx:
                        match = MatchActivateAutoTx(rule);
                        break;

                    case RxAnalyzeRuleType.ActivateRx:
                        match = MatchActivateRx(rule);
                        break;

                    default:
                        return false;
                }
                // ルールOKなら次ルールチェック
                if (match)
                {
                    check_next = Next();
                }
                else
                {
                    IsActive = false;
                    return false;
                }
            }

            // ルールすべてマッチしたら受信解析成功で終了
            if (Rules.Count <= Pos)
            {
                return true;
            }
            //
            return false;
        }

        private bool Next()
        {
            // 
            Pos++;
            // ルール全消化で完了
            if (Rules.Count <= Pos)
            {
                return false;
            }
            // 次ルールをチェックして、連続で処理するか判定
            var rule = Rules[Pos];
            switch (rule.Type)
            {
                case RxAnalyzeRuleType.ActivateAutoTx:
                case RxAnalyzeRuleType.ActivateRx:
                    // Activateは即処理してしまう
                    return true;

                default:
                    // その他は自周期以降
                    return false;
            }
        }

        private bool MatchAny()
        {
            // 
            return true;
        }

        private bool MatchValue(RxAnalyzeRule rule, byte data)
        {
            if ((rule.Mask & data) == rule.Value)
            {
                // データマッチしたら次の解析へ
                return true;
            }
            else
            {
                // マッチしなかったら終了
                IsActive = false;
                return false;
            }

        }

        private bool MatchActivateAutoTx(RxAnalyzeRule rule)
        {
            // 有効無効設定
            // invokeを使用しているので処理速度に注意
            Dispatcher.BeginInvoke(new DelegateActivate(() => {
                rule.MatchRef.AutoTxJobRef.IsActive.Value = rule.MatchRef.AutoTxState;
            }));
            // 常にOK
            return true;
        }
        private bool MatchActivateRx(RxAnalyzeRule rule)
        {
            // 有効無効設定
            Dispatcher.BeginInvoke(new DelegateActivate(() => {
                rule.MatchRef.RxPatternRef.IsActive.Value = rule.MatchRef.RxState;
            }));
            // 常にOK
            return true;
        }


        public bool Match(CycleTimer timer)
        {
            bool match = false;
            bool check_next = true;
            while (check_next)
            {
                var rule = Rules[Pos];
                switch (rule.Type)
                {
                    case RxAnalyzeRuleType.Timeout:
                        match = MatchTimeout(rule, timer);
                        break;

                    case RxAnalyzeRuleType.Any:
                    case RxAnalyzeRuleType.Value:
                    case RxAnalyzeRuleType.Script:
                    default:
                        return false;
                }
                // ルールOKなら次ルールチェック
                if (match)
                {
                    check_next = Next();
                }
                else
                {
                    IsActive = false;
                    return false;
                }
            }
            // ルールすべてマッチしたら受信解析成功で終了
            if (Rules.Count <= Pos)
            {
                return true;
            }
            //
            return false;
        }
        private bool MatchTimeout(RxAnalyzeRule rule, CycleTimer timer)
        {
            if (timer.WaitTimeElapsed(rule.Timeout))
            {
                // 時間経過したら次の解析へ
                return true;
            }
            else
            {
                // 時間未経過で待機継続
                return false;
            }
        }
    }
}
