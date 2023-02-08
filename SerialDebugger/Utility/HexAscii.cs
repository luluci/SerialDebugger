using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Utility
{
    static class HexAscii
    {

        public static string[] AsciiTbl;

        private static HexAsciiImpl Impl = new HexAsciiImpl();

    }


    class HexAsciiImpl
    {
        public HexAsciiImpl()
        {
            MakeAsciiTable();
        }

        void MakeAsciiTable()
        {
            HexAscii.AsciiTbl = new string[256];
            for (int i = 0; i< 256; i++)
            {
                HexAscii.AsciiTbl[i] = i.ToString("X2");
            }
        }
    }
}
