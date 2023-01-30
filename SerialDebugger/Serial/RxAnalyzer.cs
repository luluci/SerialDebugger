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
        Timeout,
        Cancel,
    }

    class RxData
    {
        public RxDataType Type { get; private set; }
        public byte[] Data { get; set; }

        public RxData()
        {
        }

        public static RxData MakeEmpty()
        {
            var data = new RxData
            {
                Type = RxDataType.Empty
            };
            return data;
        }

        public static RxData MakeTimeout(byte[] buff)
        {
            var data = new RxData
            {
                Type = RxDataType.Timeout,
                Data = buff,
            };
            return data;
        }

        public static RxData MakeCancel(byte[] buff)
        {
            var data = new RxData
            {
                Type = RxDataType.Cancel,
                Data = buff,
            };
            return data;
        }
    }

    class RxAnalyzeResult
    {
        public int FrameId { get; set; } = -1;
        public int PatternId { get; set; } = -1;
    }

    class RxAnalyzer
    {
        bool HasRecieve;

        const int BuffSize = 1024;
        byte[] RxBuff = new byte[BuffSize];
        int RxBuffOffset;

        SerialPort serial;
        public IList<Comm.RxFrame> RxFramesRef;

        public List<RxAnalyzeResult> AnalyzeResultQueue;


        public RxAnalyzer(SerialPort serial, IList<Comm.RxFrame> rxFrames)
        {
            this.serial = serial;
            RxFramesRef = rxFrames;

            // 受信ハンドラ登録
            HasRecieve = false;
            serial.DataReceived += Serial_DataReceived;
            serial.ReadTimeout = 0;

            // Queueサイズ計算
            int queue_size = 0;
            foreach (var frame in RxFramesRef)
            {
                queue_size += frame.Patterns.Count;
            }
            // Queue初期化
            AnalyzeResultQueue = new List<RxAnalyzeResult>(queue_size);
            for (int i=0; i<queue_size; i++)
            {
                AnalyzeResultQueue.Add(new RxAnalyzeResult());
            }
        }


        public Task<RxData> Run(int timeout, int polling, CancellationToken ct)
        {
            //
            RxBuffOffset = 0;
            // 受信タスク作成
            return Task.Run(async () =>
            {
                // シリアル処理開始
                return await RunImpl(timeout, polling, ct);
            }, ct);
        }

        private async Task<RxData> RunImpl(int timeout, int polling, CancellationToken ct)
        {
            // 受信開始時間
            DateTime RxBeginTime;
            // タイムアウト判定基準時間:最後に受信した時間からtimeout経過で受信終了
            var RxLastTime = DateTime.Now;

            while (true)
            {
                // Cancel
                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Cancel Requested");
                    //byte[] data;
                    //if (RxBuffOffset > 0)
                    //{
                    //    data = new byte[RxBuffOffset];
                    //    Buffer.BlockCopy(RxBuff, 0, data, 0, RxBuffOffset);
                    //}
                    //else
                    //{
                    //    data = new byte[0];
                    //}
                    //return RxData.MakeCancel(data);
                }

                // タイムアウト判定
                // 何かしらのデータ受信後、指定時間経過でタイムアウトする
                if (RxBuffOffset > 0)
                {
                    var now_time = DateTime.Now;
                    var diff_time = now_time - RxLastTime;
                    var diff_ms = diff_time.Ticks / TimeSpan.TicksPerMillisecond;
                    if (diff_ms >= timeout)
                    {
                        byte[] data = new byte[RxBuffOffset];
                        Buffer.BlockCopy(RxBuff, 0, data, 0, RxBuffOffset);
                        return RxData.MakeTimeout(data);
                    }
                }

                // 受信バッファ読み出し
                if (HasRecieve)
                {
                    // 受信開始した時間を記憶
                    if (RxBuffOffset == 0)
                    {
                        RxBeginTime = DateTime.Now;
                    }

                    HasRecieve = false;
                    try
                    {
                        var len = serial.Read(RxBuff, RxBuffOffset, BuffSize - RxBuffOffset);
                        RxBuffOffset += len;
                    }
                    catch (TimeoutException)
                    {
                        // HasRecieveを下してからReadするまでに受信した場合、
                        // 受信バッファが空でHasRecieveが立っている可能性がある。
                    }

                    // 最後に受信した時間を更新
                    RxLastTime = DateTime.Now;
                }

                await Task.Delay(polling);
            }
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            HasRecieve = true;
        }

    }
}
