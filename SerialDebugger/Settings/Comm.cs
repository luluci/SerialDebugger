
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

using SerialDebugger.Comm;
using System.Windows;

namespace SerialDebugger.Settings
{
    class Comm : BindableBase, IDisposable
    {
        public ReactiveCollection<TxFrame> Tx { get; set; }
        // rx_autoresp
        // tx_autosend

        public Comm()
        {
            Tx = new ReactiveCollection<TxFrame>();
            Tx.AddTo(Disposables);
        }

        public void AnalyzeJson(Json.Comm json)
        {
            if (json is null || json.Tx is null)
            {
                throw new Exception("txキーが定義されていません");
            }

            // TxFrameコレクション作成
            foreach (var frame in json.Tx.Frames)
            {
                Tx.Add(MakeTxFrame(frame));
            }
        }


        /// <summary>
        /// JsonTxFrameからTxFrameを作成する
        /// </summary>
        /// <param name="frame"></param>
        private TxFrame MakeTxFrame(Json.CommTxFrame frame)
        {
            // TxFrame作成
            var f = new TxFrame(frame.Name);
            if (!(frame.Fields is null))
            {
                foreach (var field in frame.Fields)
                {
                    f.Fields.Add(MakeTxField(field));
                }
                f.Build();
            }
            // TxFrame作成後にBackupBuffer作成
            MakeTxBackupBuffers(frame, f);

            return f;
        }

        private void MakeTxBackupBuffers(Json.CommTxFrame frame, TxFrame f)
        {
            // バッファサイズ作成
            f.BackupBufferLength = frame.BackupBufferSize;
            if (!(frame.BackupBuffers is null))
            {
                if (f.BackupBufferLength < frame.BackupBuffers.Count)
                {
                    f.BackupBufferLength = frame.BackupBuffers.Count;
                }
            }

            // 送信データバックアップバッファ作成
            if (!(frame.BackupBuffers is null))
            {
                for (int i = 0; i < f.BackupBufferLength; i++)
                {
                    if (i < frame.BackupBuffers.Count)
                    {
                        // BackupBuffers定義から作成
                        f.BackupBuffer.Add(MakeTxBackupBuffer(frame.BackupBuffers[i], f, i));
                    }
                    else
                    {
                        // 空バッファを作成
                        f.BackupBuffer.Add(MakeTxBackupBufferEmpty($"buffer[{i}]", f, i));
                    }
                }
            }
            else
            {
                // BackupBuffers未定義ならすべて空バッファ作成
                for (int i = 0; i < f.BackupBufferLength; i++)
                {
                    f.BackupBuffer.Add(MakeTxBackupBufferEmpty($"buffer[{i}]", f, i));
                }
            }
        }

        private TxBackupBuffer MakeTxBackupBuffer(Json.CommTxBackupBuffer json, TxFrame f, int idx)
        {
            // Buffer名称作成
            string name = json.Name;
            if (name == string.Empty)
            {
                name = $"buffer[{idx}]";
            }
            // value定義が無いなら空バッファを返す
            if (json.Values is null)
            {
                return MakeTxBackupBufferEmpty(name, f, idx);
            }

            // Buffer作成
            var buffer = new TxBackupBuffer(name, f.Fields.Count, f.Length);

            // Values作成
            for (int i=0; i<f.Fields.Count; i++)
            {
                var field = f.Fields[i];
                if (i < json.Values.Count)
                {
                    // valueをバッファに反映
                    var value = json.Values[i];
                    buffer.Value[i] = value;
                    f.UpdateBuffer(field, value, buffer.Buffer);
                    // 表示名を作成
                    var index = field.GetSelectsIndex(value);
                    buffer.Disp[i] = field.MakeDisp(index, value);
                }
            }
            // チェックサム
            if (f.HasChecksum)
            {
                var i = f.ChecksumIndex;
                var field = f.Fields[i];
                var value = f.CalcChecksum(buffer.Buffer);
                buffer.Value[i] = value;
                f.UpdateBuffer(field, value, buffer.Buffer);
                // 表示名を作成
                var index = field.GetSelectsIndex(value);
                buffer.Disp[i] = field.MakeDisp(index, value);
            }

            return buffer;
        }
        private TxBackupBuffer MakeTxBackupBufferEmpty(string name, TxFrame f, int idx)
        {
            // Buffer作成
            return new TxBackupBuffer(name, f.Fields.Count, f.Length);
        }

