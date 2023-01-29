using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Comm
{

    enum RxMatchType
    {
        Value,
        Any,
        Timeout,
        Script
    }

    class RxMatch
    {
        public RxMatchType Type { get; set; }
        public UInt64 Value { get; set; }
        public int Msec { get; set; }
        public string Script { get; set; }

        public Field FieldRef { get; set; }
    }
}
