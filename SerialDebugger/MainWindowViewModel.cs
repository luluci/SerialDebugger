using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Ports;
using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Data;
using System.Threading;

namespace SerialDebugger
{
    using System.Reactive.Linq;
    using Utility;
    using Logger = Log.Log;
    using Setting = Settings.Settings;

    class MainWindowViewModel : BindableBase, IDisposable, IClosing
    {
        // Title
        static string ToolTitle = "SerialDebugger";
        public ReactivePropertySlim<string> WindowTitle { get; set; }
        // Settings
        public ReactiveCollection<Settings.SettingInfo> Settings { get; set; }
        public ReactivePropertySlim<int> SettingsSelectIndex { get; set; }
        // Serial
        Serial.Settings serialSetting;
        public ReactivePropertySlim<bool> IsSerialOpen { get; set; }
        public ReactivePropertySlim<string> TextSerialOpen { get; set; }
        public ReactiveCommand OnClickSerialOpen { get; set; }
        public ReactivePropertySlim<bool> IsEnableSerialOpen { get; set; }
        public ReactiveCommand OnClickSerialSetting { get; set; }
        public ReadOnlyReactivePropertySlim<bool> IsEnableSerialSetting { get; set; }
        Popup popup;
        //public Serial.CommHandler serialHandler;
        // Comm: Tx
        public ReactiveCollection<Comm.TxFrame> TxFrames { get; set; }
        public ReactiveCommand OnClickTxDataSend { get; set; }
        // Tx Shortcut定義
        public class TxShortcutNode
        {
            public string Name { get; }
            public Comm.TxFieldBuffer Buffer { get; }

            public TxShortcutNode(string name, Comm.TxFieldBuffer buffer)
            {
                Name = name;
                Buffer = buffer;
            }
        }
        public ReactiveCollection<TxShortcutNode> TxShortcut { get; set; }
        public ReactivePropertySlim<int> TxShortcutSelectedIndex { get; set; }
        public ReactivePropertySlim<string> TxShortcutButtonDisp { get; set; }
        public ReactiveCommand OnClickTxShortcut { get; set; }
        // Comm: Rx
        public ReactiveCollection<Comm.RxFrame> RxFrames { get; set; }
        // Comm: AutoTx
        public ReactiveCollection<Comm.AutoTxJob> AutoTxJobs { get; set; }
        public ReactiveCollection<Comm.AutoTxJob> EmptyAutoTxJobs { get; set; }
        public ReactivePropertySlim<int> AutoTxShortcutSelectedIndex { get; set; }
        public ReactivePropertySlim<string> AutoTxShortcutButtonDisp { get; set; }
        public ReactiveCommand OnClickAutoTxShortcut { get; set; }
        // Script
        public ReactiveCommand OnClickOpenScript { get; set; }
        // Log
        public ReactiveCollection<string> Log { get; set; }
        // ベースGUI
        MainWindow window;
        UIElement BaseSerialTxOrig;
        UIElement BaseSerialRxOrig;
        UIElement BaseSerialAutoTxOrig;
        public ReactivePropertySlim<string> BaseSerialTxMsg { get; set; }
        public ReactivePropertySlim<string> BaseSerialRxMsg { get; set; }
        public ReactivePropertySlim<string> BaseSerialAutoTxMsg { get; set; }

        // シリアル通信管理変数
        SerialPort serialPort;
        Serial.Protocol protocol;

