using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SerialDebugger.Serial
{


    class CommTxBuffer
    {
        public int Id { get; }
        // バッファ情報
        public readonly int BufferMultiSize = 2;
        public int BuuferSize { get; }
        // byte[0:Commスレッド用][バッファサイズ]
        // byte[1:GUIスレッドから通知用][バッファサイズ]
        public byte[][] Buffer { get; set; }
        // 排他管理
        public bool HasUpdate { get; set; }
        private object BufferLocker = new object();

        public CommTxBuffer(int id, ICollection<byte> buff)
        {
            Id = id;
            BuuferSize = buff.Count;

            Buffer = new byte[BufferMultiSize][];
            for (int i = 0; i < BufferMultiSize; i++)
            {
                Buffer[i] = buff.ToArray();
            }
        }
    }




    class CommData
    {
        public List<List<CommTxBuffer>> TxBuffer { get; set; }

        public CommData()
        {

        }


        public void InitTx(ICollection<Comm.TxFrame> frames)
        {
            foreach (var frame in frames)
            {
                //
                var buff = new List<CommTxBuffer>();
                TxBuffer.Add(buff);
                // IDを降ってバッファ内容をコピー
                // TxBuffer: 0
                int id = 0;
                buff.Add(new CommTxBuffer(id, frame.TxBuffer));
                id++;
                // BackupBuffer: 1～
                foreach (var bkbuff in frame.BackupBuffer)
                {
                    buff.Add(new CommTxBuffer(id, bkbuff.Buffer));
                    id++;
                }
            }
        }

        public void UpdateTx()
        {

        }
    }
}
