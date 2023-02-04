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

    class Field : BindableBase, IDisposable
    {
        // 
        public int Id { get; }
        public string Name { get; }
        public int BitSize { get; }
        // テキストボックス表示基数
        public int InputBase { get; }
        //
        public ReactivePropertySlim<UInt64> Value { get; set; }
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

        public UInt64 Max { get; }
        public UInt64 Min { get; }
        public UInt64 Mask { get; }
        public UInt64 InvMask { get; }

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

            // 特殊Selecter
            Refer,      // 参照:既存Fieldの内容を流用する。値は個別に保持する
        };
        public InputModeType InputType { get; private set; }

        public class Selecter
        {
            public InputModeType Type { get; }
            // Dict表現要素
            public (UInt64, string)[] Dict { get; }
            // Unit表現要素
            public string Unit { get; }
            public double Lsb { get; }
            public double DispMax { get; }
            public double DispMin { get; }
            public UInt64 ValueMin { get; }
            public string Format { get; }
            // Time表現要素
            public double Elapse { get; }
            public DateTime TimeBegin { get; }
            public DateTime TimeEnd { get; }
            // Script表現要素
            public string Mode { get; }
            public int Count { get; }
            public string Script { get; }
            // Refer表現要素
            public Field FieldRef { get; }

            public Selecter((UInt64, string)[] dict)
            {
                Type = InputModeType.Dict;
                Dict = dict;
            }

            public Selecter(string unit, double lsb, double disp_max, double disp_min, UInt64 value_min, string format)
            {
                Type = InputModeType.Unit;
                Unit = unit;
                Lsb = lsb;
                DispMax = disp_max;
                DispMin = disp_min;
                ValueMin = value_min;
                Format = format;
            }

            public Selecter(double elapse, string begin, string end, UInt64 value_min)
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

            public Selecter(Field field)
            {
                Type = InputModeType.Refer;
                FieldRef = field;
            }
        }
        public static Selecter MakeSelecterDict((UInt64, string)[] dict)
        {
            return new Selecter(dict);
        }
        public static Selecter MakeSelecterUnit(string unit, double lsb, double disp_max, double disp_min, UInt64 value_min, string format)
        {
            return new Selecter(unit, lsb, disp_max, disp_min, value_min, format);
        }
        public static Selecter MakeSelecterTime(double elapse, string begin, string end, UInt64 value_min)
        {
            return new Selecter(elapse, begin, end, value_min);
        }
        public static Selecter MakeSelecterScript(string mode, int count, string script)
        {
            return new Selecter(mode, count, script);
        }

        public Selecter selecter;

        public class Select
        {
            public UInt64 Value { get; private set; }
            public string Disp { get; private set; }

            public Select(UInt64 value, string disp)
            {
                Disp = disp;
                Value = value;
            }
        }
        public ReactiveCollection<Select> Selects { get; private set; }
        public ReactivePropertySlim<int> SelectIndexSelects { get; set; }
        public UInt64 SelectsValueMax = 0;
        public UInt64 SelectsValueMin = 0;
        public Dictionary<UInt64, int> SelectsValueCheckTable;
        public ReactiveCommand OnMouseDown { get; set; }

        public enum ChangeStates
        {
            Fixed,      // 確定済み
            Changed,    // 変更あり,未確定
            Updating,   // 変更あり,送信バッファ反映中
        }
        public ReactivePropertySlim<ChangeStates> ChangeState { get; set; }

        /// <summary>
        /// チェックサムノード用コンストラクタ
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bitsize"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="selecter"></param>
        public Field(int id, ChecksumNode node)
            : this(id, node.Name, new InnerField[] { new InnerField(node.Name, node.BitSize) }, 0, 16, InputModeType.Checksum, null)
        {
            Checksum = node;
            IsChecksum = true;
        }
        
        public Field(int id, string name, InnerField[] innerFields, UInt64 value, int input_base, InputModeType type = InputModeType.Fix, Selecter selecter = null)
        {
            this.selecter = selecter;
            Id = id;
            Name = name;
            InputBase = input_base;
            BitSize = 0;
            foreach (var inner in innerFields)
            {
                BitSize += inner.BitSize;
            }
            // BitSizeチェック
            if (BitSize > 64)
            {
                throw new Exception("64bit以上は指定できません");
            }
            //
            InnerFields = new List<InnerField>(innerFields);
            // (Min,Max)
            if (BitSize == 64)
            {
                Max = UInt64.MaxValue;
                Min = UInt64.MinValue;
            }
            else
            {
                Max = ((UInt64)1 << BitSize) - 1;
                Min = 0;
            }
            Mask = Max;
            InvMask = ~Mask;
            //
            value = value & Mask;
            Value = new ReactivePropertySlim<UInt64>(value, mode: ReactivePropertyMode.DistinctUntilChanged);
            Value
                .Subscribe(x =>
                {
                    // ComboBox入力更新
                    var index = GetSelectsIndex(x);
                    if (index != -1)
                    {
                        SelectIndexSelects.Value = index;
                    }
                    // 変更状態更新
                    ChangeState.Value = ChangeStates.Changed;
                })
                .AddTo(Disposables);
            //
            Selects = new ReactiveCollection<Select>();
            Selects.AddTo(Disposables);
            SelectIndexSelects = new ReactivePropertySlim<int>(0, mode:ReactivePropertyMode.DistinctUntilChanged);
            SelectIndexSelects
                .Subscribe((int index) =>
                {
                    if (index >= 0)
                    {
                        var select = Selects[index];
                        // Value側でもSelectIndexSelectsとの同期をとって値を変更する
                        // 値に変化がないとSubscribeは発火しない。
                        Value.Value = select.Value;
                    }
                })
                .AddTo(Disposables);
            //
            OnMouseDown = new ReactiveCommand();
            OnMouseDown.AddTo(Disposables);
            // 値変更を送信バッファに反映したかどうか管理する
            ChangeState = new ReactivePropertySlim<ChangeStates>(ChangeStates.Fixed);
            ChangeState.AddTo(Disposables);
            //
            InputType = type;
        }

        public async Task InitAsync()
        {
            await MakeSelectModeAsync(selecter);
        }

        /// <summary>
        /// 設定ファイルからの初期値設定
        /// </summary>
        /// <param name="value"></param>
        public void SetValue(UInt64 value)
        {
            // Value
            value = value & Mask;
            Value.Value = value;
            // SelectIndexSelects
            SelectIndexSelects.Value = GetSelectsIndex(value);
        }

        public int GetSelectsIndex(UInt64 value)
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
                case Field.InputModeType.Edit:
                case Field.InputModeType.Fix:
                case Field.InputModeType.Checksum:
                default:
                    break;
            }
            return index;
        }

        /// <summary>
        /// 自Fieldの表示名を取得する
        /// </summary>
        /// <returns></returns>
        public string GetDisp()
        {
            return MakeDisp(SelectIndexSelects.Value, Value.Value);
        }

        /// <summary>
        /// 指定したパラメータで表示名を作成する。
        /// BackupBufferで再利用。
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public string MakeDisp(int index, UInt64 value)
        {
            switch (InputType)
            {
                case Field.InputModeType.Dict:
                case Field.InputModeType.Unit:
                case Field.InputModeType.Time:
                case Field.InputModeType.Script:
                    return Selects[index].Disp;
                case Field.InputModeType.Edit:
                case Field.InputModeType.Fix:
                default:
                    return $"0x{value:X}";
            }
        }

        public string MakeDispByValue(UInt64 value)
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
                        return $"0x{value:X}";
                    }

                case Field.InputModeType.Edit:
                case Field.InputModeType.Fix:
                default:
                    return $"0x{value:X}";
            }
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
                    SelectIndexSelects.Value = index;
                    break;
                case InputModeType.Dict:
                    index = MakeSelectModeDict(selecter);
                    SelectIndexSelects.Value = index;
                    break;
                case InputModeType.Time:
                    index = MakeSelectModeTime(selecter);
                    SelectIndexSelects.Value = index;
                    break;
                case InputModeType.Script:
                    index = await MakeSelectModeScriptAsync(selecter);
                    SelectIndexSelects.Value = index;
                    break;
                case InputModeType.Refer:
                    MakeSelectModeRefer();
                    break;
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
            double temp = selecter.DispMin;
            UInt64 value = selecter.ValueMin;
            SelectsValueMin = selecter.ValueMin;
            while (temp <= selecter.DispMax)
            {
                string disp;
                try
                {
                    disp = temp.ToString(selecter.Format) + selecter.Unit;
                }
                catch (Exception)
                {
                    disp = ((Int64)temp).ToString(selecter.Format) + selecter.Unit;
                }
                Selects.Add(new Select(value, disp));
                // SelectIndex
                if (value == Value.Value)
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
            SelectsValueMax = value - 1;

            return selectIndex;
        }

        private int MakeSelectModeDict(Selecter selecter)
        {
            SelectsValueCheckTable = new Dictionary<ulong, int>();
            int selectIndex = 0;
            int index = 0;
            foreach (var item in selecter.Dict)
            {
                // BitSize定義の範囲チェック
                if (item.Item1 <= Max)
                {
                    Selects.Add(new Select(item.Item1, item.Item2));
                    //
                    if (item.Item1 == Value.Value)
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
            UInt64 value = selecter.ValueMin;
            SelectsValueMin = selecter.ValueMin;
            double elapse = selecter.Elapse;
            var dt = selecter.TimeBegin;
            var end_dt = selecter.TimeEnd;

            while (end_dt.CompareTo(dt) >= 0)
            {
                var disp = dt.ToString("HH:mm");
                Selects.Add(new Select(value, disp));
                // SelectIndex
                if (value == Value.Value)
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
            SelectsValueCheckTable = new Dictionary<ulong, int>();
            switch (selecter.Mode)
            {
                case "Exec":
                    return await MakeSelectModeScriptExecAsync(selecter);
                case "Call":
                    return await MakeSelectModeScriptCallAsync(selecter);
                default:
                    return -1;
            }
        }
        private async Task<int> MakeSelectModeScriptExecAsync(Selecter selecter)
        {
            int index = 0;
            int selectIndex = 0;

            // Script初期化
            await Script.Interpreter.Engine.EvalInit(selecter.Script);

            for (int i=0; i<selecter.Count; i++)
            {
                // Script評価
                var result = await Script.Interpreter.Engine.EvalExec(i);
                if (!(result.Item2 is null) && result.Item1 <= Max)
                {
                    Selects.Add(new Select(result.Item1, result.Item2));
                    // SelectIndex
                    if (result.Item1 == Value.Value)
                    {
                        selectIndex = index;
                    }
                    //
                    SelectsValueCheckTable.Add(result.Item1, index);
                    //
                    index++;
                }
            }

            return selectIndex;
        }
        private async Task<int> MakeSelectModeScriptCallAsync(Selecter selecter)
        {
            int index = 0;
            int selectIndex = 0;

            // Script初期化
            //await Script.Interpreter.Engine.EvalInit(selecter.Script);

            for (int i = 0; i < selecter.Count; i++)
            {
                // Script評価
                var result = await Script.Interpreter.Engine.Call($"{selecter.Script}({i})");
                if (!(result.Item2 is null) && result.Item1 <= Max)
                {
                    Selects.Add(new Select(result.Item1, result.Item2));
                    // SelectIndex
                    if (result.Item1 == Value.Value)
                    {
                        selectIndex = index;
                    }
                    //
                    SelectsValueCheckTable.Add(result.Item1, index);
                    //
                    index++;
                }
            }

            return selectIndex;
        }

        public void MakeSelectModeRefer()
        {
            // InputTypeは参照先Fieldと同じとするので更新しない
            // InputType = InputModeType.Refer;
            // 空のオブジェクトを持っているので解放しておく
            Selects.Dispose();
            // Selectsは参照を取得して流用する
            Selects = selecter.FieldRef.Selects;
            // SelectIndexは個別に持つ必要があるので値をコピー
            SelectIndexSelects.Value = selecter.FieldRef.SelectIndexSelects.Value;
            SelectsValueCheckTable = selecter.FieldRef.SelectsValueCheckTable;
            SelectsValueMax = selecter.FieldRef.SelectsValueMax;
            SelectsValueMin = selecter.FieldRef.SelectsValueMin;
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
