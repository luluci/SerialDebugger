using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Script
{
    using Logger = Log.Log;

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class UtilityIf
    {

        public UtilityIf()
        {

        }


        public void Log(string msg)
        {
            Logger.Add(msg);
        }
    }
}
