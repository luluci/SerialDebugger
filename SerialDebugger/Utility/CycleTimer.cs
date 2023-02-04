﻿using System;
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msec"></param>
        /// <returns>
        /// 引数で指定したタイムアウト時間経過までの残り時間を返す。
        /// >  0 : 残り時間あり
        /// <= 0 : タイムアウト時間経過
        /// </returns>
        public int WaitForMsec(int msec)
        {
            var curr = DateTime.Now - prev;
            return msec - (int)(curr.Ticks / TimeSpan.TicksPerMillisecond);
        }

        public bool WaitTimeElapsed(int msec)
        {
            return (WaitForMsec(msec) <= 0);
        }
    }
}
