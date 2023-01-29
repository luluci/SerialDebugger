using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Comm
{

    class RxAnalyzer
    {

        public List<RxAnalyzeRule> Rules { get; set; }

        public RxAnalyzer()
        {
            Rules = new List<RxAnalyzeRule>();
        }

    }
}
