using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Serial
{
    using System.Threading;
    using Utility;
    using Logger = Log.Log;
    using Setting = SerialDebugger.Settings.Settings;

    public enum RxDataType
    {
        Empty,
        Match,
        Timeout,
        Cancel,
    }

    public class RxData
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

    public class RxMatchResult
    {
        public int FrameId { get; set; } = -1;
        public int PatternId { get; set; } = -1;
        public Comm.RxPattern PatternRef { get; set; }
    }



    public class Protocol
    {
        // 参照
        public SerialPort Serial;
        public ReactiveCollection<Comm.TxFrame> TxFrames;
        public ReactiveCollection<Comm.RxFrame> RxFrames;
        public ReactiveCollection<Comm.AutoTxJob> AutoTxJobs;

        // 通信管理
        bool IsRunning;
        int PollingCycle;
        int RxTimeout;
        Utility.CycleTimer PollingTimer = new Utility.CycleTimer();
        Utility.CycleTimer RxBeginTimer = new Utility.CycleTimer();
        Utility.CycleTimer RxEndTimer = new Utility.CycleTimer();
        CancellationTokenSource CancelTokenSource;

        // 受信管理
        bool MultiMatch;
        bool InvertBit;
        bool HasScriptMatch;
        // 自動操作管理
        bool IsAutoTxRunning;

        // 受信ハンドラ
        bool HasRecieve;

        // 解析結果
        public RxData Result { get; set; }
        public List<RxMatchResult> MatchResult { get; set; }
        public int MatchResultPos { get; set; }

        // 受信ハンドラを別タスクで動かすとき
        //// 定期処理関連
        //DispatcherTimer AutoTxTimer;
        //bool IsAutoTxRunning = false;

        public Protocol(SerialPort serial, int polling, int rx_timeout, ReactiveCollection<Comm.TxFrame> TxFrames, ReactiveCollection<Comm.RxFrame> RxFrames, ReactiveCollection<Comm.AutoTxJob> AutoTxJobs)
        {
            // 参照設定
            Serial = serial;
            this.TxFrames = TxFrames;
            this.RxFrames = RxFrames;
            this.AutoTxJobs = AutoTxJobs;
            // 通信管理
            PollingCycle = polling;
            RxTimeout = rx_timeout;
            CancelTokenSource = new CancellationTokenSource();
            //
            MultiMatch = Setting.Data.Comm.RxMultiMatch;
            InvertBit = Setting.Data.Comm.RxInvertBit;
            HasScriptMatch = Setting.Data.Comm.RxHasScriptMatch;
            // 受信解析結果
            Result = new RxData();
            // Queueサイズ計算
            int queue_size = 0;
            foreach (var frame in RxFrames)
            {
                queue_size += frame.Patterns.Count;
            }
            // Queue初期化
            MatchResult = new List<RxMatchResult>(queue_size);
            for (int i = 0; i < queue_size; i++)
            {
                MatchResult.Add(new RxMatchResult());
            }
            //
            MatchResultPos = 0;
            // AutoTx
            IsAutoTxRunning = AutoTxJobs.Count > 0;

            // 受信ハンドラ設定
            // ハンドラでフラグを立てて、通信ループでバッファを浚う。
            HasRecieve = false;
            serial.DataReceived += Serial_DataReceived;
            serial.ReadTimeout = 0;

            // 受信ハンドラを別タスクで動かすとき
            //// 定期処理
            //AutoTxTimer = new DispatcherTimer(DispatcherPriority.Normal);
            //AutoTxTimer.Tick += new EventHandler(AutoTxHandler);
        }

        public async Task Run()
        {
            try
            {
                IsRunning = true;

                while (IsRunning)
                {
                    // 受信開始前に解析情報系を初期化
                    await Init();
                    // 受信,自動操作ポーリング処理開始
                    // 一連の受信シーケンス完了までループする
                    await RunRxAutoTx(CancelTokenSource.Token);
                    // 受信結果に応じて処理を実施
                    await RunResultCheck();
                }

            }
            catch (Exception exc)
            {
                IsRunning = false;
                Logger.AddException(exc);
            }
        }

        public void Stop()
        {
            CancelTokenSource.Cancel();
        }


        public async Task Init()
        {
            // 解析ルール初期化
            // 解析
            foreach (var frame in RxFrames)
            {
                foreach (var pattern in frame.Patterns)
                {
                    if (pattern.IsActive.Value)
                    {
                        var analyzer = pattern.Analyzer;
                        analyzer.Pos = 0;
                        analyzer.IsActive = true;
                        // Script初期化
                        if (analyzer.HasRxBeginScript)
                        {
                            await Script.Interpreter.Engine.ExecuteScriptAsync(analyzer.RxBeginScript);
                        }
                    }
                }
            }
            // 受信バッファ初期化
            Result.RxBuffOffset = 0;
            Result.RxBuffTgtPos = 0;
            //
            MatchResultPos = 0;
            //
            if (HasScriptMatch)
            {
                Script.Interpreter.Engine.Comm.Rx.Init();
            }
        }


        public async Task RunRxAutoTx(CancellationToken cancel)
        {
            var isRxEvent = false;

            while (true)
            {
                PollingTimer.Start();

                // Task Cancel判定
                if (cancel.IsCancellationRequested)
                {
                    //throw new OperationCanceledException("Cancel Requested");
                    Result.Type = RxDataType.Cancel;
                    return;
                }
                // Rx
                isRxEvent = await RunRx();
                if (isRxEvent)
                {
                    // 受信イベントがあればループ終了して受信結果を処理する
                    return;
                }
                // AutoTx
                await RunAutoTx();

                await PollingTimer.WaitAsync(PollingCycle);
            }

        }


        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            HasRecieve = true;
        }

        public async Task<bool> RunRx()
        {
            // タイムアウト判定
            // 何かしらのデータ受信後、指定時間経過でタイムアウトする
            if (Result.RxBuffOffset > 0)
            {
                if (RxBeginTimer.WaitForMsec(RxTimeout) <= 0)
                {
                    Result.Type = RxDataType.Timeout;
                    return true;
                }
            }

            // 受信バッファ読み出し
            if (HasRecieve)
            {
                // 受信開始した時間を記憶
                if (Result.RxBuffOffset == 0)
                {
                    RxBeginTimer.Start();
                }
                // 最後に受信した時間を更新
                RxEndTimer.Start();

                // すぐに受信フラグを下す
                // フラグを下した後にシリアル受信を読み出すことで取得漏れは無くなるはず
                HasRecieve = false;
                try
                {
                    // 受信バッファ読み出し
                    var len = Serial.Read(Result.RxBuff, Result.RxBuffOffset, RxData.BuffSize - Result.RxBuffOffset);
                    Result.RxBuffOffset += len;
                    // 受信解析
                    if (await AnalyzeRx())
                    {
                        Result.Type = RxDataType.Match;
                        Result.TimeStamp = RxEndTimer.GetTime();
                        return true;
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
                if (AnalyzeTimeout(RxEndTimer))
                {
                    Result.Type = RxDataType.Match;
                    return true;
                }
            }

            // 受信イベント無し
            return false;
        }


        private async Task<bool> AnalyzeRx()
        {
            bool result = false;

            // 1文字ずつ解析
            for (; Result.RxBuffTgtPos < Result.RxBuffOffset; Result.RxBuffTgtPos++)
            {
                // 解析対象データ
                if (InvertBit)
                {
                    Result.RxBuff[Result.RxBuffTgtPos] = (byte)~Result.RxBuff[Result.RxBuffTgtPos];
                }
                var ch = Result.RxBuff[Result.RxBuffTgtPos];
                // 解析
                foreach (var frame in RxFrames)
                {
                    foreach (var pattern in frame.Patterns)
                    {
                        if (pattern.IsActive.Value)
                        {
                            var analyzer = pattern.Analyzer;
                            if (analyzer.IsActive)
                            {
                                if (await analyzer.Match(ch))
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
            foreach (var frame in RxFrames)
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

        public async Task RunAutoTx()
        {
            if (IsAutoTxRunning)
            {
                // 自動送信定期処理
                foreach (var job in AutoTxJobs)
                {
                    // 有効ジョブを実行
                    if (job.IsActive.Value)
                    {
                        await job.ExecCycle(this);
                    }
                }
            }
        }




        public async Task RunResultCheck()
        {
            switch (Result.Type)
            {
                case RxDataType.Cancel:
                    // 実際はOperationCanceledExceptionをcatchする
                    IsRunning = false;
                    break;

                case RxDataType.Timeout:
                    Logger.Add($"[Rx][Timeout] {Logger.Byte2Str(Result.RxBuff, 0, Result.RxBuffOffset)}");
                    break;

                case RxDataType.Match:
                    MakeRxLog();
                    // 処理結果を自動送信処理に通知
                    await AutoTxExecRxEvent();
                    break;

                default:
                    break;
            }
        }



        private void MakeRxLog()
        {
            int frame_id = 0;
            int result_idx = 0;
            while (result_idx < MatchResultPos)
            {
                // 先頭要素からログ作成
                var result = MatchResult[result_idx];
                var sb = new StringBuilder(result.PatternRef.Name);
                frame_id = result.FrameId;
                // 同じFrame内でのパターンマッチは同一ログになる
                result_idx++;
                while (result_idx < MatchResultPos && frame_id == MatchResult[result_idx].FrameId)
                {
                    sb.Append(",").Append(MatchResult[result_idx].PatternRef.Name);

                    result_idx++;
                }
                string log;
                if (result.PatternRef.IsLogVisualize)
                {
                    log = RxFrames[frame_id].MakeLogVisualize(Result.RxBuff, Result.RxBuffOffset, result.PatternRef);
                }
                else
                {
                    log = Logger.Byte2Str(Result.RxBuff, 0, Result.RxBuffOffset);
                }
                Logger.Add($"[Rx][{sb.ToString()}] {log}");
            }
        }

        private async Task AutoTxExecRxEvent()
        {
            if (IsAutoTxRunning)
            {
                // 受信イベントを通知
                foreach (var job in AutoTxJobs)
                {
                    // 有効ジョブを実行
                    if (job.IsActive.Value)
                    {
                        await job.ExecRecv(this);
                    }
                }
            }
        }




        //public async Task RunTask()
        //{
        //    // 必ず受信タスクを動かす
        //    // COM切断時はタスクキャンセルを実行し、受信タスクが終了したら各種後始末を行う。
        //    //tokenSource = new CancellationTokenSource();
        //    // 別スレッドで受信待機するとき
        //    // 自動送信設定
        //    // 自動送信定義があるとき自動送信定期処理タイマ開始
        //    // 自動送信はGUIスレッド上で管理する。
        //    if (AutoTxJobs.Count > 0)
        //    {
        //        AutoTxTimer.Interval = new TimeSpan(0, 0, 0, 0, serialSetting.vm.PollingCycle.Value);
        //        AutoTxTimer.Start();
        //    }

        //}

        //private async void AutoTxHandler(object sender, EventArgs e)
        //{
        //    AutoTxTimer.Stop();
        //    IsAutoTxRunning = true;
        //    var timer = new Utility.CycleTimer();

        //    try
        //    {
        //        while (IsAutoTxRunning)
        //        {
        //            timer.Start();

        //            await RunAutoTx();

        //            await timer.WaitAsync(PollingCycle);
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        IsAutoTxRunning = false;
        //        Logger.AddException(exc);
        //    }

        //}

        

    }
}
