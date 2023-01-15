using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Serial
{
    class Message
    {
    }


    class UpdateTxBuffMsg
    {
        public enum MsgType
        {
            UpdateBuffer,
            UpdateBackupBuffer
        }
        public MsgType Type { get; private set; }
        public int Idx1 { get; private set; }
        public int Idx2 { get; private set; }
        public UInt64 Value { get; private set; }

        public UpdateTxBuffMsg(MsgType type, int idx1, int idx2, UInt64 value)
        {
            Type = type;
            Idx1 = idx1;
            Idx2 = idx2;
            Value = value;
        }

        /// <summary>
        /// インスタンス使いまわし用
        /// </summary>
        /// <param name="type"></param>
        /// <param name="idx1"></param>
        /// <param name="idx2"></param>
        public void Update(MsgType type, int idx1, int idx2, UInt64 value)
        {
            Type = type;
            Idx1 = idx1;
            Idx2 = idx2;
            Value = value;
        }
    }
}
