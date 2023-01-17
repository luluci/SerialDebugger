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


    enum GuiMsgType
    {
        Quit,       // 通信終了
        Send,       // 手動送信
    }

    class GuiMsg
    {
        public GuiMsgType Type { get; }

        public GuiMsg(GuiMsgType type)
        {
            Type = type;
        }
    }

    class GuiMsgQuit : GuiMsg
    {
        public GuiMsgQuit() : base(GuiMsgType.Quit)
        {

        }
    }

    class GuiMsgSend : GuiMsg
    {
        public int FrameId { get; }
        public int FieldId { get; }

        public GuiMsgSend(int frameId, int fieldId) : base(GuiMsgType.Send)
        {
            FrameId = frameId;
            FieldId = fieldId;
        }
    }



    enum CommMsgType
    {
        TxSend,
    }
    /// <summary>
    /// 通信スレッド→GUIメッセージ ベース
    /// </summary>
    class CommMsg
    {
        public CommMsgType Type { get; }
        public long Timestamp { get; }

        public CommMsg(CommMsgType type)
        {
            Type = type;
            Timestamp = DateTime.Now.Ticks;
        }

        public void Invoke()
        {
            //throw new NotImplementedException("");
            switch (Type)
            {
                case CommMsgType.TxSend:
                    {
                        var msg = this as CommMsgTxSend;
                        msg.Invoke();
                    }
                    break;
            }
        }
    }

    class CommMsgTxSend : CommMsg
    {
        public string TxSendData { get; }

        public CommMsgTxSend(string data) : base(CommMsgType.TxSend)
        {
            TxSendData = data;
        }

        public new void Invoke()
        {
            Log.Log.Add(TxSendData);
        }
    }
    

    //class UpdateTxBuffMsg
    //{
    //    public enum MsgType
    //    {
    //        UpdateBuffer,
    //        UpdateBackupBuffer
    //    }
    //    public MsgType Type { get; private set; }
    //    public int Idx1 { get; private set; }
    //    public int Idx2 { get; private set; }
    //    public UInt64 Value { get; private set; }

    //    public UpdateTxBuffMsg(MsgType type, int idx1, int idx2, UInt64 value)
    //    {
    //        Type = type;
    //        Idx1 = idx1;
    //        Idx2 = idx2;
    //        Value = value;
    //    }

    //    /// <summary>
    //    /// インスタンス使いまわし用
    //    /// </summary>
    //    /// <param name="type"></param>
    //    /// <param name="idx1"></param>
    //    /// <param name="idx2"></param>
    //    public void Update(MsgType type, int idx1, int idx2, UInt64 value)
    //    {
    //        Type = type;
    //        Idx1 = idx1;
    //        Idx2 = idx2;
    //        Value = value;
    //    }
    //}
}
