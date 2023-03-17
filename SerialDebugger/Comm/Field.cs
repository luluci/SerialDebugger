using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reactive.Disposables;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Windows;
using System.Windows.Controls;

namespace SerialDebugger.Comm
{
    using Utility;

    public class Field : BindableBase, IDisposable
    {
        // 
        public int Id { get; }
        public string Name { get; }
        public int BitSize { get; }
        // テキストボックス表示基数
        public int InputBase { get; }
        // Field設定初期値
        public Int64 InitValue { get; }
        /// <summary>
        /// 1バイト境界からのビット位置.
        /// Nバイト目のMビット目を示す.
        /// </summary>
        internal int BitPos { get; set; }
        /// <summary>
        /// TxFrame内の先頭からのバイト位置
        /// Nバイト目を示す.
        /// </summary>
        internal int BytePos { get; set; } = 0;
        /// <summary>
        /// BytePosからBytePos+Nバイト目まで使うことを示す
        /// </summary>
        internal int ByteSize { get; set; } = 0;

        internal bool IsByteDisp { get; set; } = false;

        // true: big-endian, false: little-endian
        /// <summary>
        /// エンディアン反転フラグ
        /// little-endian環境でtrueのときはbig-endianに変換する
        /// </summary>
        public bool IsReverseEndian { get; }

        public Int64 Max { get; }
        public Int64 Min { get; }
        public Int64 Mask { get; }
        public Int64 InvMask { get; }
        public int HexSize { get; }
        public string HexFormat { get; }

        // チェックサム用プロパティ
        public enum ChecksumMethod
        {
            None,       // 総和
            cmpl_2,     // 2の補数
            cmpl_1,     // 1の補数
        }
        public class ChecksumNode
        {
            public string Name { get; set; }
            public int BitSize { get; set; }
            public int Begin { get; set; }
            public int End { get; set; }
            public ChecksumMethod Method { get; set; }
        }
        public ChecksumNode Checksum { get; set; }
        public bool IsChecksum { get; set; } = false;

        public class InnerField
        {
            public string Name { get; set; }
            public int BitSize { get; set; }

            public InnerField(string name, int bitsize)
            {
                Name = name;
                BitSize = bitsize;
            }
        }
        internal List<InnerField> InnerFields { get; }

        //
        public enum InputModeType
        {
            Fix,        // 固定値:変更不可
            Edit,       // 直接入力
            Unit,       // 単位指定
            Dict,       // 辞書指定
            Time,       // 時間(HH:MM)表現
            Script,     // スクリプト生成
            Checksum,   // チェックサム
            Char,       // 文字入力
        };
        public InputModeType InputType { get; private set; }

        public class Selecter
        {
            public InputModeType Type { get; }
            // Dict表現要素
            public (Int64, string)[] Dict { get; }
            // Unit表現要素
            public string Unit { get; }
            public double Lsb { get; }
            public double DispMax { get; }
            public double DispMin { get; }
            public Int64 ValueMin { get; }
            public string Format { get; }
            // Time表現要素
            public double Elapse { get; }
            public DateTime TimeBegin { get; }
            public DateTime TimeEnd { get; }
            // Script表現要素
            public string Mode { get; }
            public int Count { get; }
            public string Script { get; }
            // Char表現要素
            public int CharId { get; }

            public Selecter((Int64, string)[] dict)
            {
                Type = InputModeType.Dict;
                Dict = dict;
            }

            public Selecter(string unit, double lsb, double disp_max, double disp_min, Int64 value_min, string format)
            {
                Type = InputModeType.Unit;
                Unit = unit;
                Lsb = lsb;
                DispMax = disp_max;
                DispMin = disp_min;
                ValueMin = value_min;
                Format = format;
            }

            public Selecter(double elapse, string begin, string end, Int64 value_min)
            {
                Type = InputModeType.Time;
                Elapse = elapse;
                TimeBegin = DateTime.Parse(begin);
                TimeEnd = DateTime.Parse(end);
                ValueMin = value_min;
            }

