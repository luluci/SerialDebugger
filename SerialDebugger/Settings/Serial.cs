using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerialDebugger.Settings
{
    public class Serial
    {
        public int Baudrate { get; set; } = 9600;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
        public bool Rts { get; set; } = false;
        public bool Xon { get; set; } = false;
        public bool Dtr { get; set; } = false;
        public int TxTimeout { get; set; } = 500;
        public int RxTimeout { get; set; } = 500;

        public Serial()
        {

        }

        public void AnalyzeJson(Json.Serial json)
        {
            // 設定なしの場合は初期値を使う
            if (json is null)
            {
                return;
            }

            if (json.Baudrate > 0)
            {
                Baudrate = json.Baudrate;
            }
            if (json.DataBits > 0)
            {
                DataBits = json.DataBits;
            }
            // Parity
            switch (json.Parity)
            {
                case "Even":
                case "EVEN":
                case "even":
                    Parity = Parity.Even;
                    break;
                case "Odd":
                case "ODD":
                case "odd":
                    Parity = Parity.Odd;
                    break;
                case "Mark":
                case "MARK":
                case "mark":
                    Parity = Parity.Mark;
                    break;
                case "Space":
                case "SPACE":
                case "space":
                    Parity = Parity.Space;
                    break;
                default:
                    Parity = Parity.None;
                    break;
            }
            // StopBit
            switch (json.StopBits)
            {
                case 1:
                    StopBits = StopBits.One;
                    break;
                case 1.5:
                    StopBits = StopBits.OnePointFive;
                    break;
                case 2:
                    StopBits = StopBits.Two;
                    break;
                default:
                    StopBits = StopBits.None;
                    break;
            }
            // RTS/CTS
            Rts = json.Rts;
            // XOn/XOff
            Xon = json.Xon;
            // DTR/STR
            Dtr = json.Dtr;
            // TxTimeout
            if (json.TxTimeout >= 0)
            {
                TxTimeout = json.TxTimeout;
            }
            // RxTimeout
            if (json.RxTimeout >= 0)
            {
                RxTimeout = json.RxTimeout;
            }
        }
    }


    public partial class Json
    {
        public class Serial
        {
            // Baudrate
            [JsonPropertyName("baudrate")]
            public int Baudrate { get; set; } = -1;

            // DataBits
            [JsonPropertyName("data_bits")]
            public int DataBits { get; set; } = -1;

            // Parity
            [JsonPropertyName("parity")]
            public string Parity { get; set; } = string.Empty;

            // StopBits
            [JsonPropertyName("stop_bits")]
            public double StopBits { get; set; } = -1;

            // RTS/CTS
            [JsonPropertyName("rts_cts")]
            public bool Rts { get; set; } = false;

            // XOn/XOff
            [JsonPropertyName("xon_xoff")]
            public bool Xon { get; set; } = false;

            // DTR/STR
            [JsonPropertyName("dtr_str")]
            public bool Dtr { get; set; } = false;

            // WriteTimeout
            [JsonPropertyName("tx_timeout")]
            public int TxTimeout { get; set; } = -1;

            // ReadTimeout
            [JsonPropertyName("rx_timeout")]
            public int RxTimeout { get; set; } = -1;
        }
        
    }
}
