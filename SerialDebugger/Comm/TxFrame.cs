using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace SerialDebugger.Comm
{
    using Utility;
    using Setting = Settings.Settings;
    using Logger = SerialDebugger.Log.Log;
    
    public class TxFrame : BindableBase, IDisposable
    {
        public int Id { get; }
        // 
        public string Name { get; }
        /// <summary>
        /// 送信フレーム: Field集合体
        /// </summary>
        public ReactiveCollection<Field> Fields { get; set; }
        /// <summary>
        /// 送信フレームバイト長
        /// </summary>
        public int Length { get; set; } = 0;
        public int BitLength { get; set; } = 0;

        //
        public int BufferSize { get; }
        // FieldValueコンテナのコンテナ
        public ReactiveCollection<TxFieldBuffer> Buffers { get; set; }
        // ASCIIで送信するかどうか
        public bool AsAscii { get; set; } = false;

        /// <summary>
        /// TxFrame全体の変更状況
        /// </summary>
        public ReactivePropertySlim<Field.ChangeStates> ChangeState { get; set; }

        // checksum
        public bool HasChecksum { get; set; } = false;
        public int ChecksumIndex { get; set; }

        public TxFrame(int id, string name, int buffer_size, bool ascii)
        {
            //
            Id = id;
            Name = name;
            BufferSize = buffer_size;
            AsAscii = ascii;
            //
            Fields = new ReactiveCollection<Field>();
            Fields
                .ObserveElementObservableProperty(x => x.OnMouseDown).Subscribe((x) =>
                {
                    DoDragDrop(x.Instance, x.Value as System.Windows.Input.MouseButtonEventArgs);
                });
            Fields.AddTo(Disposables);


            Buffers = new ReactiveCollection<TxFieldBuffer>();
            Buffers.AddTo(Disposables);
            //
            ChangeState = new ReactivePropertySlim<Field.ChangeStates>();
            ChangeState.AddTo(Disposables);

            // Fieldsの値を格納するバッファを必ず1つ持つ
            Buffers.Add(new TxFieldBuffer(0, Name, this));

            // Saveボタン
            Buffers.ObserveElementObservableProperty(x => x.OnClickSave).Subscribe(x =>
            {
                var src = Buffers[0];
                var dst = x.Instance;
                // 送信バイトシーケンスコピー
                for (int i = 0; i < src.Buffer.Count; i++)
                {
                    dst.Buffer[i] = src.Buffer[i];
                }
                // 表示文字列コピー
                for (int i = 0; i < Fields.Count; i++)
                {
                    dst.FieldValues[i].Value.Value = src.FieldValues[i].Value.Value;
                }
            });
            // Storeボタン
            Buffers.ObserveElementObservableProperty(x => x.OnClickStore).Subscribe(x =>
            {
                var dst = Buffers[0];
                var src = x.Instance;
                for (int i = 0; i < Fields.Count; i++)
                {
                    dst.FieldValues[i].Value.Value = src.FieldValues[i].Value.Value;
                }
            });
            Buffers.AddTo(Disposables);
        }

        public void Add(Field field)
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
            byte data = 0;
            int field_no = 0;
            Int64 value = 0;
            Int64 inival = 0;
            int bit_pos = 0;
            int byte_pos = 0;
            int disp_len = 0;
            foreach (var f in Fields)
            {
                // BuffersにFieldを登録
                foreach (var buff in Buffers)
                {
                    buff.FieldValues.Add(new FieldValue(f));
                }
                // Field位置セット
                f.BitPos = bit_pos;
                f.BytePos = byte_pos;
                f.ByteSize = (bit_pos + f.BitSize + 7) / 8;
                // Frame情報更新
                BitLength += f.BitSize;
                // 送信生データ作成
                inival = f.InitValue;
                if (f.IsReverseEndian)
                {
                    inival = f.ReverseEndian(inival);
                }
                value |= (inival & f.Mask) << f.BitPos;
                // Field位置更新
                bit_pos += f.BitSize;
                while (bit_pos >= 8)
                {
                    // 送信生データに1バイトたまったら送信バイトシーケンスに登録
                    data = (byte)(value & 0xFF);
                    foreach (var buff in Buffers)
                    {
                        buff.Buffer.Add(data);
                    }
                    // 登録分を生データから削除
                    value >>= 8;
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
                data = (byte)(value & 0xFF);
                foreach (var buff in Buffers)
                {
                    buff.Buffer.Add(data);
                }
                // 登録分を生データから削除
                value >>= 8;
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
                // チェックサム計算範囲末尾省略時はチェックサムノード手前を指定
                if (cs.Checksum.End == -1)
                {
                    cs.Checksum.End = cs.BytePos - 1;
                }
                // チェックサム計算範囲がChecksumノードをまたがる、または、要素数を上回るときNG
                if ((cs.Checksum.End >= cs.BytePos) || (cs.Checksum.End >= Length))
                {
                    cs.Checksum.End = cs.BytePos > Length ? Length : cs.BytePos;
                    cs.Checksum.End--;
                    Logger.Add($"Checksum Range is invalid: Fix to {cs.Checksum.Begin}-{cs.Checksum.End}");
                }
                // チェックサム計算して初期化
                var sum = CalcChecksum(Buffers[0].Buffer);
                foreach (var buff in Buffers)
                {
                    buff.FieldValues[ChecksumIndex].Value.Value = sum;
                }
            }
            // 送信データ作成
            if (AsAscii)
            {
                // 2個目以降はバッファコピーで十分だが
                foreach (var buff in Buffers)
                {
                    buff.Data = new byte[buff.Buffer.Count * 2];
                    for (int i = 0; i < buff.Buffer.Count; i++)
                    {
                        var ch = Utility.HexAscii.AsciiTbl[buff.Buffer[i]];
                        buff.Data[i * 2 + 0] = (byte)ch[0];
                        buff.Data[i * 2 + 1] = (byte)ch[1];
                    }
                }
            }
            else
            {
                foreach (var buff in Buffers)
                {
                    buff.Data = buff.Buffer.ToArray();
                }
            }
        }

        /// <summary>
        /// Fieldsが更新されたとき、送信バイトシーケンスに反映する
        /// </summary>
        /// <param name="value"></param>
        public void Update(TxFieldBuffer buffer, FieldValue value)
        {
            // 更新されたfieldをTxBufferに適用
            UpdateBuffer(buffer, value);
            // チェックサムを持つframeで、更新fieldがチェックサムfieldでないとき、
            // チェックサムを再計算
            if (HasChecksum && !value.FieldRef.IsChecksum)
            {
                // チェックサム更新時はチェックサムfieldの更新により
                // Updateがコールされるため、ここではメッセージ作成しない
                UpdateChecksum(buffer);
            }
        }

        /// <summary>
        /// fieldの値をbufferに適用する。
        /// BackupBuffer更新に流用する。
        /// </summary>
        /// <param name="field"></param>
        /// <param name="buffer"></param>
        public void UpdateBuffer(TxFieldBuffer buffer, FieldValue value)
        {
            Int64 temp = value.Value.Value;
            if (value.FieldRef.IsReverseEndian)
            {
                temp = value.FieldRef.ReverseEndian(temp);
            }
            Int64 mask = value.FieldRef.Mask;
            Int64 inv_mask = value.FieldRef.InvMask;
            // 1回目はvalueのビット位置がbit_pos分右にあるが、
            // このタイミングで左シフトすると有効データを捨てる可能性があるのでループ内で計算していく
            int shift = 8 - value.FieldRef.BitPos;
            int bit_pos = value.FieldRef.BitPos;
            int byte_pos = value.FieldRef.BytePos;
            Int64 inv_mask_shift_fill = ((Int64)1 << bit_pos) - 1;
            for (int i = 0; i < value.FieldRef.ByteSize; i++, byte_pos++)
            {
                // 対象バイトを抽出
                byte temp_mask = (byte)((mask << bit_pos) & 0xFF);
                byte temp_value = (byte)((temp << bit_pos) & temp_mask);
                byte temp_inv_mask = (byte)((inv_mask << bit_pos) | inv_mask_shift_fill & 0xFF);
                // 送信バイトシーケンスに適用
                // 該当ビットをクリア
                buffer.Buffer[byte_pos] &= temp_inv_mask;
                // 該当ビットにセット
                buffer.Buffer[byte_pos] |= temp_value;

                // 使ったデータを削除
                temp >>= shift;
                mask >>= shift;
                inv_mask >>= shift;
                // 次の計算に合わせて設定更新
                // 2回目以降は0ビット目の位置が合っている
                bit_pos = 0;
                shift = 8;
                inv_mask_shift_fill = 0;
            }
        }

        public void UpdateChecksum(TxFieldBuffer buffer)
        {
            var cs = buffer.FieldValues[ChecksumIndex];
            cs.Value.Value = CalcChecksum(buffer.Buffer);
        }

        /// <summary>
        /// チェックサム計算
        /// チェックサムfieldはビット単位で位置を指定できるが、バイトアライメントで配置されてる前提で計算する。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Int64 CalcChecksum(IList<byte> buffer)
        {
            var cs = Fields[ChecksumIndex];
            // 合計算出
            Int64 sum = 0;
            for (int i = cs.Checksum.Begin; i <= cs.Checksum.End; i++)
            {
                sum += buffer[i];
            }
            // method
            switch (cs.Checksum.Method)
            {
                case Field.ChecksumMethod.cmpl_2:
                    // 2の補数
                    sum = ~sum + 1;
                    sum &= cs.Mask;
                    break;
                case Field.ChecksumMethod.cmpl_1:
                    // 1の補数
                    sum = ~sum;
                    sum &= cs.Mask;
                    break;
                case Field.ChecksumMethod.None:
                default:
                    // 総和
                    sum &= cs.Mask;
                    break;
            }

            return sum;
        }

        public void DoDragDrop(Field field, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragDrop.DoDragDrop(e.Source as TextBlock, MakeDragDropStr(field), DragDropEffects.Copy);
        }

        public string MakeDragDropStr(Field field)
        {
            // DragDrop設定参照取得
            var dd = field.DragDropInfo;
            if (dd is null)
            {
                dd = Setting.Data.Output.DragDrop;
            }
            //var value = field.Value.Value;
            var fv = Buffers[0].FieldValues[field.Id];
            var value = fv.Value.Value;
            var str = new StringBuilder();

            // DragDrop設定が無いときのデフォルト
            if (dd is null)
            {
                return $"{Name}/{field.Name}/0x{value:X}";
            }

            // DragDrop設定があるとき
            // Body: 全体
            if (dd.EnableBodyBegin) str.Append(dd.Body.Begin);

            // Name/Value
            if (dd.EnableField)
            {
                if (dd.EnableItemBegin) str.Append(dd.Item.Begin);

                // FrameName
                if (dd.EnableFrame)
                {
                    if (dd.EnableFrameBegin) str.Append(dd.FrameName.Begin);
                    str.Append(Name);
                    if (dd.EnableFrameEnd) str.Append(dd.FrameName.End);
                }
                // Name
                if (dd.EnableFieldName)
                {
                    if (dd.EnableFieldNameBegin) str.Append(dd.FieldName.Begin);
                    str.Append(field.Name);
                    if (dd.EnableFieldNameEnd) str.Append(dd.FieldName.End);
                }
                // Value
                if (dd.EnableFieldValue)
                {
                    if (dd.EnableFieldValueBegin) str.Append(dd.FieldValue.Begin);

                    switch (dd.ValueFormat)
                    {
                        case Settings.Output.DragDropValueFormat.Input:
                            str.Append(field.MakeDisp(fv.SelectIndex.Value, value));
                            break;

                        default:
                            str.Append($"0x{value:X}");
                            break;
                    }

                    if (dd.EnableFieldValueEnd) str.Append(dd.FieldValue.End);
                }

                if (dd.EnableItemEnd) str.Append(dd.Item.End);
            }

            // InnerName/Value
            if (dd.EnableInnerField)
            {
                // Innerデータにはendian反転を適用する
                if (field.IsReverseEndian)
                {
                    value = field.ReverseEndian(value);
                }
                for (int i = 0; i < field.InnerFields.Count; i++)
                {
                    // 該当データ計算
                    var inner = field.InnerFields[i];
                    var mask = ((Int64)1 << inner.BitSize) - 1;
                    // html生成
                    if (dd.EnableItemBegin) str.Append(dd.Item.Begin);

                    // FrameName
                    if (dd.EnableFrame)
                    {
                        if (dd.EnableFrameBegin) str.Append(dd.FrameName.Begin);
                        str.Append(Name);
                        if (dd.EnableFrameEnd) str.Append(dd.FrameName.End);
                    }
                    // Name
                    if (dd.EnableInnerFieldName)
                    {
                        if (dd.EnableInnerFieldNameBegin) str.Append(dd.InnerFieldName.Begin);
                        str.Append(inner.Name);
                        if (dd.EnableInnerFieldNameEnd) str.Append(dd.InnerFieldName.End);
                    }
                    // Value
                    if (dd.EnableInnerFieldValue)
                    {
                        if (dd.EnableInnerFieldValueBegin) str.Append(dd.InnerFieldValue.Begin);
                        str.Append($"0x{(value & mask):X}");
                        if (dd.EnableInnerFieldValueEnd) str.Append(dd.InnerFieldValue.End);
                    }

                    if (dd.EnableItemEnd) str.Append(dd.Item.End);
                    // 後処理
                    value >>= inner.BitSize;
                }
            }

            if (dd.EnableBodyEnd) str.Append(dd.Body.End);

            return str.ToString();
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
