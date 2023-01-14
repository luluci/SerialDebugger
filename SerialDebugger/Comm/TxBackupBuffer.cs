using System;
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
    class TxBackupBuffer : BindableBase, IDisposable
    {
        public class Field : BindableBase, IDisposable
        {
            public ReactivePropertySlim<UInt64> Value { get; set; }
            // DropDownList用
            public ReadOnlyReactiveCollection<TxField.Select> Selects { get; set; }
            public ReactivePropertySlim<int> SelectIndexSelects { get; set; }
            // 参照
            public TxField FieldRef { get; }

            public Field(TxField field, UInt64 value)
            {
                FieldRef = field;

                //
                Value = new ReactivePropertySlim<ulong>(value, ReactivePropertyMode.DistinctUntilChanged);
                Value.Subscribe(x =>
                    {
                        var index = FieldRef.GetSelectsIndex(x);
                        if (index != -1)
                        {
                            SelectIndexSelects.Value = index;
                        }
                    })
                    .AddTo(Disposables);

                Selects = null;
                SelectIndexSelects = new ReactivePropertySlim<int>(0, ReactivePropertyMode.DistinctUntilChanged);
                SelectIndexSelects.Subscribe((int index) =>
                {
                    var select = Selects[index];
                    // Value側でもSelectIndexSelectsとの同期をとって値を変更する
                    // 値に変化がないとSubscribeは発火しない。
                    Value.Value = select.Value;
                })
                    .AddTo(Disposables);
            }

            public Field(TxField field, ReactiveCollection<TxField.Select> selects, int selectIndex, UInt64 value)
                : this(field, value)
            {
                Selects = selects.ToReadOnlyReactiveCollection();
                SelectIndexSelects.Value = selectIndex;
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

        // 
        public int Id { get; }
        public string Name { get; }
        public TxFrame FrameRef { get; }
        /// <summary>
        /// 表示データ
        /// </summary>
        public ReactiveCollection<Field> Fields { get; set; }
        /// <summary>
        /// 送信データバイトシーケンス
        /// </summary>
        public List<byte> Buffer { get; set; }

        public ReactiveCommand OnClickSave { get; set; }
        public ReactiveCommand OnClickStore { get; set; }

        public TxBackupBuffer(int id, string name, TxFrame frame)
        {
            Id = id;
            Name = name;
            FrameRef = frame;

            Fields = new ReactiveCollection<Field>();
            Fields
                .ObserveElementObservableProperty(x => x.Value).Subscribe(x =>
                {
                    Update(x.Instance);
                });
            Fields
                .ObserveElementObservableProperty(x => x.SelectIndexSelects).Subscribe(x =>
                {
                    Update(x.Instance);
                });
            Fields.AddTo(Disposables);
            Buffer = new List<byte>(frame.Length);
            OnClickSave = new ReactiveCommand();
            OnClickSave.AddTo(Disposables);
            OnClickStore = new ReactiveCommand();
            OnClickStore.AddTo(Disposables);

            // Fields作成
            for (int i = 0; i < frame.Fields.Count; i++)
            {
                // field展開
                var field = frame.Fields[i];
                switch (field.InputType)
                {
                    case TxField.InputModeType.Dict:
                    case TxField.InputModeType.Unit:
                    case TxField.InputModeType.Time:
                    case TxField.InputModeType.Script:
                        Fields.Add(new Field(field, field.Selects, field.SelectIndexSelects.Value, field.Value.Value));
                        break;

                    default:
                        Fields.Add(new Field(field, field.Value.Value));
                        break;
                }
            }
            // Buffer作成
            foreach (var value in frame.TxBuffer)
            {
                Buffer.Add(value);
            }
        }

        /// <summary>
        /// Fieldsが更新されたとき、送信バイトシーケンスに反映する
        /// </summary>
        /// <param name="field"></param>
        private void Update(Field field)
        {
            // 更新されたfieldをTxBufferに適用
            FrameRef.UpdateBuffer(field.FieldRef, field.Value.Value, Buffer);
            // チェックサムを持つframeで、更新fieldがチェックサムfieldでないとき、
            // チェックサムを再計算
            if (FrameRef.HasChecksum && !field.FieldRef.IsChecksum)
            {
                Fields[FrameRef.ChecksumIndex].Value.Value = FrameRef.CalcChecksum(Buffer);
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
