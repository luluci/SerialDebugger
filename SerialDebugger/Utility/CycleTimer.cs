using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Utility
{
    class CycleTimer
    {
        DateTime prev;


        public CycleTimer()
        {
            Start();
        }

        public void Start()
        {
            prev = DateTime.Now;
        }

        public void WaitThread(int msec)
        {
            var wait = WaitForMsec(msec);
            if (wait > 0)
            {
                System.Threading.Thread.Sleep(WaitForMsec(msec));
            }
        }

        public async Task WaitAsync(int msec)
        {
            var wait = WaitForMsec(msec);
            if (wait > 0)
            {
                await Task.Delay(WaitForMsec(msec));
            }
        }

        public int WaitForMsec(int msec)
        {
            var curr = DateTime.Now - prev;
            return msec - (int)(curr.Ticks / TimeSpan.TicksPerMillisecond);
        }
    }
}