            public Selecter(string mode, int count, string script)
            {
                Type = InputModeType.Script;
                Mode = mode;
                Count = count;
                Script = script;
            }

            public Selecter(int char_id)
            {
                Type = InputModeType.Char;
                CharId = char_id;
            }
        }
        public static Selecter MakeSelecterDict((Int64, string)[] dict)
        {
            return new Selecter(dict);
        }
        public static Selecter MakeSelecterUnit(string unit, double lsb, double disp_max, double disp_min, Int64 value_min, string format)
        {
            return new Selecter(unit, lsb, disp_max, disp_min, value_min, format);
        }
        public static Selecter MakeSelecterTime(double elapse, string begin, string end, Int64 value_min)
        {
            return new Selecter(elapse, begin, end, value_min);
        }
        public static Selecter MakeSelecterScript(string mode, int count, string script)
        {
            return new Selecter(mode, count, script);
        }
        public static Selecter MakeSelecterChar(int char_id)
        {
            return new Selecter(char_id);
        }

        public Selecter selecter;

        public class Select
        {
            public Int64 Value { get; private set; }
            public string Disp { get; private set; }

            public Select(Int64 value, string disp)
            {
                Disp = disp;
                Value = value;
            }
        }
        public ReactiveCollection<Select> Selects { get; private set; }
        public Int64 SelectsValueMax = 0;
        public Int64 SelectsValueMin = 0;
        public Dictionary<Int64, int> SelectsValueCheckTable;
        public int InitSelectIndex { get; private set; } = -1;

        // DragDrop
        public ReactiveCommand OnMouseDown { get; set; }
        public Settings.Output.DragDropInfo DragDropInfo { get; set; }

        public enum ChangeStates
        {
            Fixed,      // 確定済み
            Changed,    // 変更あり,未確定
            Updating,   // 変更あり,送信バッファ反映中
        }

        /// <summary>
        /// チェックサムノード用コンストラクタ
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bitsize"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="selecter"></param>
        public Field(int id, bool endian, Settings.Output.DragDropInfo dd, ChecksumNode node)
            : this(id, node.Name, new InnerField[] { new InnerField(node.Name, node.BitSize) }, 0, 16, endian, dd, InputModeType.Checksum, null)
        {
            Checksum = node;
            IsChecksum = true;
        }
        
        public Field(int id, string name, InnerField[] innerFields, Int64 value, int input_base, bool endian, Settings.Output.DragDropInfo dd, InputModeType type = InputModeType.Fix, Selecter selecter = null)
        {
            this.selecter = selecter;
            Id = id;
            Name = name;
            InputBase = input_base;
            IsReverseEndian = endian;
            BitSize = 0;
            foreach (var inner in innerFields)
            {
                BitSize += inner.BitSize;
            }
            // BitSizeチェック
            if (BitSize > 32)
            {
                throw new Exception("32bit以上は指定できません");
            }
            // Endianチェック
            // エンディアン反転時はバイト単位(8bitの倍数)でないと計算できない
            if (IsReverseEndian)
            {
                if (BitSize % 8 != 0)
                {
                    throw new Exception("big-endian指定時はbit-sizeをバイト単位(8bitの倍数)にしてください");
                }
            }
            // DragDrop
            DragDropInfo = dd;
            //
            InnerFields = new List<InnerField>(innerFields);
            // (Min,Max)
            Max = ((Int64)1 << BitSize) - 1;
            Min = ((Int64)1 << (BitSize - 1)) * -1;
            Mask = Max;
            InvMask = ~Mask;
            HexSize = ((BitSize + 7) / 8) * 2;
            HexFormat = $"X{HexSize}";
            //
            value = LimitValue(value);
            InitValue = value;
            //
            Selects = new ReactiveCollection<Select>();
            Selects.AddTo(Disposables);

            //
            OnMouseDown = new ReactiveCommand();
            OnMouseDown.AddTo(Disposables);
            //
            InputType = type;
        }

        public async Task InitAsync()
        {
            await MakeSelectModeAsync(selecter);
        }