        private TxField MakeTxField(Json.CommTxField field)
        {
            switch (field.Type)
            {
                case "Checksum":
                    return MakeTxFieldChecksum(field);
                case "Dict":
                    return MakeTxFieldDict(field);
                case "Unit":
                    return MakeTxFieldUnit(field);
                case "Time":
                    return MakeTxFieldTime(field);
                case "Edit":
                    return MakeTxFieldImpl(field, TxField.InputModeType.Edit, null);
                case "Fix":
                default:
                    return MakeTxFieldImpl(field, TxField.InputModeType.Fix, null);
            }
        }

        private TxField MakeTxFieldChecksum(Json.CommTxField field)
        {
            // Checksumチェック
            if (field.Checksum is null)
            {
                throw new Exception("type:ChecksumではChecksumオブジェクトを指定してください。");
            }
            // Nameチェック
            // Checksumの場合はmulti_nameは許可しない
            if (field.Name is null)
            {
                throw new Exception("Checksumノードはname,bit_sizeを指定してください。");
            }
            // Checksum計算方法
            var method = new TxField.ChecksumMethod();
            switch (field.Checksum.Method)
            {
                case "2compl":
                    // 2の補数
                    method = TxField.ChecksumMethod.cmpl_2;
                    break;
                case "1compl":
                    // 1の補数
                    method = TxField.ChecksumMethod.cmpl_1;
                    break;
                case "Sum":
                default:
                    // 総和
                    method = TxField.ChecksumMethod.None;
                    break;
            }
            // TxField生成
            return new TxField(new TxField.ChecksumNode
            {
                Name = field.Name,
                BitSize = field.BitSize,
                Begin = field.Checksum.Begin,
                End = field.Checksum.End,
                Method = method,
            });
        }

