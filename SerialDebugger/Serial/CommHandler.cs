using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
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

            while (is_loop)
            {


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
                            is_loop = false;
                            break;

                        default:
                            break;
                    }
                }


                await Task.Delay(polling);
            }

            return 0;
        }
    }
}