        public Int64 LimitValue(Int64 value)
        {
            if (value > Max)
            {
                return Max;
            }
            else if (value < Min)
            {
                return Min;
            }
            else
            {
                return value;
            }
        }

        public int GetSelectsIndex(Int64 value)
        {
            int index = -1;
            switch (InputType)
            {
                case Field.InputModeType.Dict:
                case Field.InputModeType.Script:
                    if (SelectsValueCheckTable.TryGetValue(value, out index))
                    {
                        //SelectIndexSelects.Value = index;
                    }
                    break;
                case Field.InputModeType.Unit:
                case Field.InputModeType.Time:
                    if (SelectsValueMin <= value && value <= SelectsValueMax)
                    {
                        index = (int)(value - SelectsValueMin);
                    }
                    break;
                case Field.InputModeType.Char:
                case Field.InputModeType.Edit:
                case Field.InputModeType.Fix:
                case Field.InputModeType.Checksum:
                default:
                    break;
            }
            return index;
        }

        public Int64 ReverseEndian(Int64 value)
        {
            Int64 result = 0;
            int bit_rest = BitSize;
            while (bit_rest > 0)
            {
                // LSBに1byte詰める領域を作成
                result <<= 8;
                // 1byte詰める
                result |= (Int64)((byte)(value & 0xFF));
                // 使用した1byteを捨てる
                value >>= 8;
                bit_rest -= 8;
            }
            return result;
        }
        
        /// <summary>
        /// 指定したパラメータで表示名を作成する。
        /// BackupBufferで再利用。
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string MakeDisp(int index, Int64 value)
        {
            switch (InputType)
            {
                case Field.InputModeType.Dict:
                case Field.InputModeType.Unit:
                case Field.InputModeType.Time:
                case Field.InputModeType.Script:
                    return Selects[index].Disp;
                case Field.InputModeType.Char:
                    return MakeDispChar(value);
                case Field.InputModeType.Edit:
                case Field.InputModeType.Fix:
                default:
                    return MakeDispNumber(value);
            }
        }

        public string MakeDispByValue(Int64 value)
        {
            switch (InputType)
            {
                case Field.InputModeType.Dict:
                case Field.InputModeType.Unit:
                case Field.InputModeType.Time:
                case Field.InputModeType.Script:
                    var index = GetSelectsIndex(value);
                    if (index != -1)
                    {
                        return Selects[index].Disp;
                    }
                    else
                    {
                        return MakeDispNumber(value);
                    }

                case Field.InputModeType.Char:
                    return MakeDispChar(value);
                case Field.InputModeType.Edit:
                case Field.InputModeType.Fix:
                default:
                    return MakeDispNumber(value);
            }
        }

        public string MakeDispNumber(Int64 value)
        {
            switch (InputBase)
            {
                case 10:
                    return value.ToString("D");

                case 16:
                default:
                    return $"0x{MakeDispHex(value)}";
            }
        }

        public string MakeDispHex(Int64 value)
        {
            var result = value.ToString(HexFormat);
            var right = result.Length - HexSize;
            if (right > 0)
            {
                result = result.Substring(right, HexSize);
            }
            return result;
        }

        public string MakeDispChar(Int64 value)
        {
            var ch = (char)(value & 0xFF);
            return $"{ch}";
        }

        private async Task MakeSelectModeAsync(Selecter selecter)
        {
            int index;

            // 特殊SelecterのためにSelecterの指定を優先する
            var inputtype = InputType;
            if (!(selecter is null))
            {
                inputtype = selecter.Type;
            }
            switch (inputtype)
            {
                case InputModeType.Unit:
                    index = MakeSelectModeUnit(selecter);
                    InitSelectIndex = index;
                    break;
                case InputModeType.Dict:
                    index = MakeSelectModeDict(selecter);
                    InitSelectIndex = index;
                    break;
                case InputModeType.Time:
                    index = MakeSelectModeTime(selecter);
                    InitSelectIndex = index;
                    break;
                case InputModeType.Script:
                    index = await MakeSelectModeScriptAsync(selecter);
                    InitSelectIndex = index;
                    break;
                case InputModeType.Char:
                case InputModeType.Edit:
                case InputModeType.Fix:
                default:
                    break;
            }
        }