        private TxField MakeTxFieldDict(Json.CommTxField field)
        {
            // nullチェック
            var dict = field.Dict;
            if (dict is null)
            {
                throw new Exception("type:DictではDictオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = new (UInt64, string)[dict.Count];
            int i = 0;
            foreach (var pair in dict)
            {
                selecter[i] = (pair.Value, pair.Disp);
                i++;
            }
            // TxField生成
            return MakeTxFieldImpl(field, TxField.InputModeType.Dict, TxField.MakeSelecterDict(selecter));
        }

        private TxField MakeTxFieldUnit(Json.CommTxField field)
        {
            // nullチェック
            var unit = field.Unit;
            if (unit is null)
            {
                throw new Exception("type:UnitではUnitオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = TxField.MakeSelecterUnit(unit.Unit, unit.Lsb, unit.DispMax, unit.DispMin, unit.ValueMin, unit.Format);
            // TxField生成
            return MakeTxFieldImpl(field, TxField.InputModeType.Unit, selecter);
        }

        private TxField MakeTxFieldTime(Json.CommTxField field)
        {
            // nullチェック
            var time = field.Time;
            if (time is null)
            {
                throw new Exception("type:TimeではTimeオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = TxField.MakeSelecterTime(time.Elapse, time.Begin, time.End, time.ValueMin);
            // TxField生成
            return MakeTxFieldImpl(field, TxField.InputModeType.Time, selecter);
        }

        private TxField MakeTxFieldImpl(Json.CommTxField field, TxField.InputModeType type, TxField.Selecter selecter)
        {
            // name, multi_name選択
            if (!(field.MultiNames is null))
            {
                // multi_name優先
                int i = 0;
                var multi_name = new TxField.InnerField[field.MultiNames.Count];
                foreach (var pair in field.MultiNames)
                {
                    multi_name[i] = new TxField.InnerField(pair.Name, pair.BitSize);
                    i++;
                }
                // TxField生成
                return new TxField(multi_name, field.Value, type, selecter);
            }
            else
            {
                // name
                if (field.Name is null)
                {
                    throw new Exception("nameかmulti_nameのどちらかを指定してください。");
                }
                // TxField生成
                return new TxField(field.Name, field.BitSize, field.Value, type, selecter);
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

    partial class Json
    {

        public class Comm
        {
            // 送信設定
            [JsonPropertyName("tx")]
            public CommTx Tx { get; set; }

            // rx_autoresp
            // tx_autosend
        }

        public class CommTx
        {
            [JsonPropertyName("frames")]
            public IList<CommTxFrame> Frames { get; set; }
        }

        public class CommTxFrame
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("fields")]
            public IList<CommTxField> Fields { get; set; }

            [JsonPropertyName("backup_buffer_size")]
            public int BackupBufferSize { get; set; } = 0;

            [JsonPropertyName("backup_buffers")]
            public IList<CommTxBackupBuffer> BackupBuffers { get; set; }
        }

        public class CommTxField
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("multi_name")]
            public IList<CommTxFieldMultiName> MultiNames { get; set; }

            [JsonPropertyName("bit_size")]
            public int BitSize { get; set; } = 0;

            [JsonPropertyName("value")]
            public UInt64 Value { get; set; } = 0;

            [JsonPropertyName("type")]
            public string Type { get; set; } = string.Empty;

            [JsonPropertyName("unit")]
            public CommTxFieldUnit Unit { get; set; }

            [JsonPropertyName("dict")]
            public IList<CommTxFieldDict> Dict { get; set; }

            [JsonPropertyName("time")]
            public CommTxFieldTime Time { get; set; }

            [JsonPropertyName("checksum")]
            public CommTxFieldChecksum Checksum { get; set; }
        }

        public class CommTxFieldMultiName
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("bit_size")]
            public int BitSize { get; set; } = 0;
        }

        public class CommTxFieldUnit
        {
            [JsonPropertyName("unit")]
            public string Unit { get; set; } = string.Empty;

            [JsonPropertyName("lsb")]
            public double Lsb { get; set; } = 0;

            [JsonPropertyName("disp_max")]
            public double DispMax { get; set; } = 0;

            [JsonPropertyName("disp_min")]
            public double DispMin { get; set; } = 0;

            [JsonPropertyName("value_min")]
            public UInt64 ValueMin { get; set; } = 0;

            [JsonPropertyName("format")]
            public string Format { get; set; } = string.Empty;
        }

        public class CommTxFieldDict
        {
            [JsonPropertyName("value")]
            public UInt64 Value { get; set; } = 0;

            [JsonPropertyName("disp")]
            public string Disp { get; set; } = string.Empty;
        }

        public class CommTxFieldTime
        {
            [JsonPropertyName("elapse")]
            public double Elapse { get; set; } = 0;

            [JsonPropertyName("begin")]
            public string Begin { get; set; } = string.Empty;

            [JsonPropertyName("end")]
            public string End { get; set; } = string.Empty;

            [JsonPropertyName("value_min")]
            public UInt64 ValueMin { get; set; } = 0;
        }

        public class CommTxFieldChecksum
        {
            [JsonPropertyName("begin")]
            public int Begin { get; set; } = 0;

            [JsonPropertyName("end")]
            public int End { get; set; } = 0;

            [JsonPropertyName("method")]
            public string Method { get; set; } = string.Empty;
        }

        public class CommTxBackupBuffer
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
            
            [JsonPropertyName("value")]
            public IList<UInt64> Values { get; set; }
        }

    }
}
