using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialDebugger.Serial
{
    enum RxDataType
    {
        Empty,
        Match,
        Timeout,
        Cancel,
    }

    class RxData
    {
        public RxDataType Type { get; set; }
        public const int BuffSize = 1024;
        public byte[] RxBuff { get; set; }
        public int RxBuffOffset { get; set; }
        public int RxBuffTgtPos { get; set; }
        public DateTime TimeStamp { get; set; }

        public RxData()
        {
            RxBuff = new byte[BuffSize];
            RxBuffOffset = 0;
            RxBuffTgtPos = 0;
        }
    }

    class RxMatchResult
    {
        public int FrameId { get; set; } = -1;
        public int PatternId { get; set; } = -1;
        public Comm.RxPattern PatternRef { get; set; }
    }

    class RxAnalyzer
    {
        bool HasRecieve;

        SerialPort serial;
        public IList<Comm.RxFrame> RxFramesRef;
        bool MultiMatch;

        // 解析結果
        public RxData Result { get; set; }
        public List<RxMatchResult> MatchResult;
        public int MatchResultPos { get; set; }

        public RxAnalyzer(SerialPort serial, IList<Comm.RxFrame> rxFrames, bool multiMatch)
        {
            this.serial = serial;
            RxFramesRef = rxFrames;
            MultiMatch = multiMatch;

            // 受信ハンドラ登録
            HasRecieve = false;
            serial.DataReceived += Serial_DataReceived;
            serial.ReadTimeout = 0;

            //
            Result = new RxData();

            // Queueサイズ計算
            int queue_size = 0;
            foreach (var frame in RxFramesRef)
            {
                queue_size += frame.Patterns.Count;
            }
            // Queue初期化
            MatchResult = new List<RxMatchResult>(queue_size);
            for (int i=0; i<queue_size; i++)
            {
                MatchResult.Add(new RxMatchResult());
            }
            //
            MatchResultPos = 0;
        }

        public void Init()
        {
            // 解析ルール初期化
            // 解析
            foreach (var frame in RxFramesRef)
            {
                foreach (var pattern in frame.Patterns)
                {
                    if (pattern.IsActive.Value)
                    {
                        var analyzer = pattern.Analyzer;
                        analyzer.Pos = 0;
                        analyzer.IsActive = true;
                    }
                }
            }
            // 受信バッファ初期化
            Result.RxBuffOffset = 0;
            Result.RxBuffTgtPos = 0;
            //
            MatchResultPos = 0;
        }

        public Task Run(int timeout, int polling, CancellationToken ct)
        {
            // 受信タスク作成
            return Task.Run(async () =>
            {
                // シリアル処理開始
                await RunImpl(timeout, polling, ct);
            }, ct);
        }

        private async Task RunImpl(int timeout, int polling, CancellationToken ct)
        {
            // 周期タイマ
            Utility.CycleTimer cycle = new Utility.CycleTimer();
            // タイムアウトタイマ
            // タイムアウト判定基準時間:最終受信時刻からtimeout経過で受信終了
            // タイムアウト判定基準時間:受信開始時刻からtimeout経過で受信終了
            // 暫定:必ず打ち切れるように受信開始時刻ベースのタイムアウトでいく
            Utility.CycleTimer beginTimer = new Utility.CycleTimer();
            Utility.CycleTimer endTimer = new Utility.CycleTimer();

            while (true)
            {
                // ポーリング周期測定開始
                cycle.Start();

                // Task Cancel判定
                if (ct.IsCancellationRequested)
                {
                    //throw new OperationCanceledException("Cancel Requested");
                    Result.Type = RxDataType.Cancel;
                    return;
                }

                // タイムアウト判定
                // 何かしらのデータ受信後、指定時間経過でタイムアウトする
                if (Result.RxBuffOffset > 0)
                {
                    if (beginTimer.WaitForMsec(timeout) <= 0)
                    {
                        Result.Type = RxDataType.Timeout;
                        return;
                    }
                }

                // 受信バッファ読み出し
                if (HasRecieve)
                {
                    // 受信開始した時間を記憶
                    if (Result.RxBuffOffset == 0)
                    {
                        beginTimer.Start();
                    }
                    // 最後に受信した時間を更新
                    endTimer.Start();

                    // すぐに受信フラグを下す
                    // フラグを下した後にシリアル受信を読み出すことで取得漏れは無くなるはず
                    HasRecieve = false;
                    try
                    {
                        // 受信バッファ読み出し
                        var len = serial.Read(Result.RxBuff, Result.RxBuffOffset, RxData.BuffSize - Result.RxBuffOffset);
                        Result.RxBuffOffset += len;
                        // 受信解析
                        if (Analyze())
                        {
                            Result.Type = RxDataType.Match;
                            Result.TimeStamp = endTimer.GetTime();
                            return;
                        }
                    }
                    catch (TimeoutException)
                    {
                        // HasRecieveを下してからReadするまでに受信した場合、
                        // 受信バッファが空でHasRecieveが立っている可能性があるが、
                        // そのケースはスルー
                    }
                }
                else
                {
                    if (AnalyzeTimeout(endTimer))
                    {
                        Result.Type = RxDataType.Match;
                        return;
                    }
                }

                await cycle.WaitAsync(polling);
            }
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            HasRecieve = true;
        }

        private bool Analyze()
        {
            bool result = false;

            // 1文字ずつ解析
            for (; Result.RxBuffTgtPos < Result.RxBuffOffset; Result.RxBuffTgtPos++)
            {
                // 解析対象データ
                var ch = Result.RxBuff[Result.RxBuffTgtPos];
                // 解析
                foreach (var frame in RxFramesRef)
                {
                    foreach (var pattern in frame.Patterns)
                    {
                        if (pattern.IsActive.Value)
                        {
                            var analyzer = pattern.Analyzer;
                            if (analyzer.IsActive)
                            {
                                if (analyzer.Match(ch))
                                {
                                    // パターンマッチ成功ならマッチしたインスタンスを登録
                                    MatchResult[MatchResultPos].FrameId = frame.Id;
                                    MatchResult[MatchResultPos].PatternId = pattern.Id;
                                    MatchResult[MatchResultPos].PatternRef = pattern;
                                    MatchResultPos++;
                                    // 複数マッチ不許可なら終了
                                    if (!MultiMatch)
                                    {
                                        return true;
                                    }
                                    result = true;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        private bool AnalyzeTimeout(Utility.CycleTimer timeoutTimer)
        {
            bool result = false;

            // 解析
            foreach (var frame in RxFramesRef)
            {
                foreach (var pattern in frame.Patterns)
                {
                    if (pattern.IsActive.Value)
                    {
                        var analyzer = pattern.Analyzer;
                        if (analyzer.IsActive)
                        {
                            if (analyzer.Match(timeoutTimer))
                            {
                                // パターンマッチ成功ならマッチしたインスタンスを登録
                                MatchResult[MatchResultPos].FrameId = frame.Id;
                                MatchResult[MatchResultPos].PatternId = pattern.Id;
                                MatchResult[MatchResultPos].PatternRef = pattern;
                                MatchResultPos++;
                                // 複数マッチ不許可なら終了
                                if (!MultiMatch)
                                {
                                    return true;
                                }
                                result = true;
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
