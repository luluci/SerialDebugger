using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialDebugger.Serial
{
    
    class CommHandler
    {
        // Queue: GUI -> Comm
        public ConcurrentQueue<GuiMsg> qGui2Comm = new ConcurrentQueue<GuiMsg>();
        // Queue: Comm -> GUI
        public ConcurrentQueue<CommMsg> qComm2Gui = new ConcurrentQueue<CommMsg>();

        // Buffer
        public CommData Data { get; set; }

        public CommHandler()
        {
            
        }

        public void Init(ICollection<Comm.TxFrame> frames)
        {
            Data = new CommData();
            Data.InitTx(frames);
        }

        public Task Run(SerialPort serial, int polling)
        {
            return Task.Run(async () =>
            {
                // シリアル処理開始
                var result = await RunImpl(serial, polling);
                // 終了したらデータ解放
                Data = null;
                return result;
            });
        }

        private async Task<int> RunImpl(SerialPort serial, int polling)
        {
            bool is_loop = true;
            int read_buff = 0;
            serial.ReadTimeout = 10000;
            serial.DataReceived += Serial_DataReceived;

            while (is_loop)
            {



                UpdateTxBuffer();
                if (ProcGuiQueue())
                {
                    break;
                }

                await Task.Delay(polling);
            }

            return 0;
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void UpdateTxBuffer()
        {
            if (Data.HasUpdate)
            {
                // HasUpdate==trueを検出してifブロックに入った時点でフラグを下す。
                // バッファ更新処理では最後にHasUpdateを立てるため、
                // このブロックに突入した時点でバッファはすべて更新されている。
                Data.HasUpdate = false;

                // TryEnterによりバッファ更新中はlock解放を待たずに処理を抜ける。
                // 未取り込みの更新ありバッファの有無を記憶しておく。
                // 処理を抜けるときにtrueであればHasUpdateをtrueに戻す。
                bool rest_update = false;

                foreach (var frame in Data.TxBuffer)
                {
                    foreach (var buffer in frame)
                    {
                        if (buffer.HasUpdate)
                        {
                            if (Monitor.TryEnter(buffer, 0))
                            {
                                try
                                {
                                    // バッファ配列をすべて転送
                                    Buffer.BlockCopy(buffer.Buffer[1], 0, buffer.Buffer[0], 0, buffer.Buffer[0].Length);
                                }
                                finally
                                {
                                    Monitor.Exit(buffer);
                                }
                            }
                            else
                            {
                                rest_update = true;
                            }
                        }
                    }
                }

                if (rest_update)
                {
                    Data.HasUpdate = true;
                }
            }
        }

        /// <summary>
        /// GUI -> Comm キューを消化する
        /// </summary>
        /// <returns>通信スレッド終了有無</returns>
        private bool ProcGuiQueue()
        {
            bool result = false;

            if (qGui2Comm.TryDequeue(out GuiMsg msg))
            {
                switch (msg.Type)
                {
                    case GuiMsgType.Send:
                        var send_msg = msg as GuiMsgSend;
                        var buff = Data.TxBuffer[send_msg.FrameId][send_msg.FieldId].Buffer[0];

                        qComm2Gui.Enqueue(new CommMsgTxSend(buff.ToArray()));
                        break;

                    case GuiMsgType.Quit:
                        result = true;
                        break;

                    default:
                        break;
                }
            }

            return result;
        }
    }
}
