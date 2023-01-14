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
using System.Windows.Controls;
using System.Windows;

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
        public ReactivePropertySlim<bool> IsSerialOpen { get; set; }
        public ReactivePropertySlim<string> TextSerialOpen { get; set; }
        public ReactiveCommand OnClickSerialOpen { get; set; }
        public ReactiveCommand OnClickSerialSetting { get; set; }
        public ReadOnlyReactivePropertySlim<bool> IsEnableSerialSetting { get; set; }
        Popup popup;
        public Serial.CommHandler serialHandler;
        // Comm
        public ReactiveCollection<Comm.TxFrame> TxFrames { get; set; }
        public ReactiveCommand OnClickTxDataSend { get; set; }
        public ReactiveCommand OnClickTxBufferSend { get; set; }
        // Log
        public ReactiveCollection<string> Log { get; set; }
        // ベースGUI
        MainWindow window;
        UIElement BaseSerialTxOrig;
        public ReactivePropertySlim<string> BaseSerialTxMsg { get; set; }

        // Debug
        public ReactiveCommand OnClickTestSend { get; set; }

        public MainWindowViewModel(MainWindow window)
        {
            this.window = window;
            // 初期表示のGridは動的に入れ替えるので最初に参照を取得しておく
            BaseSerialTxOrig = window.BaseSerialTx.Children[0];
            BaseSerialTxMsg = new ReactivePropertySlim<string>("設定ファイルを読み込んでいます...");
            BaseSerialTxMsg.AddTo(Disposables);

            // Serial
            serialSetting = new Serial.Settings();
            serialHandler = new Serial.CommHandler();
            // Serial Open
            IsSerialOpen = new ReactivePropertySlim<bool>(false);
            IsSerialOpen.AddTo(Disposables);
            TextSerialOpen = new ReactivePropertySlim<string>("COM接続");
            TextSerialOpen.AddTo(Disposables);
            OnClickSerialOpen = new ReactiveCommand();
            OnClickSerialOpen.Subscribe(async (x) => 
                {
                    await SerialMain();
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
            Settings = new ReactiveCollection<Settings.SettingInfo>();
            Settings.AddTo(Disposables);
            SettingsSelectIndex = new ReactivePropertySlim<int>(mode:ReactivePropertyMode.DistinctUntilChanged);
            SettingsSelectIndex
                .Subscribe(async (int idx) =>
                {
                    await UpdateTxAsync();
                })
                .AddTo(Disposables);
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
            OnClickTestSend.Subscribe(async (x) =>
                {
                    //SerialWrite_test();
                    var task = Script.Interpreter.Engine.wv.CoreWebView2.ExecuteScriptAsync($@"

                        (function () {{
                            //処理
                            try {{
                                return {{ code: -1, result: test_func_async()}};
                            }} catch (e) {{
                                return {{ code: -1, result: e.message}};
                            }}
                            return 2;
                        }}())
                    ");
                    int i = 0;
                    i++;
                    var result = await task;
                    i++;
                })
                .AddTo(Disposables);
        }

        public async Task InitAsync()
        {
            // 設定ファイル読み込み
            await Setting.InitAsync(Settings);

            // 有効な設定ファイルを読み込んでいたら
            if (Settings.Count > 0)
            {
                SettingsSelectIndex.Value = 0;
                var data = Settings[SettingsSelectIndex.Value];
                // 未ロードファイルならロード処理
                if (!data.IsLoaded)
                {
                    await Setting.LoadAsync(data);
                }
                // 有効な通信フォーマットがあればツールに取り込む
                if (data.Comm.Tx.Count > 0)
                {
                    // GUI作成
                    window.Width = data.Gui.Window.Width;
                    window.Height = data.Gui.Window.Height;
                    TxFrames = data.Comm.Tx;
                    var grid = Comm.TxGui.Make(data);
                    // GUI反映
                    window.BaseSerialTx.Children.Clear();
                    window.BaseSerialTx.Children.Add(grid);
                }
                else
                {
                    BaseSerialTxMsg.Value = "有効な送信設定が存在しません。";
                    TxFrames = new ReactiveCollection<Comm.TxFrame>();
                    TxFrames.AddTo(Disposables);
                }
                // COMポート設定更新
                serialSetting.vm.SetSerialSetting(data.Serial);
            }
            else
            {
                BaseSerialTxMsg.Value = "有効な設定ファイルが存在しません。";
                Logger.Add("有効な設定ファイルが存在しません。");
            }
        }

        public async Task UpdateTxAsync()
        {
            // 現在表示中のGUIを破棄
            window.BaseSerialTx.Children.Clear();
            window.BaseSerialTx.Children.Add(BaseSerialTxOrig);


            try
            {
                // 選択した設定ファイルを取得
                var data = Settings[SettingsSelectIndex.Value];
                // 未ロードファイルならロード処理
                if (!data.IsLoaded)
                {
                    await Setting.LoadAsync(data);
                }
                TxFrames = data.Comm.Tx;

                // 有効な通信フォーマットがあればツールに取り込む
                if (data.Comm.Tx.Count > 0)
                {
                    // Window設定を反映
                    window.Width = data.Gui.Window.Width;
                    window.Height = data.Gui.Window.Height;
                    // GUI作成
                    var grid = Comm.TxGui.Make(data);
                    window.BaseSerialTx.Children.Clear();
                    window.BaseSerialTx.Children.Add(grid);
                    // COMポート設定更新
                    serialSetting.vm.SetSerialSetting(data.Serial);
                }
                else
                {
                    BaseSerialTxMsg.Value = "有効な送信設定が存在しません。";
                    TxFrames = new ReactiveCollection<Comm.TxFrame>();
                    TxFrames.AddTo(Disposables);
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Setting File Load Error: {ex.Message}");
                Logger.AddException(ex, "設定ファイル読み込みエラー:");
                BaseSerialTxMsg.Value = "有効な送信設定が存在しません。";
            }

        }

        private async Task SerialMain()
        {
            try
            {
                if (!IsSerialOpen.Value)
                {
                    using (var serialPort = serialSetting.vm.GetSerialPort())
                    {
                        // シリアルポートを開く
                        serialPort.Open();
                        // COM切断を有効化
                        IsSerialOpen.Value = true;
                        TextSerialOpen.Value = "COM切断";
                        // シリアル通信管理ハンドラを別スレッドで起動
                        // スレッドが終了するまで待機
                        await serialHandler.Run(serialPort);
                        IsSerialOpen.Value = false;
                        TextSerialOpen.Value = "COM接続";
                    }
                }
                else
                {
                    // スレッド終了メッセージ送信
                }
            }
            catch (Exception e)
            {
                IsSerialOpen.Value = false;
                TextSerialOpen.Value = "COM接続";
                Logger.Add($"COM Open Error: {e.Message}");
            }
        }

        private void SerialWrite(ReactiveCollection<byte> buff)
        {
            if (IsSerialOpen.Value)
            {
                try
                {
                    //serialPort.Write(buff.ToArray(), 0, buff.Count);
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
                        //serialPort.Write(buff.ToArray(), 0, buff.Count);
                    }
                    System.Threading.Thread.Sleep(1000);
                    {
                        var buff = TxFrames[1].TxBuffer;
                        //serialPort.Write(buff.ToArray(), 0, buff.Count);
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
