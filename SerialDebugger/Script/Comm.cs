using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Runtime.InteropServices;

namespace SerialDebugger.Script
{
    using Logger = Log.Log;

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CommIf
    {
        // Commデータへの参照
        // Comm: Tx
        public ReactiveCollection<SerialDebugger.Comm.TxFrame> TxFramesRef { get; set; }
        // Comm: Rx
        public ReactiveCollection<SerialDebugger.Comm.RxFrame> RxFramesRef { get; set; }
        // Comm: AutoTx
        public ReactiveCollection<SerialDebugger.Comm.AutoTxJob> AutoTxJobsRef { get; set; }
        // Serial: Protocol
        public Serial.Protocol ProtocolRef { get; set; }
        public MainWindowViewModel ToolRef { get; set; }

        // WebView2向けI/F
        public CommTxFramesIf Tx { get; set; }
        public CommAutoTxJobsIf AutoTx { get; set; }
        public CommRxFramesIf Rx { get; set; }
        public SerialMatchResultsIf RxMatch { get; set; }

        // 送信データバッファ
        public byte[] Data { get; set; }

        public int DebugValue { get; set; } = 0;

        public CommIf()
        {
            Tx = new CommTxFramesIf();
            AutoTx = new CommAutoTxJobsIf();
            RxMatch = new SerialMatchResultsIf();
            Rx = new CommRxFramesIf();

            ProtocolRef = null;

            // WebView2からのデータを受け取るバッファ
            // 256サイズで初期化
            Data = new byte[256];
        }

        public void Init(ReactiveCollection<SerialDebugger.Comm.TxFrame> tx, ReactiveCollection<SerialDebugger.Comm.RxFrame> rx, ReactiveCollection<SerialDebugger.Comm.AutoTxJob> autotx)
        {
            //
            TxFramesRef = tx;
            RxFramesRef = rx;
            AutoTxJobsRef = autotx;
            //
            Tx.TxFramesRef = tx;
            AutoTx.AutoTxJobsRef = autotx;
            Rx.RxFrames(rx);
        }
        public void Init(MainWindowViewModel tool)
        {
            //
            ToolRef = tool;
        }
        public void Init(Serial.Protocol protocol)
        {
            //
            ProtocolRef = protocol;
            Tx.ProtocolRef = protocol;
            RxMatch.ProtocolRef = protocol;
        }

        public bool IsSerialOpen()
        {
            return ProtocolRef.IsSerialOpen;
        }

        public string[] GetComPortList()
        {
            var list = ToolRef.ScriptIfGetComPortList();
            return list;
        }
        public void RefreshComPortList()
        {
            ToolRef.ScriptIfRefreshComPortList();
        }

        public bool OpenSerial(string name)
        {
            return ToolRef.ScriptIfOpenSerial(name);
        }
        public void CloseSerial()
        {
            ToolRef.ScriptIfCloseSerial();
        }

        public Int64 TxField(int frame_id, int field_id)
        {
            return TxFramesRef[frame_id].Buffers[0].FieldValues[field_id].Value.Value;
        }

        public void SetTxField(int frame_id, int field_id, Int64 value)
        {

        }

        public void SetTxData(int frame_id, int field_id, Int64 value)
        {

        }

        public void SendData(object[] data, int len)
        {
            try
            {
                // COMポート未接続なら終了
                if (!IsSerialOpen())
                {
                    Logger.Add($"Script Warning: Comm.SendData : COMポートを開いていません");
                    return;
                }
                // 配列サイズが送信サイズ未満なら終了
                if (data.Length < len)
                {
                    Logger.Add($"Script Warning: Comm.SendData : 配列サイズ({data.Length})が送信サイズ({len})未満です");
                    return;
                }

                // バッファサイズチェック
                // 既存の中身はすべて捨てていいので新しく領域確保
                if (Data.Length < len)
                {
                    Data = new byte[len];
                }
                // webView2/JavaScriptから配列を渡す場合、objectにしないといけない
                // objectはintである前提で処理する
                // int以外が混ざっていたら例外で処理を終了する
                // len分を送信する
                for (int idx = 0; idx < len; idx++)
                {
                    Data[idx] = (byte)(int)data[idx];
                }
                // データ送信
                ProtocolRef.SendData(Data, 0, len);
                // Log出力
                Logger.Add($"[Tx][Script] {Logger.Byte2Str(Data, 0, len)}");
            }
            catch (Exception e)
            {
                Logger.Add($"Script Error: Comm.SendData : {e.Message}");
            }
        }

        public void Debug()
        {
            // WebView2/JavaScript側からのコールがGUIスレッドで呼ばれるっぽい
            DebugValue++;
        }

        public void Error(string msg)
        {
            DebugValue++;
        }
    }
    
}
