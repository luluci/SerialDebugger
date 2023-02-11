using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Comm
{
    using Utility;

    class TxFieldValue : BindableBase, IDisposable
    {
        //
        public Field FieldRef { get; }
        //
        public ReactivePropertySlim<Int64> Value { get; set; }
        public ReactivePropertySlim<int> SelectIndex { get; set; }
        public ReactivePropertySlim<Field.ChangeStates> ChangeState { get; set; }

        public TxFieldValue(Field field)
        {
            // 対応するFieldへの参照
            FieldRef = field;
            // 値変更を送信バッファに反映したかどうか管理する
            ChangeState = new ReactivePropertySlim<Field.ChangeStates>(Field.ChangeStates.Fixed);
            ChangeState.AddTo(Disposables);

            Value = new ReactivePropertySlim<Int64>(FieldRef.Value.Value, mode: ReactivePropertyMode.DistinctUntilChanged);
            Value.Subscribe(x =>
                {
                    // ComboBox入力更新
                    var index = FieldRef.GetSelectsIndex(x);
                    if (index != -1)
                    {
                        SelectIndex.Value = index;
                    }
                    // 変更状態更新
                    ChangeState.Value = Field.ChangeStates.Changed;
                })
                .AddTo(Disposables);
            SelectIndex = new ReactivePropertySlim<int>(FieldRef.InitSelectIndex, mode: ReactivePropertyMode.DistinctUntilChanged);
            SelectIndex.Subscribe((int index) =>
                {
                    if (index >= 0)
                    {
                        var select = FieldRef.Selects[index];
                        // Value側でもSelectIndexSelectsとの同期をとって値を変更する
                        // 値に変化がないとSubscribeは発火しない。
                        Value.Value = select.Value;
                    }
                })
                .AddTo(Disposables);
        }

        /// <summary>
        /// 任意値設定
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(Int64 value)
        {
            // Value
            value = FieldRef.LimitValue(value);
            Value.Value = value;
            // SelectIndexSelects
            SelectIndex.Value = FieldRef.GetSelectsIndex(value);
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