        public MainWindowViewModel(MainWindow window)
        {
            Comm.RxAnalyzer.Dispatcher = window.Dispatcher;
            this.window = window;
            // 初期表示のGridは動的に入れ替えるので最初に参照を取得しておく
            BaseSerialTxOrig = window.BaseSerialTx.Children[0];
            BaseSerialRxOrig = window.BaseSerialRx.Children[0];
            BaseSerialAutoTxOrig = window.BaseSerialAutoTx.Children[0];
            BaseSerialTxMsg = new ReactivePropertySlim<string>();
            BaseSerialTxMsg.AddTo(Disposables);
            BaseSerialRxMsg = new ReactivePropertySlim<string>();
            BaseSerialRxMsg.AddTo(Disposables);
            BaseSerialAutoTxMsg = new ReactivePropertySlim<string>();
            BaseSerialAutoTxMsg.AddTo(Disposables);

            //
            WindowTitle = new ReactivePropertySlim<string>("SerialDebugger");
            // Serial
            serialSetting = new Serial.Settings();
            //serialHandler = new Serial.CommHandler();
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
            IsEnableSerialOpen = new ReactivePropertySlim<bool>(true);
            IsEnableSerialOpen.AddTo(Disposables);
            // 設定ボタン
            IsEnableSerialSetting = IsSerialOpen
                .Inverse()
                .CombineLatest(IsEnableSerialOpen, (x, y) => x && y)
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
                    IsEnableSerialOpen.Value = false;
                    await UpdateTxAsync();
                    IsEnableSerialOpen.Value = true;
                })
                .AddTo(Disposables);
            OnClickTxDataSend = new ReactiveCommand();
            OnClickTxDataSend
                .Subscribe(x => {
                    var frame = (Comm.TxFieldBuffer)x;
                    SerialTxBufferSendFix(frame);
                })
                .AddTo(Disposables);

            // Log
            Log = Logger.GetLogData();

            //
            TxFrames = new ReactiveCollection<Comm.TxFrame>();
            TxFrames.AddTo(Disposables);
            RxFrames = new ReactiveCollection<Comm.RxFrame>();
            RxFrames.AddTo(Disposables);
            AutoTxJobs = new ReactiveCollection<Comm.AutoTxJob>();
            AutoTxJobs.AddTo(Disposables);
            EmptyAutoTxJobs = AutoTxJobs;
            // 送信ショートカット
            TxShortcut = new ReactiveCollection<TxShortcutNode>();
            TxShortcut.AddTo(Disposables);
            TxShortcutButtonDisp = new ReactivePropertySlim<string>();
            TxShortcutButtonDisp.AddTo(Disposables);
            TxShortcutSelectedIndex = new ReactivePropertySlim<int>();
            TxShortcutSelectedIndex
                .Subscribe(x =>
                {
                    if (0 <= x && x < TxShortcut.Count)
                    {
                        switch (TxShortcut[x].Buffer.ChangeState.Value)
                        {
                            case Comm.Field.ChangeStates.Fixed:
                                TxShortcutButtonDisp.Value = "送信";
                                break;

                            default:
                                TxShortcutButtonDisp.Value = "確定";
                                break;
                        }
                    }
                    else
                    {
                        TxShortcutButtonDisp.Value = "-";
                    }
                })
                .AddTo(Disposables);
            OnClickTxShortcut = new ReactiveCommand();
            OnClickTxShortcut.Subscribe(x =>
            {
                if (0 <= TxShortcutSelectedIndex.Value && TxShortcutSelectedIndex.Value < TxShortcut.Count)
                {
                    var node = TxShortcut[TxShortcutSelectedIndex.Value];
                    SerialTxBufferSendFix(node.Buffer);
                }
            })
            .AddTo(Disposables);
            // 自動送信操作ショートカット
            AutoTxShortcutButtonDisp = new ReactivePropertySlim<string>();
            AutoTxShortcutButtonDisp.AddTo(Disposables);
            AutoTxShortcutSelectedIndex = new ReactivePropertySlim<int>();
            AutoTxShortcutSelectedIndex.Subscribe(x =>
                {
                    if (0 <= x && x < AutoTxJobs.Count)
                    {
                        if (AutoTxJobs[x].IsActive.Value)
                        {
                            AutoTxShortcutButtonDisp.Value = "停止";
                        }
                        else
                        {
                            AutoTxShortcutButtonDisp.Value = "Run";
                        }
                    }
                    else
                    {
                        AutoTxShortcutButtonDisp.Value = "-";
                    }
                })
                .AddTo(Disposables);
            OnClickAutoTxShortcut = new ReactiveCommand();
            OnClickAutoTxShortcut.Subscribe(x =>
                {
                    if (AutoTxShortcutSelectedIndex.Value < AutoTxJobs.Count)
                    {
                        var job = AutoTxJobs[AutoTxShortcutSelectedIndex.Value];
                        job.IsActive.Value = !job.IsActive.Value;
                        AutoTxShortcutSelectedIndex.ForceNotify();
                    }
                })
                .AddTo(Disposables);