        private int MakeSelectModeUnit(Selecter selecter)
        {
            int selectIndex = 0;
            int index = 0;
            Int64 value = selecter.ValueMin;
            SelectsValueMin = selecter.ValueMin;

            if (selecter.Lsb > 0)
            {
                double temp = selecter.DispMin;
                while (temp <= selecter.DispMax)
                {
                    MakeSelectModeUnitImpl(selecter, value, temp);
                    // SelectIndex
                    if (value == InitValue)
                    {
                        selectIndex = index;
                    }
                    //
                    temp += selecter.Lsb;
                    index++;
                    value++;
                    // BitSize定義の上限到達で終了
                    if (value > Max)
                    {
                        break;
                    }
                }
            }
            else if (selecter.Lsb < 0)
            {
                double temp = selecter.DispMax;
                while (temp >= selecter.DispMin)
                {
                    MakeSelectModeUnitImpl(selecter, value, temp);
                    // SelectIndex
                    if (value == InitValue)
                    {
                        selectIndex = index;
                    }
                    //
                    temp += selecter.Lsb;
                    index++;
                    value++;
                    // BitSize定義の上限到達で終了
                    if (value > Max)
                    {
                        break;
                    }
                }
            }
            else
            {
                // 
                throw new Exception($"Field({Name}): Selecter=Unit: LSBにゼロは設定不可です。");
            }

            SelectsValueMax = value - 1;

            return selectIndex;
        }

        private void MakeSelectModeUnitImpl(Selecter selecter, Int64 value, double disp_value)
        {
            string disp;
            try
            {
                disp = disp_value.ToString(selecter.Format) + selecter.Unit;
            }
            catch (Exception)
            {
                disp = ((Int64)disp_value).ToString(selecter.Format) + selecter.Unit;
            }
            Selects.Add(new Select(value, disp));
        }

        private int MakeSelectModeDict(Selecter selecter)
        {
            SelectsValueCheckTable = new Dictionary<Int64, int>();
            int selectIndex = 0;
            int index = 0;
            foreach (var item in selecter.Dict)
            {
                // BitSize定義の範囲チェック
                if (item.Item1 <= Max)
                {
                    Selects.Add(new Select(item.Item1, item.Item2));
                    //
                    if (item.Item1 == InitValue)
                    {
                        selectIndex = index;
                    }
                    //
                    try
                    {
                        SelectsValueCheckTable.Add(item.Item1, index);
                    }
                    catch (Exception)
                    {
                        throw new Exception($"dict指定のvalueに重複があります: (value:{item.Item1}, disp:{item.Item2})");
                    }
                    //
                    index++;
                }
            }

            return selectIndex;
        }

        private int MakeSelectModeTime(Selecter selecter)
        {
            int selectIndex = 0;
            int index = 0;
            Int64 value = selecter.ValueMin;
            SelectsValueMin = selecter.ValueMin;
            double elapse = selecter.Elapse;
            var dt = selecter.TimeBegin;
            var end_dt = selecter.TimeEnd;

            while (end_dt.CompareTo(dt) >= 0)
            {
                var disp = dt.ToString("HH:mm");
                Selects.Add(new Select(value, disp));
                // SelectIndex
                if (value == InitValue)
                {
                    selectIndex = index;
                }
                //
                dt = dt.AddMinutes(elapse);
                index++;
                value++;
                // BitSize定義の上限到達で終了
                if (value > Max)
                {
                    break;
                }
            }
            SelectsValueMax = value - 1;

            return selectIndex;
        }

        private async Task<int> MakeSelectModeScriptAsync(Selecter selecter)
        {
            SelectsValueCheckTable = new Dictionary<Int64, int>();
            return await Script.Interpreter.Engine.MakeFieldSelecter(this);
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
