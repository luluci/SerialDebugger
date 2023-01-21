﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Comm
{
    enum AutoTxActionType
    {
        Send,       // シリアル送信
        Wait,       // 時間待機
        Recv,       // 受信待機
        Jump,       // ジャンプ
        Script,     // スクリプト実行
    }

    class AutoTxAction : BindableBase, IDisposable
    {
        public int Id { get; }
        public AutoTxActionType Type { get; private set; }

        public ReactivePropertySlim<string> TxFrameName { get; private set; }
        public int TxFrameIndex { get; private set; }
        public ReactivePropertySlim<int> TxFrameBuffIndex { get; private set; }

        /// <summary>
        /// 待機時間(milli sec)
        /// </summary>
        public ReactivePropertySlim<int> WaitTime { get; private set; }

        public ReactivePropertySlim<string> RxFrameName { get; private set; }
        public int RxAnalyzeIndex { get; private set; }

        public ReactivePropertySlim<int> JumpTo { get; private set; }

        public ReactivePropertySlim<string> ScriptName { get; private set; }

        public ReactivePropertySlim<bool> IsActive { get; set; }


        public AutoTxAction(int id)
        {
            Id = id;

            IsActive = new ReactivePropertySlim<bool>(false);
            IsActive.AddTo(Disposables);
        }

        /// <summary>
        /// 時間待機ノード作成
        /// </summary>
        /// <param name="id"></param>
        /// <param name="wait"></param>
        /// <returns></returns>
        public static AutoTxAction MakeWaitAction(int id, int wait)
        {
            var action = new AutoTxAction(id)
            {
                Type = AutoTxActionType.Wait,
            };
            action.WaitTime = new ReactivePropertySlim<int>(wait);
            action.WaitTime.AddTo(action.Disposables);

            return action;
        }
        public static AutoTxAction MakeJumpAction(int id, int jumpto)
        {
            var action = new AutoTxAction(id)
            {
                Type = AutoTxActionType.Jump,
            };
            action.JumpTo = new ReactivePropertySlim<int>(jumpto);
            action.JumpTo.AddTo(action.Disposables);

            return action;
        }

        public static AutoTxAction MakeSendAction(int id, string tx_frame_name, int tx_frame_idx, int buff_idx)
        {
            var action = new AutoTxAction(id)
            {
                Type = AutoTxActionType.Send
            };
            action.TxFrameName = new ReactivePropertySlim<string>(tx_frame_name);
            action.TxFrameName.AddTo(action.Disposables);
            action.TxFrameIndex = tx_frame_idx;
            action.TxFrameBuffIndex = new ReactivePropertySlim<int>(buff_idx);
            action.TxFrameBuffIndex.AddTo(action.Disposables);

            return action;
        }

        public static AutoTxAction MakeRecvAction(int id, string rx_name, int rx_idx)
        {
            var action = new AutoTxAction(id)
            {
                Type = AutoTxActionType.Recv,
            };
            action.RxFrameName = new ReactivePropertySlim<string>(rx_name);
            action.RxFrameName.AddTo(action.Disposables);
            action.RxAnalyzeIndex = rx_idx;

            return action;
        }

        public static AutoTxAction MakeScriptAction(int id, string script_func)
        {
            var action = new AutoTxAction(id)
            {
                Type = AutoTxActionType.Script,
            };
            action.ScriptName = new ReactivePropertySlim<string>(script_func);
            action.ScriptName.AddTo(action.Disposables);

            return action;
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