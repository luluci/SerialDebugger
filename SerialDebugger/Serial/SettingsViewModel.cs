using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Ports;
using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Serial
{
    using Setting = SerialDebugger.Settings.Settings;

    public class SettingsViewModel : BindableBase, IDisposable
    {
        public class ParityNode
        {
            public Parity Parity { get; set; }
            public string Disp { get; set; }
        }
        public class StopBitsNode
        {
            public StopBits StopBits { get; set; }
            public string Disp { get; set; }
        }

        public ReactiveCollection<string> ComList { get; set; }
        public ReactivePropertySlim<int> ComListSelectIndex { get; set; }
        private int[] Baudrates = { 4800, 9600, 115200 };
        public ReactiveCollection<int> BaudrateList { get; set; }
        public ReactivePropertySlim<int> BaudrateListSelectIndex { get; set; }
        public ReactivePropertySlim<int> BaudrateListSelectItem { get; set; }
        private int[] DataBits = { 5, 6, 7, 8 };
        public ReactiveCollection<int> DataBitsList { get; set; }
        public ReactivePropertySlim<int> DataBitsListSelectIndex { get; set; }
        public ReactiveCollection<ParityNode> ParityList { get; set; }
        public ReactivePropertySlim<int> ParityListSelectIndex { get; set; }
        public ReactiveCollection<StopBitsNode> StopBitsList { get; set; }
        public ReactivePropertySlim<int> StopBitsListSelectIndex { get; set; }
        public ReactiveCollection<string> RtsList { get; set; }
        public ReactivePropertySlim<int> RtsListSelectIndex { get; set; }
        public ReactiveCollection<string> XonList { get; set; }
        public ReactivePropertySlim<int> XonListSelectIndex { get; set; }
        public ReactiveCollection<string> DtrEnableList { get; set; }
        public ReactivePropertySlim<int> DtrEnableListSelectIndex { get; set; }
        public ReactivePropertySlim<int> TxTimeout { get; set; }
        public ReactivePropertySlim<bool> TxTimeoutEnable { get; set; }
        public ReactivePropertySlim<int> RxTimeout { get; set; }
        public ReactivePropertySlim<bool> RxTimeoutEnable { get; set; }
        public ReactiveCommand OnClickReload { get; set; }

        public SettingsViewModel()
        {
            // COMポート
            ComList = new ReactiveCollection<string>();
            ComList.AddTo(Disposables);
            ComListSelectIndex = new ReactivePropertySlim<int>(0);
            ComListSelectIndex.AddTo(Disposables);
            InitComPort();
            // baudrate
            // ComboBoxをEditableにして、Textから設定を読み出す
            BaudrateListSelectItem = new ReactivePropertySlim<int>();
            BaudrateListSelectItem.AddTo(Disposables);
            BaudrateList = new ReactiveCollection<int>();
            BaudrateList.AddTo(Disposables);
            foreach (int b in Baudrates)
            {
                BaudrateList.Add(b);
            }
            BaudrateListSelectIndex = new ReactivePropertySlim<int>(1);
            BaudrateListSelectIndex.AddTo(Disposables);
            // 最後に値設定
            BaudrateListSelectItem.Value = BaudrateList[BaudrateListSelectIndex.Value];
            // データサイズ
            DataBitsList = new ReactiveCollection<int>();
            DataBitsList.AddTo(Disposables);
            foreach (int d in DataBits)
            {
                DataBitsList.Add(d);
            }
            DataBitsListSelectIndex = new ReactivePropertySlim<int>(3);
            DataBitsListSelectIndex.AddTo(Disposables);
            // Parity bit
            ParityList = new ReactiveCollection<ParityNode>();
            ParityList.AddTo(Disposables);
            ParityList.Add(new ParityNode { Parity = Parity.None, Disp = "なし" });
            ParityList.Add(new ParityNode { Parity = Parity.Even, Disp = "偶数" });
            ParityList.Add(new ParityNode { Parity = Parity.Odd, Disp = "奇数" });
            ParityList.Add(new ParityNode { Parity = Parity.Space, Disp = "常に0" });
            ParityList.Add(new ParityNode { Parity = Parity.Mark, Disp = "常に1" });
            ParityListSelectIndex = new ReactivePropertySlim<int>(1);
            ParityListSelectIndex.AddTo(Disposables);
            // Stop bit
            StopBitsList = new ReactiveCollection<StopBitsNode>();
            StopBitsList.AddTo(Disposables);
            StopBitsList.Add(new StopBitsNode { StopBits = StopBits.None, Disp = "なし" });
            StopBitsList.Add(new StopBitsNode { StopBits = StopBits.One, Disp = "1bit" });
            StopBitsList.Add(new StopBitsNode { StopBits = StopBits.OnePointFive, Disp = "1.5bit" });
            StopBitsList.Add(new StopBitsNode { StopBits = StopBits.Two, Disp = "2bit" });
            StopBitsListSelectIndex = new ReactivePropertySlim<int>(1);
            StopBitsListSelectIndex.AddTo(Disposables);
            // フロー制御
            // RTS/CTS
            RtsList = new ReactiveCollection<string>();
            RtsList.AddTo(Disposables);
            RtsList.Add("Disable");
            RtsList.Add("Enable");
            RtsListSelectIndex = new ReactivePropertySlim<int>(0);
            RtsListSelectIndex.AddTo(Disposables);
            // XOn/XOff
            XonList = new ReactiveCollection<string>();
            XonList.AddTo(Disposables);
            XonList.Add("Disable");
            XonList.Add("Enable");
            XonListSelectIndex = new ReactivePropertySlim<int>(0);
            XonListSelectIndex.AddTo(Disposables);
            // DTR Enable
            DtrEnableList = new ReactiveCollection<string>();
            DtrEnableList.AddTo(Disposables);
            DtrEnableList.Add("Disable");
            DtrEnableList.Add("Enable");
            DtrEnableListSelectIndex = new ReactivePropertySlim<int>(0);
            DtrEnableListSelectIndex.AddTo(Disposables);
            // タイムアウト
            TxTimeout = new ReactivePropertySlim<int>(1000);
            TxTimeout.AddTo(Disposables);
            TxTimeoutEnable = new ReactivePropertySlim<bool>(false);
            TxTimeoutEnable.AddTo(Disposables);
            RxTimeout = new ReactivePropertySlim<int>(1000);
            RxTimeout.AddTo(Disposables);
            RxTimeoutEnable = new ReactivePropertySlim<bool>(false);
            RxTimeoutEnable.AddTo(Disposables);

            // COMポート再読み込み
            OnClickReload = new ReactiveCommand();
            OnClickReload.Subscribe(x =>
                {
                    InitComPort();
                })
                .AddTo(Disposables);
            
        }

        public void InitComPort()
        {
            // COMポート
            string[] ports = SerialPort.GetPortNames();
            ComList.Clear();
            foreach (var port in ports)
            {
                ComList.Add(port);
            }
            ComListSelectIndex.Value = 0;
        }

        public void SetSerialSetting(SerialDebugger.Settings.Serial serial)
        {
            // Baudrate
            BaudrateListSelectItem.Value = serial.Baudrate;
            // DataBits
            var idx = DataBitsList.IndexOf(serial.DataBits);
            if (idx != -1)
            {
                DataBitsListSelectIndex.Value = idx;
            }
            // Parity
            switch (serial.Parity)
            {
                case Parity.Even:
                    ParityListSelectIndex.Value = 1;
                    break;
                case Parity.Odd:
                    ParityListSelectIndex.Value = 2;
                    break;
                case Parity.Space:
                    ParityListSelectIndex.Value = 3;
                    break;
                case Parity.Mark:
                    ParityListSelectIndex.Value = 4;
                    break;
                case Parity.None:
                default:
                    ParityListSelectIndex.Value = 0;
                    break;
            }
            // StopBit
            switch (serial.StopBits)
            {
                case StopBits.One:
                    StopBitsListSelectIndex.Value = 1;
                    break;
                case StopBits.OnePointFive:
                    StopBitsListSelectIndex.Value = 2;
                    break;
                case StopBits.Two:
                    StopBitsListSelectIndex.Value = 3;
                    break;
                case StopBits.None:
                default:
                    StopBitsListSelectIndex.Value = 0;
                    break;
            }
            // RTS/CTS
            RtsListSelectIndex.Value = serial.Rts ? 1 : 0;
            // XOn/XOff
            XonListSelectIndex.Value = serial.Xon ? 1 : 0;
            // DTR/STR
            DtrEnableListSelectIndex.Value = serial.Dtr ? 1 : 0;
            // TxTimeout
            TxTimeout.Value = serial.TxTimeout;
            TxTimeoutEnable.Value = serial.TxTimeout != -1;
            // RxTimeout
            RxTimeout.Value = serial.RxTimeout;
            RxTimeoutEnable.Value = serial.RxTimeout != -1;
        }

        public SerialPort GetSerialPort()
        {
            // Handshake作成
            var handshake = Handshake.None;
            var rts = RtsListSelectIndex.Value == 1;
            var xon = XonListSelectIndex.Value == 1;
            if (rts && xon)
            {
                handshake = Handshake.RequestToSendXOnXOff;
            }
            else if (rts)
            {
                handshake = Handshake.RequestToSend;
            }
            else if (xon)
            {
                handshake = Handshake.XOnXOff;
            }
            // timeout
            int txtimeout = -1;
            if (TxTimeoutEnable.Value)
            {
                txtimeout = TxTimeout.Value;
            }
            int rxtimeout = -1;
            if (RxTimeoutEnable.Value)
            {
                rxtimeout = RxTimeout.Value;
            }

            return new SerialPort
            {
                PortName = ComList[ComListSelectIndex.Value],
                BaudRate = BaudrateListSelectItem.Value,
                DataBits = DataBitsList[DataBitsListSelectIndex.Value],
                Parity = ParityList[ParityListSelectIndex.Value].Parity,
                StopBits = StopBitsList[StopBitsListSelectIndex.Value].StopBits,
                // フロー制御
                DtrEnable = (DtrEnableListSelectIndex.Value == 1),
                Handshake = handshake,
                // Timeout
                WriteTimeout = txtimeout,
                ReadTimeout = rxtimeout,
            };
        }

        #region IDisposable Support
        private CompositeDisposable Disposables { get; } = new CompositeDisposable();

        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージド状態を破棄します (マネージド オブジェクト)。
                    this.Disposables.Dispose();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~TxFrame() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        void IDisposable.Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion


    }
}
