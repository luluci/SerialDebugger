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
        private int[] DataBits = { 7, 8, 9 };
        public ReactiveCollection<int> DataBitsList { get; set; }
        public ReactivePropertySlim<int> DataBitsListSelectIndex { get; set; }
        public ReactiveCollection<ParityNode> ParityList { get; set; }
        public ReactivePropertySlim<int> ParityListSelectIndex { get; set; }
        public ReactiveCollection<StopBitsNode> StopBitsList { get; set; }
        public ReactivePropertySlim<int> StopBitsListSelectIndex { get; set; }
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
            // データサイズ
            DataBitsList = new ReactiveCollection<int>();
            DataBitsList.AddTo(Disposables);
            foreach (int d in DataBits)
            {
                DataBitsList.Add(d);
            }
            DataBitsListSelectIndex = new ReactivePropertySlim<int>(1);
            DataBitsListSelectIndex.AddTo(Disposables);
            // Parity bit
            ParityList = new ReactiveCollection<ParityNode>();
            ParityList.AddTo(Disposables);
            ParityList.Add(new ParityNode { Parity = Parity.None, Disp = "なし" });
            ParityList.Add(new ParityNode { Parity = Parity.Even, Disp = "偶数" });
            ParityList.Add(new ParityNode { Parity = Parity.Odd, Disp = "奇数" });
            ParityList.Add(new ParityNode { Parity = Parity.Space, Disp = "常に0" });
            ParityList.Add(new ParityNode { Parity = Parity.Mark, Disp = "常に1" });
            ParityListSelectIndex = new ReactivePropertySlim<int>(0);
            ParityListSelectIndex.AddTo(Disposables);
            // Stop bit
            StopBitsList = new ReactiveCollection<StopBitsNode>();
            StopBitsList.AddTo(Disposables);
            StopBitsList.Add(new StopBitsNode { StopBits = StopBits.None, Disp = "なし" });
            StopBitsList.Add(new StopBitsNode { StopBits = StopBits.One, Disp = "1bit" });
            StopBitsList.Add(new StopBitsNode { StopBits = StopBits.OnePointFive, Disp = "1.5bit" });
            StopBitsList.Add(new StopBitsNode { StopBits = StopBits.Two, Disp = "2bit" });
            StopBitsListSelectIndex = new ReactivePropertySlim<int>(0);
            StopBitsListSelectIndex.AddTo(Disposables);
            
            // COMポート再読み込み
            OnClickReload = new ReactiveCommand();
            OnClickReload.Subscribe(x =>
                {
                    InitComPort();
                })
                .AddTo(Disposables);

            /*
            var serial = new SerialPort
            {
                PortName = "",
                BaudRate = 0,
                Parity = Parity.None,
                StopBits = StopBits.None,
            };
            */
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

        public SerialPort GetSerialPort()
        {
            return new SerialPort
            {
                PortName = ComList[ComListSelectIndex.Value],
                BaudRate = BaudrateListSelectItem.Value,
                DataBits = DataBitsList[DataBitsListSelectIndex.Value],
                Parity = ParityList[ParityListSelectIndex.Value].Parity,
                StopBits = StopBitsList[StopBitsListSelectIndex.Value].StopBits,
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
