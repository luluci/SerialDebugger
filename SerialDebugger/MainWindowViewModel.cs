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
using System.Windows.Controls.Primitives;

namespace SerialDebugger
{
    using Logger = Log.Log;
    using Setting = Settings.Settings;

    class MainWindowViewModel : BindableBase, IDisposable
    {
        // Settings
        public ReactiveCollection<Settings.SettingInfo> Settings { get; set; }
        public ReactivePropertySlim<int> SettingsSelectIndex { get; set; }
        // Serial
        Serial.Settings serialSetting;
        SerialPort serialPort = null;
        public ReactivePropertySlim<bool> IsSerialOpen { get; set; }
        public ReactivePropertySlim<string> TextSerialOpen { get; set; }
        public ReactiveCommand OnClickSerialOpen { get; set; }
        public ReactiveCommand OnClickSerialSetting { get; set; }
        public ReadOnlyReactivePropertySlim<bool> IsEnableSerialSetting { get; set; }
        Popup popup;
        // Comm
        public ReactiveCollection<Comm.TxFrame> TxFrames { get; set; }
        public ReactiveCommand OnClickTxDataSend { get; set; }
        public ReactiveCommand OnClickTxBufferSend { get; set; }
        // Log
        public ReactiveCollection<string> Log { get; set; }
        //
        MainWindow window;

        // Debug
        public ReactiveCommand OnClickTestSend { get; set; }

        public MainWindowViewModel(MainWindow window)
        {
            // Logger設定
            Logger.Init(window);
            this.window = window;
            // 設定ファイル読み込み
            Setting.Init(0);
            //
            window.Width = Setting.Data.Gui.Window.Width;
            window.Height = Setting.Data.Gui.Window.Height;

            // Serial
            serialSetting = new Serial.Settings();
            // Serial Open
            IsSerialOpen = new ReactivePropertySlim<bool>(false);
            IsSerialOpen.AddTo(Disposables);
            TextSerialOpen = new ReactivePropertySlim<string>("COM接続");
            TextSerialOpen.AddTo(Disposables);
            OnClickSerialOpen = new ReactiveCommand();
            OnClickSerialOpen.Subscribe(x => 
                {
                    try
                    {
                        if (!IsSerialOpen.Value)
                        {
                            serialPort = serialSetting.vm.GetSerialPort();
                            serialPort.Open();

                            IsSerialOpen.Value = true;
                            TextSerialOpen.Value = "COM切断";
                        }
                        else
                        {
                            serialPort.Close();
                            serialPort = null;
                            IsSerialOpen.Value = false;
                            TextSerialOpen.Value = "COM接続";
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Add($"COM Open Error: {e.Message}");
                    }
                })
                .AddTo(Disposables);
            // 設定ボタン
            IsEnableSerialSetting = IsSerialOpen
                .Inverse()
                .ToReadOnlyReactivePropertySlim<bool>();
            OnClickSerialSetting = new ReactiveCommand();
            OnClickSerialSetting
                .Subscribe(x =>
                {
                    popup.PlacementTarget = window.BtnSettings;
                    popup.IsOpen = !popup.IsOpen;
                })
                .AddTo(Disposables);
            popup = new Popup();
            popup.StaysOpen = false;
            popup.Child = serialSetting;
            // Settingファイル選択GUI
            Settings = Setting.DataList;
            SettingsSelectIndex = new ReactivePropertySlim<int>(mode:ReactivePropertyMode.DistinctUntilChanged);
            SettingsSelectIndex
                .Subscribe((int idx) =>
                {
                    window.BaseSerialTx.Children.Clear();
                    Setting.Select(idx);
                    TxFrames = Setting.Data.Comm.Tx;
                    // GUI構築する
                    window.Width = Setting.Data.Gui.Window.Width;
                    window.Height = Setting.Data.Gui.Window.Height;
                    Comm.TxGui.Make(window.BaseSerialTx, TxFrames);
                    // COMポート設定更新
                    serialSetting.vm.SetSerialSetting(Setting.Data.Serial);
                })
                .AddTo(Disposables);
            // 有効な設定ファイルを読み込んでいたら
            if (Settings.Count > 0)
            {
                // 有効な通信フォーマットがあればツールに取り込む
                if (Setting.Data.Comm.Tx.Count > 0)
                {
                    // 先頭要素を選択
                    TxFrames = Setting.Data.Comm.Tx;
                    // GUI構築する
                    Comm.TxGui.Make(window.BaseSerialTx, TxFrames);
                }
                else
                {
                    TxFrames = new ReactiveCollection<Comm.TxFrame>();
                    TxFrames.AddTo(Disposables);
                }
                // COMポート設定更新
                serialSetting.vm.SetSerialSetting(Setting.Data.Serial);
            }
            OnClickTxDataSend = new ReactiveCommand();
            OnClickTxDataSend
                .Subscribe(x => {
                    var bytes = (ReactiveCollection<byte>)x;
                    SerialWrite(bytes);
                })
                .AddTo(Disposables);
            OnClickTxBufferSend = new ReactiveCommand();
            OnClickTxBufferSend.AddTo(Disposables);

            // Log
            Log = Logger.GetLogData();

            // test
            OnClickTestSend = new ReactiveCommand();
            OnClickTestSend.Subscribe(x =>
                {
                    SerialWrite_test();
                })
                .AddTo(Disposables);
        }

        private void SerialWrite(ReactiveCollection<byte> buff)
        {
            if (IsSerialOpen.Value)
            {
                try
                {
                    serialPort.Write(buff.ToArray(), 0, buff.Count);
                }
                catch (Exception ex)
                {
                    Logger.Add($"送信エラー: {ex.Message}");
                }
            }
            else
            {
                Logger.Add("error: COMポート未接続");
            }
        }

        private void SerialWrite_test()
        {
            if (IsSerialOpen.Value)
            {
                try
                {
                    {
                        var buff = TxFrames[0].TxBuffer;
                        serialPort.Write(buff.ToArray(), 0, buff.Count);
                    }
                    System.Threading.Thread.Sleep(1000);
                    {
                        var buff = TxFrames[1].TxBuffer;
                        serialPort.Write(buff.ToArray(), 0, buff.Count);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Add($"送信エラー: {ex.Message}");
                }
            }
            else
            {
                Logger.Add("error: COMポート未接続");
            }
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
