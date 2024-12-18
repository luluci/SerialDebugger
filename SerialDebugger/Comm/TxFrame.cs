﻿using System;
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
        // LogをVisualizeするか
        public bool IsLogVisualize { get; }

        //
        public ReactiveCollection<Group> Groups { get; set; }

        /// <summary>
        /// TxFrame全体の変更状況
        /// </summary>
        public ReactivePropertySlim<Field.ChangeStates> ChangeState { get; set; }

        // GUI: スクロールバーに対する相対位置
        public Point Point;

        // checksum
        public bool HasChecksum { get; set; }
        public List<Field> ChecksumRefList { get; set; }
        // Byte配列に対するチェックサムかどうかフラグ
        public bool[] IsChecksumNode { get; set; }

        public TxFrame(int id, string name, int buffer_size, bool ascii, bool log_visualize)
        {
            //
            Id = id;
            Name = name;
            BufferSize = buffer_size;
            AsAscii = ascii;
            IsLogVisualize = log_visualize;
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
            Groups = new ReactiveCollection<Group>();
            Groups.AddTo(Disposables);
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

            // checksum
            HasChecksum = false;
            ChecksumRefList = new List<Field>();
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
            foreach (var field in Fields)
            {
                // BuffersにFieldを登録
                foreach (var buff in Buffers)
                {
                    buff.FieldValues.Add(new FieldValue(field));
                }
                // Field位置セット
                field.BitPos = bit_pos;
                field.BytePos = byte_pos;
                field.ByteSize = (bit_pos + field.BitSize + 7) / 8;
                // Frame情報更新
                BitLength += field.BitSize;
                // 送信生データ作成
                inival = field.InitValue;
                if (field.IsReverseEndian)
                {
                    inival = field.ReverseEndian(inival);
                }
                value |= (inival & field.Mask) << field.BitPos;
                // Field位置更新
                bit_pos += field.BitSize;
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
                if ((field.BitSize % 8) == 0 && field.BitPos == 0)
                {
                    field.IsByteDisp = true;
                }
                // 表示データ数
                disp_len += field.InnerFields.Count;
                // checksum
                if (field.IsChecksum)
                {
                    // ChecksumField複数設定を許可する
                    // 後優先で上書きする
                    HasChecksum = true;
                    ChecksumRefList.Add(field);
                }
                //
                if (field.Id != field_no)
                {
                    Logger.Add($"Logic error?");
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
                // チェックサム計算対象設定
                IsChecksumNode = new bool[Length];
                //
                foreach (var cs in ChecksumRefList)
                {
                    // チェックサム計算範囲末尾省略時はチェックサムノード手前を指定
                    if (cs.Checksum.End == -1)
                    {
                        cs.Checksum.End = cs.BytePos - 1;
                    }
                    // チェックサム計算範囲が要素数を上回るときNG
                    if (cs.Checksum.End >= Length)
                    {
                        cs.Checksum.End = Length - 1;
                        Logger.Add($"Checksum Range is invalid: Fix to {cs.Checksum.Begin}-{cs.Checksum.End}");
                    }
                    // チェックサムノードが対応するバッファをChecksumとしてマーク
                    for (var idx = 0; idx < cs.ByteSize; idx++)
                    {
                        IsChecksumNode[cs.BytePos + idx] = true;
                    }
                }
                // チェックサム情報設定後にチェックサム初期値を計算して更新
                foreach (var cs in ChecksumRefList)
                {
                    // チェックサム計算して初期化
                    var sum = CalcChecksum(Buffers[0].Buffer, cs);
                    foreach (var buff in Buffers)
                    {
                        buff.FieldValues[cs.Id].Value.Value = sum;
                    }
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
            foreach (var cs in ChecksumRefList)
            {
                var csv = buffer.FieldValues[cs.Id];
                csv.Value.Value = CalcChecksum(buffer.Buffer, cs);
            }
        }

        /// <summary>
        /// チェックサム計算
        /// チェックサムfieldはビット単位で位置を指定できるが、バイトアライメントで配置されてる前提で計算する。
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public Int64 CalcChecksum(IList<byte> buffer, Field cs)
        {
            // 合計算出
            Int64 sum = CalcSum(buffer, cs);
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
        public Int64 CalcSum(IList<byte> buffer, Field cs)
        {
            // Sum算出
            Int64 sum = 0;
            if (cs.Checksum.WordSize == 1)
            {
                // WordSize=1用処理
                // バッファから単純に総和を取るだけなので専用処理で定義する
                for (int i = cs.Checksum.Begin; i <= cs.Checksum.End; i++)
                {
                    if (!IsChecksumNode[i])
                    {
                        sum += buffer[i];
                    }
                }
            }
            else
            {
                // WordSize>1用処理
                if (!cs.Checksum.WordEndian)
                {
                    // WordEndial=little-endian
                    // Little-Endianで計算
                    var word_pos = 0;
                    for (int i = cs.Checksum.Begin; i <= cs.Checksum.End; i++)
                    {
                        if (!IsChecksumNode[i])
                        {
                            sum += buffer[i] << (word_pos * 8);
                        }
                        // Wordはバッファ全体の並びで判定するので
                        // ChecksumFieldでも考慮する
                        word_pos++;
                        if (word_pos >= cs.Checksum.WordSize)
                        {
                            word_pos = 0;
                        }
                    }
                }
                else
                {
                    // WordEndial=big-endian
                    // Big-Endianで計算
                    var word_pos = cs.Checksum.WordSize - 1;
                    for (int i = cs.Checksum.Begin; i <= cs.Checksum.End; i++)
                    {
                        if (!IsChecksumNode[i])
                        {
                            sum += buffer[i] << (word_pos * 8);
                        }
                        // Wordはバッファ全体の並びで判定するので
                        // ChecksumFieldでも考慮する
                        word_pos--;
                        if (word_pos < 0)
                        {
                            word_pos = cs.Checksum.WordSize - 1;
                        }
                    }
                }
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

        public void SetString2CharField(string str, int field_idx, int buffer_idx)
        {
            // UTF16 -> UTF8
            var bytes = System.Text.Encoding.UTF8.GetBytes(str);
            // 先頭Fieldチェック
            var root = Fields[field_idx];
            var str_len = root.selecter.StrLen;
            var str_max_pos = str_len;
            // UTF8バイト列がCharFieldより大きい場合は1文字分のバイト列が入るところまでで区切る
            if (bytes.Length > str_len)
            {
                // 後ろ側から探索
                // 上位2bitが0b10だと複数バイトシーケンスの2バイト目以降
                // 1バイト目が出現するまでループ
                while ((bytes[str_max_pos] & 0xC0) == 0x80 && str_max_pos >= 0)
                {
                    str_max_pos--;
                }
            }
            else
            {
                str_max_pos = bytes.Length;
            }
            // コピー可能な分のbytesをTxFieldBufferにコピー
            int idx = 0;
            for (; idx < str_max_pos; idx++)
            {
                Buffers[buffer_idx].FieldValues[field_idx + idx].Value.Value = bytes[idx];
            }
            // 入力が無かったバッファはゼロクリア
            for (; idx < str_len; idx++)
            {
                Buffers[buffer_idx].FieldValues[field_idx + idx].Value.Value = 0;
            }
        }

        public string MakeCharField2String(int field_idx, int buffer_idx)
        {
            // 初期設定
            if (field_idx < Fields.Count && Fields[field_idx].InputType == Field.InputModeType.Char)
            {

            }
            else
            {
                return "";
            }
            // 先頭Fieldチェック
            var root = Fields[field_idx];
            var str_len = root.selecter.StrLen;
            var raw = new byte[str_len];
            var raw_idx = 0;
            raw[raw_idx] = Buffers[buffer_idx].Buffer[field_idx];
            raw_idx++;
            //
            for (; raw_idx<str_len; raw_idx++)
            {
                raw[raw_idx] = Buffers[buffer_idx].Buffer[field_idx+raw_idx];
            }
            
            return System.Text.Encoding.UTF8.GetString(raw);
        }

        public string MakeLogVisualize(int buff_idx)
        {
            var log = new StringBuilder();
            var buffer = Buffers[buff_idx];
            
            bool is_first = true;
            string str;
            string char_type_name = string.Empty;
            bool prev_is_char = false;
            var chars = new char[Fields.Count];
            var prev_char_id = -1;
            var chars_size = 0;

            for (int field_id = 0; field_id < Fields.Count; field_id++)
            {
                var field = Fields[field_id];
                var value = buffer.FieldValues[field_id];
                // Char[]判定
                if (field.InputType == Field.InputModeType.Char)
                {
                    // 文字列ログ作成開始処理
                    if (prev_char_id == -1)
                    {
                        prev_char_id = field.selecter.CharId;
                        char_type_name = field.Name;
                    }
                    // charは連続しているが異なる文字列定義の境目判定
                    // 一旦これまでの文字列ログをコミットして新しく文字列ログを作成開始する
                    if (prev_char_id != field.selecter.CharId)
                    {
                        // separator
                        if (!is_first)
                        {
                            log.Append(", ");
                        }
                        log.Append(char_type_name).Append("=").Append(chars, 0, chars_size);
                        is_first = false;
                        chars_size = 0;
                        prev_char_id = field.selecter.CharId;
                        char_type_name = field.Name;
                    }
                    chars[chars_size] = (char)(value.Value.Value & 0xFF);
                    chars_size++;

                    prev_is_char = true;
                }
                else
                {
                    // 直前までcharログだったとき、これまでの文字列ログをコミット
                    if (prev_is_char)
                    {
                        // separator
                        if (!is_first)
                        {
                            log.Append(", ");
                        }
                        log.Append(char_type_name).Append("=").Append(chars, 0, chars_size);
                        is_first = false;
                        chars_size = 0;
                        prev_char_id = -1;
                    }
                    // separator
                    if (!is_first)
                    {
                        log.Append(", ");
                    }
                    // match定義が無いときはfield定義から作成
                    str = field.MakeDispByValue(value.Value.Value);
                    log.Append(field.Name).Append("=").Append(str);

                    is_first = false;
                    prev_is_char = false;
                }
            }
            // Char[]判定
            if (prev_is_char)
            {
                // separator
                if (!is_first)
                {
                    log.Append(", ");
                }
                log.Append(char_type_name).Append("=").Append(chars, 0, chars_size);
                is_first = false;
                chars_size = 0;
            }

            return log.ToString();
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
