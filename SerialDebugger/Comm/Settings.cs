using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Threading.Tasks;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace SerialDebugger.Comm
{
    using Logger = SerialDebugger.Log.Log;

    static class Settings
    {
        public class CommInfo
        {
            public string FilePath { get; set; }
            public string Name { get; set; }
            public ReactiveCollection<TxFrame> Tx { get; set; }
            // rx_autoresp
            // tx_autosend
        }

        static public ReactiveCollection<CommInfo> GetComm()
        {
            var list = new ReactiveCollection<CommInfo>();
            // デフォルトパス
            string rootPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string CommSettingPath = rootPath + @"\Comm";
            // 設定ファイルチェック
            if (Directory.Exists(CommSettingPath))
            {
                // ディレクトリ内ファイル取得
                var CommSettingFiles = Directory.GetFiles(CommSettingPath);
                //
                foreach (var file in CommSettingFiles)
                {
                    // 
                    var info = new CommInfo
                    {
                        FilePath = file
                    };
                    var result = LoadSettingFile(file, info);
                    if (result)
                    {
                        list.Add(info);
                    }
                }
            }
            return list;
        }

        static private bool LoadSettingFile(string path, CommInfo info)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                //Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                //NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };
            //
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // jsonファイルパース
                Task<JsonComm> json;
                try
                {
                    // 呼び出し元でWait()している。ConfigureAwait(false)無しにawaitするとデッドロックで死ぬ。
                    json = JsonSerializer.DeserializeAsync<JsonComm>(stream, options).AsTask();
                    json.Wait();
                }
                catch (Exception e)
                {
                    Logger.Add($"json解析エラー: {e.InnerException.Message} : in file {path}\n");
                    return false;
                }
                // json読み込み
                return MakeSetting(json.Result, info);
            }
        }

        static private bool MakeSetting(JsonComm comm, CommInfo info)
        {
            try
            {
                info.Name = comm.Name;
                // Tx
                info.Tx = MakeTxFrameCollection(comm.Tx);
                // rx_autoresp
                // tx_autosend
            }
            catch (Exception e)
            {
                Logger.Add($"json設定エラー: {e.Message} : in file {info.FilePath}\n");
                return false;
            }

            if (info.Name is null)
            {
                return false;
            }
            if (info.Tx is null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// JsonCommからReactiveCollection&lt;Comm.TxFrame&gt;を作成する
        /// </summary>
        /// <param name="tx"></param>
        static private ReactiveCollection<TxFrame> MakeTxFrameCollection(JsonTx tx)
        {
            var colle = new ReactiveCollection<TxFrame>();
            // TxFrameコレクション作成
            foreach (var frame in tx.Frames)
            {
                colle.Add(MakeTxFrame(frame));
            }
            // 正常終了したら登録
            return colle;
        }

        /// <summary>
        /// JsonTxFrameからTxFrameを作成する
        /// </summary>
        /// <param name="frame"></param>
        static private TxFrame MakeTxFrame(JsonTxFrame frame)
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

        static private TxField MakeTxField(JsonTxField field)
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

        static private TxField MakeTxFieldChecksum(JsonTxField field)
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
            // TxField生成
            return new TxField(field.Name, field.BitSize, field.Checksum.Begin, field.Checksum.End);
        }

        static private TxField MakeTxFieldDict(JsonTxField field)
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

        static private TxField MakeTxFieldUnit(JsonTxField field)
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

        static private TxField MakeTxFieldImpl(JsonTxField field, TxField.SelectModeType type, TxField.Selecter selecter)
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
    }
    
    public class JsonComm
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // 送信設定
        [JsonPropertyName("tx")]
        public JsonTx Tx { get; set; }

        // rx_autoresp
        // tx_autosend
    }

    public class JsonTx
    {
        [JsonPropertyName("frames")]
        public IList<JsonTxFrame> Frames { get; set; }
    }

    public class JsonTxFrame
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("buffer_size")]
        public int BufferSize { get; set; } = 0;

        [JsonPropertyName("fields")]
        public IList<JsonTxField> Fields { get; set; }
    }

    public class JsonTxField
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("multi_name")]
        public IList<JsonTxFieldMultiName> MultiNames { get; set; }

        [JsonPropertyName("bit_size")]
        public int BitSize { get; set; } = 0;

        [JsonPropertyName("value")]
        public UInt64 Value { get; set; } = 0;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("unit")]
        public JsonTxFieldUnit Unit { get; set; }

        [JsonPropertyName("dict")]
        public IList<JsonTxFieldDict> Dict { get; set; }

        [JsonPropertyName("checksum")]
        public JsonTxFieldChecksum Checksum { get; set; }
    }

    public class JsonTxFieldMultiName
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("bit_size")]
        public int BitSize { get; set; } = 0;
    }

    public class JsonTxFieldUnit
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

    public class JsonTxFieldDict
    {
        [JsonPropertyName("value")]
        public UInt64 Value { get; set; } = 0;

        [JsonPropertyName("disp")]
        public string Disp { get; set; } = string.Empty;
    }

    public class JsonTxFieldChecksum
    {
        [JsonPropertyName("begin")]
        public int Begin { get; set; } = 0;

        [JsonPropertyName("end")]
        public int End { get; set; } = 0;
    }
}
