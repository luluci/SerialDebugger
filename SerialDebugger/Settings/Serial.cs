using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SerialDebugger.Settings
{
    class Serial
    {
        public int Baudrate { get; set; }
        public int DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
        public int TxTimeout { get; set; }

        public Serial()
        {

        }

        public void AnalyzeJson(Json.Serial json)
        {
            Baudrate = json.Baudrate;
            DataBits = json.DataBits;
            TxTimeout = json.TxTimeout;
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
        }
    }


    partial class Json
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

            // WriteTimeout
            [JsonPropertyName("tx_timeout")]
            public int TxTimeout { get; set; } = -1;
        }
        
    }
}
