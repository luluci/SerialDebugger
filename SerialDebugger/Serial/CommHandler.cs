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
        public ConcurrentQueue<int> qGui2Comm = new ConcurrentQueue<int>();
        // Queue: Comm -> GUI
        public ConcurrentQueue<int> qComm2Gui = new ConcurrentQueue<int>();

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

        public Task Run(SerialPort serial)
        {
            return Task.Run(async () =>
            {
                // シリアル処理開始
                var result = await RunImpl(serial);
                // 終了したらデータ解放
                Data = null;
                return result;
            });
        }

        private async Task<int> RunImpl(SerialPort serial)
        {
            int i = 0;

            while (true)
            {
                i++;
                if (i > 10)
                {
                    break;
                }




                await Task.Delay(1000);
            }

            return 0;
        }
    }
}
