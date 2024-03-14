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

    public class MainWindowViewModel : BindableBase, IDisposable, IClosing
    {
        // Title
        static string ToolTitle = "SerialDebugger";
        public ReactivePropertySlim<string> WindowTitle { get; set; }
        // Tab
        public ReactivePropertySlim<int> TabSelectedIndex { get; set; }
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
        Popup popupSerialSetting;
        // Comm: Tx
        public ReactiveCollection<Comm.TxFrame> TxFrames { get; set; }
        public ReactiveCommand OnClickTxDataSend { get; set; }
        public ReactiveCommand OnClickTxDataCopy { get; set; }
        
        // Tx Shortcut定義
        public class TxShortcutNode
        {
            public string Name { get; }
            public Comm.TxFrame Frame { get; }
            public Comm.TxFieldBuffer Buffer { get; }

            public TxShortcutNode(string name, Comm.TxFrame frame, Comm.TxFieldBuffer buffer)
            {
                Name = name;
                Frame = frame;
                Buffer = buffer;
            }
        }
        public ReactiveCollection<TxShortcutNode> TxShortcut { get; set; }
        public ReactivePropertySlim<int> TxShortcutSelectedIndex { get; set; }
        public ReactivePropertySlim<string> TxShortcutButtonDisp { get; set; }
        public ReactiveCommand OnClickTxShortcut { get; set; }
        public ReactiveCommand OnClickTxScroll { get; set; }
        // Comm: Rx
        public ReactiveCollection<Comm.RxFrame> RxFrames { get; set; }
        // Rx Shortcut定義
        public class RxShortcutNode
        {
            public string Name { get; }
            public Comm.RxFrame Frame { get; }

            public RxShortcutNode(string name, Comm.RxFrame frame)
            {
                Name = name;
                Frame = frame;
            }
        }
        public ReactiveCollection<RxShortcutNode> RxShortcut { get; set; }
        public ReactivePropertySlim<int> RxShortcutSelectedIndex { get; set; }
        public ReactiveCommand OnClickRxScroll { get; set; }
        // Comm: AutoTx
        public ReactiveCollection<Comm.AutoTxJob> AutoTxJobs { get; set; }
        public ReactiveCollection<Comm.AutoTxJob> EmptyAutoTxJobs { get; set; }
        public ReactivePropertySlim<int> AutoTxShortcutSelectedIndex { get; set; }
        public ReactivePropertySlim<string> AutoTxShortcutButtonDisp { get; set; }
        public ReactiveCommand OnClickAutoTxShortcut { get; set; }
        public ReactiveCommand OnClickAutoTxScroll { get; set; }
        // Comm: common
        // string入力I/F
        Comm.InputString inputString { get; set; }
        public ReactiveCommand OnClickInputString { get; set; }
        public ReactivePropertySlim<bool> IsEnableInputString { get; set; }

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
        Task protocolTask;

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
            // Tab
            TabSelectedIndex = new ReactivePropertySlim<int>(0);
            TabSelectedIndex.AddTo(Disposables);
            // Serial
            serialSetting = new Serial.Settings();
            //serialHandler = new Serial.CommHandler();
            // Serial Open
            IsSerialOpen = new ReactivePropertySlim<bool>(false);
            IsSerialOpen.AddTo(Disposables);
            TextSerialOpen = new ReactivePropertySlim<string>("COM接続");
            TextSerialOpen.AddTo(Disposables);
            OnClickSerialOpen = new ReactiveCommand();
            OnClickSerialOpen.Subscribe((x) => 
                {
                    SerialMain();
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
                    popupSerialSetting.PlacementTarget = window.BtnSettings;
                    popupSerialSetting.IsOpen = !popupSerialSetting.IsOpen;
                })
                .AddTo(Disposables);
            popupSerialSetting = new Popup();
            popupSerialSetting.StaysOpen = false;
            popupSerialSetting.Child = serialSetting;
            // Settingファイル選択GUI
            Settings = new ReactiveCollection<Settings.SettingInfo>();
            Settings.AddTo(Disposables);
            SettingsSelectIndex = new ReactivePropertySlim<int>(mode:ReactivePropertyMode.DistinctUntilChanged);
            SettingsSelectIndex
                .Subscribe(async (int idx) =>
                {
                    IsEnableSerialOpen.Value = false;
                    await UpdateSettingAsync(false);
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
            OnClickTxDataCopy = new ReactiveCommand();
            OnClickTxDataCopy
                .Subscribe(x => {
                    var frame = (Comm.TxFieldBuffer)x;
                    int idx = 0;
                    var sb = new StringBuilder();
                    sb.Append("\"value\": [");
                    sb.Append($" {frame.FieldValues[idx].Value.Value}");
                    idx++;
                    for (; idx < frame.FieldValues.Count; idx++)
                    {
                        sb.Append($", {frame.FieldValues[idx].Value.Value}");
                    }
                    sb.Append(" ]");
                    // クリップボードにコピー
                    Clipboard.SetText(sb.ToString());
                })
                .AddTo(Disposables);

            // Log
            Log = Logger.GetLogData();

            // 文字列入力ポップアップウインドウ
            IsEnableInputString = new ReactivePropertySlim<bool>(true);
            IsEnableInputString.AddTo(Disposables);
            OnClickInputString = new ReactiveCommand();
            OnClickInputString
                .Subscribe(x =>
                {
                    try
                    {
                        IsEnableInputString.Value = false;

                        // 文字列入力ポップアップの入力準備
                        //var tuple = (Tuple<Object, Object>)x;
                        var tuple = ((Object, Object, Object))x;
                        var frame = (Comm.TxFrame)tuple.Item1;
                        var field = (Comm.Field)tuple.Item2;
                        var buffer = (Comm.TxFieldBuffer)tuple.Item3;
                        inputString.vm.TxFrameRef = frame;
                        inputString.vm.FieldRef = field;
                        inputString.vm.TxFieldBufferRef = buffer;
                        inputString.vm.Caption.Value = $"{field.selecter.StrLen}バイトまで受付可";
                        inputString.vm.InputString.Value = frame.MakeCharField2String(field.Id, buffer.Id);
                        var pt = GetInputStringPos(buffer.FieldValues[field.Id].UI);
                        inputString.Top = pt.Y;
                        inputString.Left = pt.X;
                        inputString.Visibility = Visibility.Visible;
                        inputString.Show();
                        inputString.SetFocus();
                    }
                    catch (Exception ex)
                    {
                        inputString.Hide();
                        Logger.Add("Logic error?: " + ex.Message);
                    }
                })
                .AddTo(Disposables);
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
            OnClickTxScroll = new ReactiveCommand();
            OnClickTxScroll.Subscribe(x =>
                {
                    if (0 <= TxShortcutSelectedIndex.Value && TxShortcutSelectedIndex.Value < TxShortcut.Count)
                    {
                        var node = TxShortcut[TxShortcutSelectedIndex.Value];
                        TabSelectedIndex.Value = 0;
                        window.TxScrollViewer.ScrollToHorizontalOffset(node.Frame.Point.X);
                    }
                })
                .AddTo(Disposables);
            // Rx Shortcut
            RxShortcut = new ReactiveCollection<RxShortcutNode>();
            RxShortcut.AddTo(Disposables);
            RxShortcutSelectedIndex = new ReactivePropertySlim<int>();
            RxShortcutSelectedIndex.AddTo(Disposables);
            OnClickRxScroll = new ReactiveCommand();
            OnClickRxScroll.Subscribe(x =>
                {
                    if (0 <= RxShortcutSelectedIndex.Value && RxShortcutSelectedIndex.Value < RxShortcut.Count)
                    {
                        var node = RxShortcut[RxShortcutSelectedIndex.Value];
                        TabSelectedIndex.Value = 1;
                        window.RxScrollViewer.ScrollToHorizontalOffset(node.Frame.Point.X);
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
            OnClickAutoTxScroll = new ReactiveCommand();
            OnClickAutoTxScroll.Subscribe(x =>
                {
                    if (AutoTxShortcutSelectedIndex.Value < AutoTxJobs.Count)
                    {
                        // AutoTxは高さが可変なので都度計算が必要
                        var job = AutoTxJobs[AutoTxShortcutSelectedIndex.Value];
                        TabSelectedIndex.Value = 2;
                        //
                        var temp_point = new Point(0, 0);
                        var point = job.UiElemRef.TranslatePoint(temp_point, window.AutoTxScrollViewer);
                        window.AutoTxScrollViewer.ScrollToVerticalOffset(point.Y);
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
            inputString = new Comm.InputString
            {
                Visibility = Visibility.Hidden,
                WindowState = WindowState.Normal,
                //Owner = window,
            };
            inputString.vm.OnLostFocus.Subscribe(x =>
            {
                try
                {
                    if (!(inputString.vm.TxFrameRef is null) && !(inputString.vm.FieldRef is null) && !(inputString.vm.TxFieldBufferRef is null))
                    {
                        // 入力内容をTxFieldBufferに反映
                        inputString.vm.TxFrameRef.SetString2CharField(inputString.vm.InputString.Value, inputString.vm.FieldRef.Id, inputString.vm.TxFieldBufferRef.Id);
                    }
                }
                finally
                {
                    inputString.vm.TxFrameRef = null;
                    inputString.vm.FieldRef = null;
                    inputString.vm.TxFieldBufferRef = null;
                    inputString.Hide();
                    IsEnableInputString.Value = true;
                }
            });

            // グローバルインスタンスはMainWindowのViewModelで管理する
            // Scriptの初期化が終わってから登録するのでここでDisposable登録
            Script.Interpreter.GetDisposable().AddTo(Disposables);
            Logger.GetDisposable().AddTo(Disposables);

            // 
            InitGui();
            // 設定ファイル読み込み
            await Setting.InitAsync(Settings);
            // protocolは未作成なので初期化不要
            // await StopProtocol();

            // 有効な設定ファイルを読み込んでいたら
            if (Settings.Count > 0)
            {
                // 最初に取得したファイルを読み込む
                SettingsSelectIndex.Value = 0;
                var result = await LoadSettingAsync(false);
                if (result)
                {
                    // AutoTxを常時実行するためにProtocolタスクを起動する
                    // 設定読み込み成功していたらprotocolを作成
                    result = MakeProtocol();
                    // Protocolタスク開始
                    protocolTask = protocol.Run();
                }
                else
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

        private Point GetInputStringPos(UIElement ui)
        {
            var wnd_pt = GetUIPos(window);
            var wnd_w = window.RenderSize.Width;
            var wnd_h = window.RenderSize.Height;
            double titleBarHeight = SystemParameters.CaptionHeight;
            var wnd_right = wnd_pt.X + wnd_w;
            var wnd_bottom = wnd_pt.Y + wnd_h - titleBarHeight * 1.5;
            var ui_pt = GetUIPos(ui);
            var ui_w = ui.RenderSize.Width;
            var ui_h = ui.RenderSize.Height;
            var ui_right = ui_pt.X + ui_w;
            var ui_bottom = ui_pt.Y + ui_h;
            var popup_w = inputString.RenderSize.Width;
            var popup_h = inputString.RenderSize.Height;
            //var screen = GetWholeScreenRect();

            if (ui_bottom + popup_h <= wnd_bottom)
            {
                if (ui_pt.X + popup_w <= wnd_right)
                {
                    ui_pt.Y = ui_bottom;
                    return ui_pt;
                }
                else if (ui_right - popup_w >= wnd_pt.X)
                {
                    ui_pt.X = ui_right - popup_w;
                    ui_pt.Y = ui_bottom;
                    return ui_pt;
                }
                else
                {
                    // いずれにせよはみ出る場合はボタンの左下を起点に表示
                }
            }
            else if (ui_pt.Y - popup_h >= wnd_pt.Y)
            {
                if (ui_pt.X + popup_w <= wnd_right)
                {
                    ui_pt.Y = ui_pt.Y - popup_h;
                    return ui_pt;
                }
                else if (ui_right - popup_w >= wnd_pt.X)
                {
                    ui_pt.X = ui_right - popup_w;
                    ui_pt.Y = ui_pt.Y - popup_h;
                    return ui_pt;
                }
                else
                {
                    // いずれにせよはみ出る場合はボタンの左下を起点に表示
                }
            }
            else
            {
                // いずれにせよはみ出る場合はボタンの左下を起点に表示
            }
            // 
            ui_pt.Y = ui_bottom;
            return ui_pt;
        }
        public Point GetUIPos(UIElement ui)
        {
            var pt = ui.PointToScreen(new Point(0.0d, 0.0d));
            var transform = PresentationSource.FromVisual(window).CompositionTarget.TransformFromDevice;
            return transform.Transform(pt);
        }
        public System.Drawing.Rectangle GetWholeScreenRect()
        {
            int left = 0;
            int right = 0;
            int top = 0;
            int bottom = 0;
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (left > screen.Bounds.Left)
                {
                    left = screen.Bounds.Left;
                }
                if (right < screen.Bounds.Right)
                {
                    right = screen.Bounds.Right;
                }
                if (top > screen.Bounds.Top)
                {
                    top = screen.Bounds.Top;
                }
                if (bottom < screen.Bounds.Bottom)
                {
                    bottom = screen.Bounds.Bottom;
                }
            }
            return new System.Drawing.Rectangle(left, top, right - left, bottom - top);
        }

        public async Task ReloadSettingAsync()
        {
            await UpdateSettingAsync(true);
        }

        public async Task UpdateSettingAsync(bool force_load)
        {
            try
            {
                // 設定ファイルを切り替えたときにログクリア
                Logger.Clear();
                // 現在表示中のGUIを破棄
                InitGui();
                // protocol起動中なら終了する
                await StopProtocol();

                // GUI再構築するため明示的にGC起動しておく
                await Task.Run(() => { GC.Collect(); });
                //Logger.Add($"GC: {GC.GetTotalMemory(false)}");

                // 選択して設定ファイルをロードする
                var result = await LoadSettingAsync(force_load);
                if (result)
                {
                    // AutoTxを常時実行するためにProtocolタスクを起動する
                    // 設定読み込み成功していたらprotocolを作成
                    result = MakeProtocol();
                    // Protocolタスク開始
                    protocolTask = protocol.Run();
                }
                else
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
            RxShortcut.Clear();
        }

        public void SetMsgNoSettings()
        {
            BaseSerialTxMsg.Value = "有効な設定が存在しません。";
            BaseSerialRxMsg.Value = "有効な設定が存在しません。";
            BaseSerialAutoTxMsg.Value = "有効な設定が存在しません。";
        }

        public async Task<bool> LoadSettingAsync(bool force_load)
        {
            var data = Settings[SettingsSelectIndex.Value];
            // ログ設定更新
            Logger.UpdateSetting(data.Log);
            // 強制ロード有効なら読み込み済み設定をクリアする
            if (force_load && data.IsLoaded)
            {
                data.ClearForReload();
            }
            // 未ロードファイルならロード処理
            if (!data.IsLoaded)
            {
                var result = await Setting.LoadAsync(data);
                if (!result)
                {
                    return false;
                }
            }
            // Script更新
            Script.Interpreter.ChangeSettingFile(data);
            // GUI作成
            Comm.Gui.Init(data);
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
                // GUI座標取得のために画面更新, Tabを表示しないと座標が取得できない
                TabSelectedIndex.Value = 0;
                window.BaseSerialTx.UpdateLayout();
                // GUI反映後処理
                foreach (var frame in TxFrames)
                {
                    // スクロールバーに対する通信フレーム表示相対位置を取得
                    if (frame.Id < tx.Children.Count)
                    {
                        Grid node = (Grid)tx.Children[frame.Id];
                        var temp_point = new Point(0, 0);
                        frame.Point = node.TranslatePoint(temp_point, window.TxScrollViewer);
                        //var point = node.Children[0].TransformToAncestor(window.TxScrollViewer).Transform(frame.Point);
                    }
                    // ショートカット作成
                    foreach (var fb in frame.Buffers)
                    {
                        TxShortcut.Add(new TxShortcutNode(fb.Name, frame, fb));
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
                // GUI座標取得のために画面更新
                TabSelectedIndex.Value = 1;
                window.BaseSerialRx.UpdateLayout();
                //
                foreach (var frame in RxFrames)
                {
                    if (frame.Id < rx.Children.Count)
                    {
                        Grid node = (Grid)rx.Children[frame.Id];
                        var temp_point = new Point(0, 0);
                        frame.Point = node.TranslatePoint(temp_point, window.RxScrollViewer);
                        //var point = node.Children[0].TransformToAncestor(window.TxScrollViewer).Transform(frame.Point);
                    }
                    // Shortcut作成
                    RxShortcut.Add(new RxShortcutNode(frame.Name, frame));
                }
                if (RxShortcut.Count > 0)
                {
                    RxShortcutSelectedIndex.Value = 0;
                }
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
                // Shortcut作成
                foreach (var job in AutoTxJobs)
                {
                    // 該当するGUI部品への参照を記憶しておく
                    var idx = job.Id * 2;
                    if (idx < autotx.Children.Count)
                    {
                        var node = autotx.Children[idx];
                        job.UiElemRef = node;
                    }
                }
            }
            else
            {
                AutoTxJobs = EmptyAutoTxJobs;
                BaseSerialAutoTxMsg.Value = "有効な自動送信設定が存在しません。";
            }
            BindAutoTxShortcut();

            // デフォルトでTxを表示する
            TabSelectedIndex.Value = 0;

            // 設定ファイル読み込み完了後にOnLoadイベントハンドラを実行
            if (data.Script.HasOnLoad)
            {
                await Script.Interpreter.Engine.ExecuteScriptAsync(data.Script.OnLoad);
            }

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

        private bool MakeProtocol()
        {
            try
            {
                // 初期化
                // 通信設定
                var polling = serialSetting.vm.PollingCycle.Value;
                var rx_timeout = serialSetting.vm.RxTimeout.Value;
                // 通信管理クラス作成
                protocol = new Serial.Protocol(polling, rx_timeout, TxFrames, RxFrames, AutoTxJobs);
                // Script
                Script.Interpreter.Engine.Comm.Init(protocol, this);

                return true;
            }
            catch (Exception e)
            {
                Logger.Add($"Make protocl Error: {e.Message}");
                return false;
            }
        }
        private async Task StopProtocol()
        {
            if (!(protocol is null))
            {
                // task終了を通知
                protocol.Stop();
                //
                switch (protocolTask.Status)
                {
                    case TaskStatus.Running:
                        // Running中のみ停止を待つ
                        await protocolTask;
                        break;

                    default:
                        // 他の状態はすべて停止してるはず
                        // 念のためawaitしてしまって問題ない？
                        await protocolTask;
                        break;
                }
            }
        }

        private void SerialMain()
        {
            if (!IsSerialOpen.Value)
            {
                SerialStart();
            }
            else
            {
                SerialFinish();
            }
        }

        private void SerialStart()
        {
            try
            {
                // シリアルポートを開く
                serialPort = serialSetting.vm.GetSerialPort();
                serialPort.Open();
                // 通信設定
                var polling = serialSetting.vm.PollingCycle.Value;
                var rx_timeout = serialSetting.vm.RxTimeout.Value;

                // Protocolタスクシリアル通信開始
                protocol.OpenSerial(serialPort, polling, rx_timeout);

                // GUI更新
                // COM切断を有効化
                IsSerialOpen.Value = true;
                TextSerialOpen.Value = "COM切断";
            }
            catch (OperationCanceledException e)
            {
                SerialFinish();
                Logger.Add($"Comm Cancel: {e.Message}");
            }
            catch (Exception e)
            {
                SerialFinish();
                Logger.Add($"Error: {e.Message}");
            }
        }

        private void SerialFinish()
        {
            try
            {
                // 終了処理中はGUI操作ブロック
                IsEnableSerialOpen.Value = false;
                TextSerialOpen.Value = "切断中";

                // Protocolタスクシリアル通信終了
                protocol.CloseSerial();
                // COMポート終了
                if (!(serialPort is null))
                {
                    serialPort.Close();
                    serialPort = null;
                }

                // 終了処理完了でGUI有効化
                IsSerialOpen.Value = false;
                IsEnableSerialOpen.Value = true;
                TextSerialOpen.Value = "COM接続";
            }
            catch (Exception e)
            {
                Logger.Add($"Error: {e.Message}");
            }
        }

        public string[] ScriptIfGetComPortList()
        {
            return serialSetting.vm.ComPortStrList;
        }
        public void ScriptIfRefreshComPortList()
        {
            serialSetting.vm.InitComPort();
        }

        /// <summary>
        /// Script用インターフェース：シリアルポート接続
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ScriptIfOpenSerial(string name)
        {
            bool result;

            // シリアルポート接続有無チェック
            if (IsSerialOpen.Value)
            {
                return false;
            }

            // COMポート設定チェック
            result = serialSetting.vm.SetComPort(name);
            if (!result)
            {
                return false;
            }

            // シリアルポート接続
            SerialStart();
            if (!IsSerialOpen.Value)
            {
                return false;
            }

            return true;
        }

        public void ScriptIfCloseSerial()
        {
            if (IsSerialOpen.Value)
            {
                SerialFinish();
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
                    SerialWrite(frame);
                    break;
            }
        }

        private void SerialWrite(Comm.TxFieldBuffer frame)
        {
            // 通信中のみ送信
            if (IsSerialOpen.Value)
            {
                try
                {
                    // 送信はGUIスレッドからのみ送信
                    serialPort.Write(frame.Data, 0, frame.Data.Length);
                    //
                    if (frame.FrameRef.IsLogVisualize)
                    {
                        Logger.Add($"[Tx][{frame.Name}] {frame.FrameRef.MakeLogVisualize(frame.Id)}, ({Logger.Byte2Str(frame.Data)})");
                    }
                    else
                    {
                        Logger.Add($"[Tx][{frame.Name}] {Logger.Byte2Str(frame.Data)}");
                    }
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

                    //var node = TxFrames[TxFrames.Count - 1];
                    //window.TxScrollViewer.ScrollToHorizontalOffset(node.Point.X);

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
                    // 一応protocolに停止指令を出す
                    // 以降の
                    protocol.Stop();
                    inputString.Close();
                    this.Disposables.Dispose();
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
