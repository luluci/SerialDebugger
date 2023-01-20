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
using System.Windows.Threading;

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

        // シリアル通信管理変数
        SerialPort serialPort;
        //定期処理関連
        DispatcherTimer AutoTxTimer;

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
                    var frame = (Comm.TxFrame)x;
                    SerialTxBufferSendFix(frame);
                })
                .AddTo(Disposables);
            OnClickTxBufferSend = new ReactiveCommand();
            OnClickTxBufferSend
                .Subscribe(x => {
                    var frame = (Comm.TxBackupBuffer)x;
                    SerialTxBufferSendFix(frame);
                })
                .AddTo(Disposables);

            // Log
            Log = Logger.GetLogData();

            // 定期処理
            AutoTxTimer = new DispatcherTimer(DispatcherPriority.Normal)
            {
                Interval = new TimeSpan(0, 0, 0, 0, 500),
            };
            AutoTxTimer.Tick += new EventHandler(TickEvent);

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
                await LoadTxAsync();
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
            BaseSerialTxMsg.Value = "設定ファイル読み込み中。。";
            window.BaseSerialTx.Children.Add(BaseSerialTxOrig);

            try
            {
                // GUI再構築するため明示的にGC起動しておく
                await Task.Run(() => { GC.Collect(); });
                //Logger.Add($"GC: {GC.GetTotalMemory(false)}");
                await LoadTxAsync();
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Setting File Load Error: {ex.Message}");
                BaseSerialTxMsg.Value = "有効な送信設定が存在しません。";
                Logger.AddException(ex, "設定ファイル読み込みエラー:");
            }
        }

        public async Task LoadTxAsync()
        {
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
                //TxFrames.ObserveElementObservableProperty(x => x.UpdateMsg).Subscribe(x =>
                //{
                //    int hoge;
                //    hoge = 0;
                //});
                var grid = Comm.TxGui.Make(data);
                // GUI反映
                window.BaseSerialTx.Children.Clear();
                window.BaseSerialTx.Children.Add(grid);
                // 通信データ初期化
                InitComm();
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

        private async void TickEvent(object sender, EventArgs e)
        {
            // Queueをすべて処理
            await ProcQueue();
        }

        private async void TickEventFinish()
        {
            // tick停止
            AutoTxTimer.Stop();
            // Queueをすべて処理
            await ProcQueue();
        }

        private async Task ProcQueue()
        {
            try
            {
                while (!serialHandler.qComm2Gui.IsEmpty)
                {
                    // Msg処理
                    if (serialHandler.qComm2Gui.TryDequeue(out Serial.CommMsg msg))
                    {
                        ProcMsg(msg);
                    }
                    // GUI解放
                    await Task.Delay(1);
                }
            }
            catch
            {

            }
        }

        private void ProcMsg(Serial.CommMsg msg)
        {
            msg.Invoke();
        }

        private void InitComm()
        {
            // 送信データをすべて確定する
            // TxFrame
            foreach (var frame in TxFrames)
            {
                foreach (var field in frame.Fields)
                {
                    field.ChangeState.Value = Comm.TxField.ChangeStates.Fixed;
                }
                // BackupBuffer
                foreach (var bk_buff in frame.BackupBuffer)
                {
                    foreach (var field in bk_buff.Fields)
                    {
                        field.ChangeState.Value = Comm.TxField.ChangeStates.Fixed;
                    }
                    //
                    bk_buff.ChangeState.Value = Comm.TxField.ChangeStates.Fixed;
                }
                //
                frame.ChangeState.Value = Comm.TxField.ChangeStates.Fixed;
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
                // COM切断を有効化
                IsSerialOpen.Value = true;
                TextSerialOpen.Value = "COM切断";
                // 自動送信定期処理タイマ開始
                // 自動送信はGUIスレッド上で管理する。
                AutoTxTimer.Start();

                // シリアル通信管理ハンドラ初期化？
                // serialHandler.Init(TxFrames);
                // 受信解析定義転送？
                // ツール上で変更しないなら他の場所でいい

                // 受信解析定義があるときに受信解析ループを実行
                if (false)
                {
                    while (IsSerialOpen.Value)
                    {
                        // 受信解析, 一連の受信シーケンスが完了するまでawait
                        // 受信フレーム受理orタイムアウトによるノイズ受信確定が返ってくる
                        await serialHandler.Run(serialPort, serialSetting.vm.PollingCycle.Value);
                        // 処理結果を自動送信処理に通知
                        // ...
                    }
                }
                
            }
            catch (Exception e)
            {
                IsSerialOpen.Value = false;
                TextSerialOpen.Value = "COM接続";
                Logger.Add($"Error: {e.Message}");
            }
        }

        private void SerialFinish()
        {
            try
            {
                // スレッド終了メッセージ送信
                serialHandler.qGui2Comm.Enqueue(new Serial.GuiMsgQuit());
                // シリアル通信スレッドメッセージポーリング処理終了
                TickEventFinish();
                //serialHandler = null;
            }
            catch (Exception e)
            {
                Logger.Add($"Error: {e.Message}");
            }
            finally
            {
                IsSerialOpen.Value = false;
                TextSerialOpen.Value = "COM接続";
            }
        }

        private void SerialTxBufferSendFix(Comm.TxFrame frame)
        {
            switch (frame.ChangeState.Value)
            {
                case Comm.TxField.ChangeStates.Changed:
                    // 変更内容をシリアル通信データに反映
                    SerialTxBufferFix(frame);
                    break;

                default:
                    // シリアル送信
                    SerialWrite(frame.TxData);
                    break;
            }
        }
        private void SerialTxBufferSendFix(Comm.TxBackupBuffer frame)
        {
            switch (frame.ChangeState.Value)
            {
                case Comm.TxField.ChangeStates.Changed:
                    // 変更内容をシリアル通信データに反映
                    SerialTxBufferFix(frame);
                    break;

                default:
                    // シリアル送信
                    SerialWrite(frame.TxData);
                    break;
            }
        }

        private void SerialTxBufferFix(Comm.TxFrame frame)
        {
            // バッファを送信データにコピー
            frame.TxBuffer.CopyTo(frame.TxData, 0);
            // 変更フラグを下す
            foreach (var field in frame.Fields)
            {
                field.ChangeState.Value = Comm.TxField.ChangeStates.Fixed;
            }
            //
            frame.ChangeState.Value = Comm.TxField.ChangeStates.Fixed;
        }
        private void SerialTxBufferFix(Comm.TxBackupBuffer frame)
        {
            // バッファを送信データにコピー
            frame.TxBuffer.CopyTo(frame.TxData, 0);
            // 変更フラグを下す
            foreach (var field in frame.Fields)
            {
                field.ChangeState.Value = Comm.TxField.ChangeStates.Fixed;
            }
            //
            frame.ChangeState.Value = Comm.TxField.ChangeStates.Fixed;
        }



        private void SerialWrite(byte[] data)
        {
            // 通信中のみ送信
            if (IsSerialOpen.Value)
            {
                try
                {
                    // 送信はGUIスレッドからのみ送信
                    serialPort.Write(data, 0, data.Length);
                    //
                    Logger.Add($"Send: {data.ToString()}");
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
