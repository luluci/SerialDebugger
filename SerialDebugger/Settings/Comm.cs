
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
            if (json.Tx is null)
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
            var f = new TxFrame(frame.Name, frame.BufferSize);
            foreach (var field in frame.Fields)
            {
                f.Fields.Add(MakeTxField(field));
            }
            f.Build();

            return f;
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
                case "Edit":
                    return MakeTxFieldImpl(field, TxField.SelectModeType.Edit, null);
                case "Fix":
                default:
                    return MakeTxFieldImpl(field, TxField.SelectModeType.Fix, null);
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
            // Checksumチェック
            if (field.Dict is null)
            {
                throw new Exception("type:DictではDictオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = new (UInt64, string)[field.Dict.Count];
            int i = 0;
            foreach (var pair in field.Dict)
            {
                selecter[i] = (pair.Value, pair.Disp);
                i++;
            }
            // TxField生成
            return MakeTxFieldImpl(field, TxField.SelectModeType.Dict, new TxField.Selecter(selecter));
        }

        private TxField MakeTxFieldUnit(Json.CommTxField field)
        {
            var unit = field.Unit;
            // Checksumチェック
            if (unit is null)
            {
                throw new Exception("type:UnitではUnitオブジェクトを指定してください。");
            }
            // Selecter作成
            var selecter = new TxField.Selecter(unit.Unit, unit.Lsb, unit.DispMax, unit.DispMin, unit.ValueMin, unit.Format);
            // TxField生成
            return MakeTxFieldImpl(field, TxField.SelectModeType.Unit, selecter);
        }

        private TxField MakeTxFieldImpl(Json.CommTxField field, TxField.SelectModeType type, TxField.Selecter selecter)
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

            [JsonPropertyName("buffer_size")]
            public int BufferSize { get; set; } = 0;

            [JsonPropertyName("fields")]
            public IList<CommTxField> Fields { get; set; }
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

        public class CommTxFieldChecksum
        {
            [JsonPropertyName("begin")]
            public int Begin { get; set; } = 0;

            [JsonPropertyName("end")]
            public int End { get; set; } = 0;

            [JsonPropertyName("method")]
            public string Method { get; set; } = string.Empty;
        }

    }
}
