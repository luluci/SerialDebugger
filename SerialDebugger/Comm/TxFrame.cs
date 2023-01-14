using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Windows;
using System.Collections.ObjectModel;

namespace SerialDebugger.Comm
{
    using Logger = SerialDebugger.Log.Log;

    class TxFrame : BindableBase, IDisposable
    {
        public int Id { get; }
        // 
        public string Name { get; }
        /// <summary>
        /// 送信フレーム: TxField集合体
        /// </summary>
        public ReactiveCollection<TxField> Fields { get; set; }
        /// <summary>
        /// 送信フレームバイト長
        /// </summary>
        public int Length { get; set; } = 0;
        public int BitLength { get; set; } = 0;
        /// <summary>
        /// 送信バイトシーケンス
        /// </summary>
        public ReactiveCollection<byte> TxBuffer { get; set; }
        /// <summary>
        /// 送信データセーブ用バッファ
        /// </summary>
        public ReactiveCollection<TxBackupBuffer> BackupBuffer { get; set; }
        /// <summary>
        /// 送信バッファ数
        /// </summary>
        public int BackupBufferLength { get; set; } = 0;

        // checksum
        public bool HasChecksum { get; set; } = false;
        public int ChecksumIndex { get; set; }

        public TxFrame(int id, string name)
        {
            Id = id;
            Name = name;

            Fields = new ReactiveCollection<TxField>();
            Fields
                .ObserveElementObservableProperty(x => x.Value).Subscribe(x =>
                {
                    Update(x.Instance);
                });
            Fields
                .ObserveElementObservableProperty(x => x.SelectIndexSelects).Subscribe(x =>
                {
                    Update(x.Instance);
                })
                .AddTo(Disposables);
            TxBuffer = new ReactiveCollection<byte>();
            TxBuffer.AddTo(Disposables);
            BackupBuffer = new ReactiveCollection<TxBackupBuffer>();
            // Saveボタン
            BackupBuffer.ObserveElementObservableProperty(x => x.OnClickSave).Subscribe(x =>
            {
                var buffer = x.Instance;
                // 送信バイトシーケンスコピー
                for (int i=0; i<TxBuffer.Count; i++)
                {
                    buffer.Buffer[i] = TxBuffer[i];
                }
                // 表示文字列コピー
                for (int i=0; i<Fields.Count; i++)
                {
                    var field = Fields[i];
                    var bk_field = buffer.Fields[i];
                    bk_field.Value.Value = field.Value.Value;
                }
            });
            // Storeボタン
            BackupBuffer.ObserveElementObservableProperty(x => x.OnClickStore).Subscribe(x =>
            {
                var buffer = x.Instance;
                for (int i = 0; i < Fields.Count; i++)
                {
                    var field = Fields[i];
                    var bk_field = buffer.Fields[i];
                    field.Value.Value = bk_field.Value.Value;
                }
            });
            BackupBuffer.AddTo(Disposables);
        }

        public void Add(TxField field)
        {
            Fields.Add(field);
        }

        /// <summary>
        /// 登録したFieldからFrame情報を構築する。
        /// Build()は一度のみ行い、内部情報をクリアしてのインスタンス再利用はしない。
        /// *特にブロックはかけていないので運用でカバー
        /// </summary>
        public void Build()
        {
            // Fieldから情報収集
            int field_no = 0;
            UInt64 buff = 0;
            int bit_pos = 0;
            int byte_pos = 0;
            int disp_len = 0;
            foreach (var f in Fields)
            {
                // Field位置セット
                f.BitPos = bit_pos;
                f.BytePos = byte_pos;
                f.ByteSize = (bit_pos + f.BitSize + 7) / 8;
                // Frame情報更新
                BitLength += f.BitSize;
                // 送信生データ作成
                buff |= (f.Value.Value) << f.BitPos;
                // Field位置更新
                bit_pos += f.BitSize;
                while (bit_pos >= 8)
                {
                    // 送信生データに1バイトたまったら送信バイトシーケンスに登録
                    TxBuffer.Add((byte)(buff & 0xFF));
                    // 登録分を生データから削除
                    buff >>= 8;
                    //
                    byte_pos++;
                    bit_pos -= 8;
                }
                //
                if ((f.BitSize % 8) == 0 && f.BitPos == 0)
                {
                    f.IsByteDisp = true;
                }
                // 表示データ数
                disp_len += f.InnerFields.Count;
                // checksum
                if (f.IsChecksum)
                {
                    // ChecksumFieldは1つだけ対応
                    if (HasChecksum)
                    {
                        Logger.Add($"チェックサムフィールドは1つだけ指定してください : Frame={Name} Field={Fields[ChecksumIndex].Name}");
                    }
                    // 後優先で上書きする
                    HasChecksum = true;
                    ChecksumIndex = field_no;
                }
                //
                field_no++;
            }
            // 定義がバイト単位でなくビットに残りがあった場合
            if (bit_pos > 0)
            {
                // 送信生データに1バイトたまったら送信バイトシーケンスに登録
                TxBuffer.Add((byte)(buff & 0xFF));
                // 登録分を生データから削除
                buff >>= 8;
                //
                byte_pos++;
                bit_pos -= 8;
            }
            // バイトサイズ計算
            Length = (BitLength / 8);
            if ((BitLength % 8) != 0)
            {
                Length++;
            }
            // チェックサム整合性チェック
            if (HasChecksum)
            {
                var cs = Fields[ChecksumIndex];
                // チェックサム計算範囲がChecksumノードをまたがる、または、要素数を上回るときNG
                if ((cs.Checksum.End >= cs.BytePos) || (cs.Checksum.End >= Length))
                {
                    cs.Checksum.End = cs.BytePos > Length ? Length : cs.BytePos;
                    cs.Checksum.End--;
                    Logger.Add($"Checksum Range is invalid: Fix to {cs.Checksum.Begin}-{cs.Checksum.End}");
                }
                //
                UpdateChecksum(TxBuffer);
            }
        }

