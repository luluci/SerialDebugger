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

        public CommHandler()
        {
            
        }

        public Task Run(SerialPort serial)
        {
            return Task.Run(async () =>
            {
                return await RunImpl(serial);
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
