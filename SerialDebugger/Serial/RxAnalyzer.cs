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

    class RxAnalyzer
    {
        bool HasRecieve;

        const int BuffSize = 1024;
        byte[] RxBuff = new byte[BuffSize];
        int RxBuffOffset;

        public RxAnalyzer()
        {

        }


        public Task<RxData> Run(SerialPort serial, int timeout, int polling, CancellationToken ct)
        {
            //
            RxBuffOffset = 0;
            // 受信ハンドラ登録
            HasRecieve = false;
            serial.DataReceived += Serial_DataReceived;
            serial.ReadTimeout = 0;
            // 受信タスク作成
            return Task.Run(async () =>
            {
                // シリアル処理開始
                return await RunImpl(serial, timeout, polling, ct);
            }, ct);
        }

        private async Task<RxData> RunImpl(SerialPort serial, int timeout, int polling, CancellationToken ct)
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
                var now_time = DateTime.Now;
                var diff_time = now_time - RxLastTime;
                var diff_ms = diff_time.Ticks / TimeSpan.TicksPerMillisecond;
                if (diff_ms >= timeout)
                {
                    if (RxBuffOffset > 0)
                    {
                        byte[] data = new byte[RxBuffOffset];
                        Buffer.BlockCopy(RxBuff, 0, data, 0, RxBuffOffset);

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
