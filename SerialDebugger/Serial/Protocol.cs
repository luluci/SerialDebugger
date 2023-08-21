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
        public int MatchResultCount { get; set; }
        public bool MatchResultIsTimeout { get; set; }

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
            // 定義が存在する場合のみ処理するためのフラグを作成
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
                InitBeforeCommStart();

                IsRunning = true;
                while (IsRunning)
                {
                    // 受信開始前に解析情報系を初期化
                    await InitBeforeRx();
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
            // 通信終了通知
            CancelTokenSource.Cancel();
        }

        /// <summary>
        /// 通信開始前の初期化処理
        /// </summary>
        public void InitBeforeCommStart()
        {
            // AutoTx初期化
            foreach (var job in AutoTxJobs)
            {
                job.Reset();
            }
        }

        /// <summary>
        /// 受信シーケンス開始前の初期化処理
        /// </summary>
        /// <returns></returns>
        public async Task InitBeforeRx()
        {
            // 解析ルール初期化
            // 解析状況をリセットする
            foreach (var frame in RxFrames)
            {
                foreach (var pattern in frame.Patterns)
                {
                    if (pattern.IsActive.Value)
                    {
                        var analyzer = pattern.Analyzer;
                        analyzer.Pos = 0;
                        analyzer.IsActive = true;
                        // 受信解析Script初期設定
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
            // 結果初期化
            MatchResultPos = 0;
            // Script I/F初期化
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
            // MatchResult初期化
            MatchResultIsTimeout = false;
            MatchResultCount = 0;

            switch (Result.Type)
            {
                case RxDataType.Cancel:
                    // COM切断により通信終了
                    IsRunning = false;
                    break;

                case RxDataType.Timeout:
                    // 受信タイムアウトによる受信シーケンス終了
                    // タイムアウトありフラグセット
                    MatchResultIsTimeout = true;
                    // タイムアウトは1バイト以上の受信があるときのみ発生する。
                    // 受信バッファをログに出力して次の受信シーケンスに移行する。
                    Logger.Add($"[Rx][Timeout] {Logger.Byte2Str(Result.RxBuff, 0, Result.RxBuffOffset)}");
                    // 受信タイムアウトを自動操作に通知
                    await AutoTxExecRxEvent();
                    break;

                case RxDataType.Match:
                    // 受信解析で定義したルールにマッチ
                    // 受信内容を必要な変数に展開
                    // 受信結果(MatchResult)を別バッファにコピーすることはとりあえずしない
                    // 基本的に受信解析マッチしたタイミングでツール内に一括通報して終了する
                    // Scriptから遅れてマッチ結果を参照するためにMatchResultCountを作成する
                    // MatchResultPos!=0のときはMatchResultが更新されているので注意
                    MatchResultCount = MatchResultPos;
                    // 受信内容をログに出力
                    MakeRxLog();
                    // 受信内容を自動操作に通知
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
                // 受信ログ作成
                string log;
                if (result.PatternRef.IsLogVisualize)
                {
                    // Visualizeログ作成
                    // RxPatternへの受信値反映も同時に実施
                    log = RxFrames[frame_id].MakeLogVisualize(Result.RxBuff, Result.RxBuffOffset, result.PatternRef);
                }
                else
                {
                    // RxPatternに受信値を反映
                    RxFrames[frame_id].UpdateRxPatternDisp(Result.RxBuff, Result.RxBuffOffset, result.PatternRef);
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

        // 以下、シリアル通信制御用I/F
        // 現状でVMから直接SerialPortを使って送信しているが、
        // 送受信はprotocolに集約するようなリファクタリング構想

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frame_idx"></param>
        /// <param name="buffer_idx"></param>
        /// <returns></returns>
        public Comm.TxFieldBuffer GetTxBuffer(int frame_idx, int buffer_idx)
        {
            return TxFrames[frame_idx].Buffers[buffer_idx];
        }

        public void SendData(byte[] buff, int offset, int length)
        {
            Serial.Write(buff, offset, length);
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
