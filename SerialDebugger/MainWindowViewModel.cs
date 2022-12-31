﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Windows.Controls.Primitives;

namespace SerialDebugger
{
    using Logger = SerialDebugger.Log.Log;

    class MainWindowViewModel : BindableBase, IDisposable
    {
        // MainWindow
        public ReactiveCommand OnClickSerialSetting { get; set; }
        Popup popup;
        // Comm
        public ReactiveCollection<Comm.Settings.CommInfo> CommSettings { get; set; }
        public ReactivePropertySlim<int> CommSettingsSelectIndex { get; set; }
        public ReactiveCollection<Comm.TxFrame> TxFrames { get; set; }
        // Log
        public ReactiveCollection<string> Log { get; set; }

        public MainWindowViewModel(MainWindow window)
        {
            // Serial
            // 設定ボタン
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
            popup.Child = new Serial.Settings();
            // Comm
            // Comm設定取得
            CommSettings = Comm.Settings.GetComm();
            CommSettings.AddTo(Disposables);
            CommSettingsSelectIndex = new ReactivePropertySlim<int>(mode:ReactivePropertyMode.DistinctUntilChanged);
            CommSettingsSelectIndex
                .Subscribe((int idx) =>
                {
                    window.BaseSerialTx.Children.Clear();
                    TxFrames = CommSettings[idx].Tx;
                    // GUI構築する
                    Comm.TxGui.Make(window.BaseSerialTx, TxFrames);
                })
                .AddTo(Disposables);
            // 有効な通信フォーマットがあればツールに取り込む
            if (CommSettings.Count > 0)
            {
                foreach (var comm in CommSettings)
                {
                    comm.Tx.AddTo(Disposables);
                }

                // 先頭要素を選択
                TxFrames = CommSettings[0].Tx;
                CommSettingsSelectIndex.Value = 0;
                // GUI構築する
                Comm.TxGui.Make(window.BaseSerialTx, TxFrames);
            }
            else
            {
                TxFrames = new ReactiveCollection<Comm.TxFrame>();
                TxFrames.AddTo(Disposables);
            }
            // Log
            Log = Logger.GetLogData();
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