        /// <summary>
        /// Fieldsが更新されたとき、送信バイトシーケンスに反映する
        /// </summary>
        /// <param name="field"></param>
        private void Update(TxField field)
        {
            // 更新されたfieldをTxBufferに適用
            UpdateBuffer(field, field.Value.Value, TxBuffer);
            // チェックサムを持つframeで、更新fieldがチェックサムfieldでないとき、
            // チェックサムを再計算
            if (HasChecksum && !field.IsChecksum)
            {
                UpdateChecksum(TxBuffer);
            }
        }

        /// <summary>
        /// fieldの値をbufferに適用する。
        /// BackupBuffer更新に流用する。
        /// </summary>
        /// <param name="field"></param>
        /// <param name="buffer"></param>
        public void UpdateBuffer(TxField field, UInt64 value, IList<byte> buffer)
        {
            UInt64 mask = field.Mask;
            UInt64 inv_mask = field.InvMask;
            // 1回目はvalueのビット位置がbit_pos分右にあるが、
            // このタイミングで左シフトすると有効データを捨てる可能性があるのでループ内で計算していく
            int shift = 8 - field.BitPos;
            int bit_pos = field.BitPos;
            int byte_pos = field.BytePos;
            UInt64 inv_mask_shift_fill = ((UInt64)1 << bit_pos) - 1;
            for (int i = 0; i < field.ByteSize; i++, byte_pos++)
            {
                // 対象バイトを抽出
                byte temp_mask = (byte)((mask << bit_pos) & 0xFF);
                byte temp_value = (byte)((value << bit_pos) & temp_mask);
                byte temp_inv_mask = (byte)((inv_mask << bit_pos) | inv_mask_shift_fill & 0xFF);
                // 送信バイトシーケンスに適用
                // 該当ビットをクリア
                buffer[byte_pos] &= temp_inv_mask;
                // 該当ビットにセット
                buffer[byte_pos] |= temp_value;

                // 使ったデータを削除
                value >>= shift;
                mask >>= shift;
                inv_mask >>= shift;
                // 次の計算に合わせて設定更新
                // 2回目以降は0ビット目の位置が合っている
                bit_pos = 0;
                shift = 8;
                inv_mask_shift_fill = 0;
            }
        }

        private void UpdateChecksum(IList<byte> buffer)
        {
            var cs = Fields[ChecksumIndex];
            cs.Value.Value = CalcChecksum(buffer);
        }

        /// <summary>
        /// チェックサム計算
        /// チェックサムfieldはビット単位で位置を指定できるが、バイトアライメントで配置されてる前提で計算する。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public UInt64 CalcChecksum(IList<byte> buffer)
        {
            var cs = Fields[ChecksumIndex];
            // 合計算出
            UInt64 sum = 0;
            for (int i = cs.Checksum.Begin; i <= cs.Checksum.End; i++)
            {
                sum += buffer[i];
            }
            // method
            switch (cs.Checksum.Method)
            {
                case TxField.ChecksumMethod.cmpl_2:
                    // 2の補数
                    sum = ~sum + 1;
                    sum &= cs.Mask;
                    break;
                case TxField.ChecksumMethod.cmpl_1:
                    // 1の補数
                    sum = ~sum;
                    sum &= cs.Mask;
                    break;
                case TxField.ChecksumMethod.None:
                default:
                    // 総和
                    sum &= cs.Mask;
                    break;
            }

            return sum;
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
