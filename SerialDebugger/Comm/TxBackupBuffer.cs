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

    class TxBackupBuffer : BindableBase, ITxFrame, IDisposable
    {
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
        public ReactiveCollection<byte> TxBuffer { get; set; }
        // 確定送信データ
        public byte[] TxData { get; set; }

        public ReactiveCommand OnClickSave { get; set; }
        public ReactiveCommand OnClickStore { get; set; }

        /// <summary>
        /// TxBackupBuffer全体の変更状況
        /// </summary>
        public ReactivePropertySlim<Field.ChangeStates> ChangeState { get; set; }
        //
        //public ReactivePropertySlim<Serial.UpdateTxBuffMsg> UpdateMsg { get; set; }

        public TxBackupBuffer(int id, string name, TxFrame frame)
        {
            Id = id;
            Name = name;
            FrameRef = frame;

            //UpdateMsg = new ReactivePropertySlim<Serial.UpdateTxBuffMsg>();
            //UpdateMsg.AddTo(Disposables);
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
            Fields
                .ObserveElementObservableProperty(x => x.ChangeState).Subscribe(x =>
                {
                    ChangeState.Value = Field.ChangeStates.Changed;
                });
            Fields.AddTo(Disposables);
            TxBuffer = new ReactiveCollection<byte>();
            //
            OnClickSave = new ReactiveCommand();
            OnClickSave.AddTo(Disposables);
            OnClickStore = new ReactiveCommand();
            OnClickStore.AddTo(Disposables);
            //
            ChangeState = new ReactivePropertySlim<Field.ChangeStates>();
            ChangeState.AddTo(Disposables);

            // Fields作成
            // TxFrame.Fieldsをコピー、参照する形で構築する
            for (int i = 0; i < frame.Fields.Count; i++)
            {
                // field展開
                // FieldからID,Name,Value,InputTypeを継承する
                var field = frame.Fields[i];
                var bk_field = new Field(
                    field.Id,
                    field.Name,
                    field.InnerFields.ToArray(),
                    field.Value.Value,
                    field.InputBase,
                    field.InputType,
                    new Field.Selecter(field)
                );
                // ComboBoxで入力するInputTypeでは
                // Selectsの参照を取得しておき表示に流用する
                switch (field.InputType)
                {
                    case Field.InputModeType.Dict:
                    case Field.InputModeType.Unit:
                    case Field.InputModeType.Time:
                    case Field.InputModeType.Script:
                        bk_field.MakeSelectModeRefer();
                        break;

                    default:
                        break;
                }
                Fields.Add(bk_field);
            }
            // Buffer作成
            foreach (var value in frame.TxBuffer)
            {
                TxBuffer.Add(value);
            }
            // 送信データ作成
            TxData = TxBuffer.ToArray();
        }

        /// <summary>
        /// Fieldsが更新されたとき、送信バイトシーケンスに反映する
        /// </summary>
        /// <param name="field"></param>
        private void Update(Field field)
        {
            // 更新されたfieldをTxBufferに適用
            FrameRef.UpdateBuffer(field.selecter.FieldRef, field.Value.Value, TxBuffer);
            //// 更新メッセージ作成
            //var begin = field.selecter.FieldRef.BytePos;
            //var end = field.selecter.FieldRef.BytePos + field.selecter.FieldRef.ByteSize;
            //for (int pos = begin; pos < end; pos++)
            //{
            //    UpdateMsg.Value = new Serial.UpdateTxBuffMsg(Serial.UpdateTxBuffMsg.MsgType.UpdateBackupBuffer, Id, pos, Buffer[pos]);
            //}
            // チェックサムを持つframeで、更新fieldがチェックサムfieldでないとき、
            // チェックサムを再計算
            if (FrameRef.HasChecksum && !field.selecter.FieldRef.IsChecksum)
            {
                // チェックサム更新時はチェックサムfieldの更新により
                // Updateがコールされるため、ここではメッセージ作成しない
                Fields[FrameRef.ChecksumIndex].Value.Value = FrameRef.CalcChecksum(TxBuffer);
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