            // Script
            OnClickOpenScript = new ReactiveCommand();
            OnClickOpenScript.Subscribe((x)=>
            {
                Script.Interpreter.Engine.ShowView(window);
            })
            .AddTo(Disposables);

            // Debug
            DebugInit();
        }

        public async Task InitAsync()
        {
            // 
            InitGui();
            // 設定ファイル読み込み
            await Setting.InitAsync(Settings);

            // 有効な設定ファイルを読み込んでいたら
            if (Settings.Count > 0)
            {
                // 最初に取得したファイルを読み込む
                SettingsSelectIndex.Value = 0;
                var result = await LoadTxAsync();
                if (!result)
                {
                    SetMsgNoSettings();
                }
            }
            else
            {
                BaseSerialTxMsg.Value = "有効な設定ファイルが存在しません。";
                BaseSerialRxMsg.Value = "有効な設定ファイルが存在しません。";
                BaseSerialAutoTxMsg.Value = "有効な設定ファイルが存在しません。";
                Logger.Add("有効な設定ファイルが存在しません。");
            }
        }

        public async Task UpdateTxAsync()
        {
            try
            {
                // 設定ファイルを切り替えたときにログクリア
                Logger.Clear();
                // 現在表示中のGUIを破棄
                InitGui();

                // GUI再構築するため明示的にGC起動しておく
                await Task.Run(() => { GC.Collect(); });
                //Logger.Add($"GC: {GC.GetTotalMemory(false)}");
                var result = await LoadTxAsync();
                if (!result)
                {
                    SetMsgNoSettings();
                }
            }
            catch (Exception ex)
            {
                WindowTitle.Value = $"{ToolTitle}";
                SetMsgNoSettings();
                Logger.AddException(ex, "設定ファイル読み込みエラー:");
            }
        }

        /// <summary>
        /// GUI表示初期設定
        /// </summary>
        public void InitGui()
        {
            WindowTitle.Value = $"{ToolTitle}";
            window.BaseSerialTx.Children.Clear();
            window.BaseSerialRx.Children.Clear();
            window.BaseSerialAutoTx.Children.Clear();
            BaseSerialTxMsg.Value = "設定ファイル読み込み中...";
            BaseSerialRxMsg.Value = "設定ファイル読み込み中...";
            BaseSerialAutoTxMsg.Value = "設定ファイル読み込み中...";
            window.BaseSerialTx.Children.Add(BaseSerialTxOrig);
            window.BaseSerialRx.Children.Add(BaseSerialRxOrig);
            window.BaseSerialAutoTx.Children.Add(BaseSerialAutoTxOrig);
            TxShortcut.Clear();
        }

        public void SetMsgNoSettings()
        {
            BaseSerialTxMsg.Value = "有効な設定が存在しません。";
            BaseSerialRxMsg.Value = "有効な設定が存在しません。";
            BaseSerialAutoTxMsg.Value = "有効な設定が存在しません。";
        }

        public async Task<bool> LoadTxAsync()
        {
            var data = Settings[SettingsSelectIndex.Value];
            // ログ設定更新
            Logger.UpdateSetting(data.Log);
            // 未ロードファイルならロード処理
            if (!data.IsLoaded)
            {
                var result = await Setting.LoadAsync(data);
                if (!result)
                {
                    return false;
                }
            }
            // Script
            Script.Interpreter.Engine.Comm.Init(data.Comm.Tx, data.Comm.Rx, data.Comm.AutoTx);
            Comm.Gui.Init(data);
            // GUI作成
            WindowTitle.Value = $"{ToolTitle} [{data.Name}]";
            window.Width = data.Gui.Window.Width;
            window.Height = data.Gui.Window.Height;
            // COMポート設定更新
            serialSetting.vm.SetSerialSetting(data.Serial);
            // Tx設定
            if (data.Comm.Tx.Count > 0)
            {
                TxFrames = data.Comm.Tx;
                // 通信データ初期化
                InitComm();
                // GUI作成
                var tx = Comm.TxGui.Make();
                // GUI反映
                window.BaseSerialTx.Children.Clear();
                window.BaseSerialTx.Children.Add(tx);
                // ショートカット作成
                foreach (var frame in TxFrames)
                {
                    foreach (var fb in frame.Buffers)
                    {
                        TxShortcut.Add(new TxShortcutNode(fb.Name, fb));
                        fb.ChangeState.Subscribe(x =>
                        {
                            TxShortcutSelectedIndex.ForceNotify();
                        });
                    }
                }
                if (TxShortcut.Count > 0)
                {
                    TxShortcutSelectedIndex.Value = 0;
                }
            }
            else
            {
                BaseSerialTxMsg.Value = "有効な送信設定が存在しません。";
            }
            // Rx設定
            if (data.Comm.Rx.Count > 0)
            {
                RxFrames = data.Comm.Rx;
                // GUI作成
                var rx = Comm.RxGui.Make();
                // GUI反映
                window.BaseSerialRx.Children.Clear();
                window.BaseSerialRx.Children.Add(rx);
            }
            else
            {
                BaseSerialRxMsg.Value = "有効な受信設定が存在しません。";
            }
            // AutoTx設定
            if (data.Comm.AutoTx.Count > 0)
            {
                AutoTxJobs = data.Comm.AutoTx;
                // GUI作成
                var autotx = Comm.AutoTxGui.Make(data);
                // GUI反映
                window.BaseSerialAutoTx.Children.Clear();
                window.BaseSerialAutoTx.Children.Add(autotx);
            }
            else
            {
                AutoTxJobs = EmptyAutoTxJobs;
                BaseSerialAutoTxMsg.Value = "有効な自動送信設定が存在しません。";
            }
            BindAutoTxShortcut();

            return true;
        }

        private void BindAutoTxShortcut()
        {
            // Binding先インスタンスを切り替えたため、再接続しないとGUIに反映されない
            // 新インスタンスにBinding設定
            var bind = new Binding("AutoTxJobs");
            window.AutoTxShortcut.SetBinding(ComboBox.ItemsSourceProperty, bind);
            AutoTxShortcutSelectedIndex.Value = 0;
            AutoTxShortcutSelectedIndex.ForceNotify();
            // AutoTxイベント購読
            AutoTxJobs.ObserveElementObservableProperty(x => x.IsActive).Subscribe(x =>
            {
                // 
                AutoTxShortcutSelectedIndex.ForceNotify();
            });
        }

        private void InitComm()
        {
            // 送信データをすべて確定する
            // TxFrame
            foreach (var frame in TxFrames)
            {
                foreach (var fb in frame.Buffers)
                {
                    fb.BufferToData();
                }
            }
        }

        private async Task SerialMain()
        {
            if (!IsSerialOpen.Value)
            {
                await SerialStart();
            }
            else
            {
                SerialFinish();
            }
        }

        private async Task SerialStart()
        {
            try
            {
                // シリアルポートを開く
                serialPort = serialSetting.vm.GetSerialPort();
                serialPort.Open();
                // 通信設定
                var polling = serialSetting.vm.PollingCycle.Value;
                var rx_timeout = serialSetting.vm.RxTimeout.Value;
                // 通信管理クラス作成
                protocol = new Serial.Protocol(serialPort, polling, rx_timeout, TxFrames, RxFrames, AutoTxJobs);
                // Script
                Script.Interpreter.Engine.Comm.Init(protocol);
                // GUI更新
                // COM切断を有効化
                IsSerialOpen.Value = true;
                TextSerialOpen.Value = "COM切断";
                // シリアル通信開始
                await protocol.Run();
            }
            catch (OperationCanceledException e)
            {
                Logger.Add($"Comm Cancel: {e.Message}");
            }
            catch (Exception e)
            {
                Logger.Add($"Error: {e.Message}");
            }
            finally
            {
                // COM終了でプロトコル破棄
                //protocol.Dispose();
                protocol = null;
                // COMポート終了
                serialPort.Close();
                serialPort = null;
                // GUI処理
                IsSerialOpen.Value = false;
                IsEnableSerialOpen.Value = true;
                TextSerialOpen.Value = "COM接続";
            }
        }

        private void SerialFinish()
        {
            try
            {
                // スレッド終了メッセージ送信
                protocol.Stop();
                // 
                IsEnableSerialOpen.Value = false;
                TextSerialOpen.Value = "切断中";
            }
            catch (Exception e)
            {
                Logger.Add($"Error: {e.Message}");
            }
        }




        private void SerialTxBufferSendFix(Comm.TxFieldBuffer frame)
        {
            switch (frame.ChangeState.Value)
            {
                case Comm.Field.ChangeStates.Changed:
                    // 変更内容をシリアル通信データに反映
                    frame.BufferFix();
                    break;

                default:
                    // シリアル送信
                    SerialWrite(frame.Data, frame.Name);
                    break;
            }
        }

        private void SerialWrite(byte[] data, string name)
        {
            // 通信中のみ送信
            if (IsSerialOpen.Value)
            {
                try
                {
                    // 送信はGUIスレッドからのみ送信
                    serialPort.Write(data, 0, data.Length);
                    //
                    Logger.Add($"[Tx][{name}] {Logger.Byte2Str(data)}");
                    /*
                    await Task.Run(() => {
                        serialPort.Write(buff.ToArray(), 0, buff.Count);
                    });
                     */
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
                        //var buff = TxFrames[0].TxBuffer;
                        //serialPort.Write(buff.ToArray(), 0, buff.Count);
                    }
                    System.Threading.Thread.Sleep(1000);
                    {
                        //var buff = TxFrames[1].TxBuffer;
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


        // Debug
        public bool IsDebug { get; set; } = false;
        public Visibility DebugVisible { get; set; } = Visibility.Collapsed;
        public ReactiveCommand OnClickTestSend { get; set; }

        [System.Diagnostics.Conditional("DEBUG")]
        public void DebugInit()
        {
            IsDebug = true;
            DebugVisible = Visibility.Visible;
            // test
            OnClickTestSend = new ReactiveCommand();
            OnClickTestSend.Subscribe(async (x) =>
            {
                try
                {
                    //var z = await Script.Interpreter.Engine.wv.ExecuteScriptAsync("debug()");

                    var script = @"
(() => {
try {
    //throw new Error('error');
    //Comm.Debug();
    return CommDebug();
}
catch (e) {
    Comm.Error(e.message);
    return false;
}
return true;
})();
";
                    //script = @"import { test_js_test } from 'test.js';";
                    //var result = await Script.Interpreter.Engine.wv.CoreWebView2.ExecuteScriptAsync("CommDebug()");
                    //var result = await Script.Interpreter.Engine.wv.CoreWebView2.ExecuteScriptAsync(script);
                    int i;
                    i = 0;
                    i++;
                    await Task.Delay(1);
                }
                catch (Exception e)
                {
                    Logger.AddException(e);
                }
                /*
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
                */
            })
                .AddTo(Disposables);
        }



        #region IClosing Support
        bool IClosing.OnClosing()
        {
            if (IsSerialOpen.Value) return true;



            Script.Interpreter.Engine.Close();

            return false;
        }
        #endregion

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

                    Script.Interpreter.Engine.Close();
                }

                // TODO: アンマネージド リソース (アンマネージド オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージド リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        //~MainWindowViewModel()
        //{
        //    // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //    Dispose(false);
        //}

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
