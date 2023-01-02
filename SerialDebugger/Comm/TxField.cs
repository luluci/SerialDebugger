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
    class TxField : BindableBase, IDisposable
    {
        // 
        public string Name { get; }
        public int BitSize { get; }
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
        public enum SelectModeType
        {
            Fix,        // 固定値:変更不可
            Edit,       // 直接入力
            Unit,       // 単位指定
            Dict,       // 辞書指定
            Checksum,   // チェックサム
        };
        public SelectModeType SelectType { get; }

        public class Selecter
        {
            public SelectModeType Type { get; }
            // Dict表現要素
            public (UInt64, string)[] Dict { get; }
            // Unit表現要素
            public string Unit { get; }
            public double Lsb { get; }
            public double DispMax { get; }
            public double DispMin { get; }
            public UInt64 ValueMin { get; }
            public string Format { get; }

            public Selecter((UInt64, string)[] dict)
            {
                Type = SelectModeType.Dict;
                Dict = dict;
            }

            public Selecter(string unit, double lsb, double disp_max, double disp_min, UInt64 value_min, string format)
            {
                Type = SelectModeType.Unit;
                Unit = unit;
                Lsb = lsb;
                DispMax = disp_max;
                DispMin = disp_min;
                ValueMin = value_min;
                Format = format;
            }
        }

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
        private UInt64 SelectsValueMax = 0;
        private UInt64 SelectsValueMin = 0;
        private Dictionary<UInt64, int> SelectsValueCheckTable;

        /// <summary>
        /// チェックサムノード用コンストラクタ
        /// </summary>
        /// <param name="name"></param>
        /// <param name="bitsize"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <param name="selecter"></param>
        public TxField(ChecksumNode node)
            : this(new InnerField[] { new InnerField(node.Name, node.BitSize) }, 0, SelectModeType.Checksum, null)
        {
            Checksum = node;
            IsChecksum = true;
        }

        public TxField(string name, int bitsize, UInt64 value = 0, SelectModeType type = SelectModeType.Fix, Selecter selecter = null)
            : this(new InnerField[]{ new InnerField(name, bitsize) }, value, type, selecter)
        {
        }

        public TxField(InnerField[] innerFields, UInt64 value = 0, SelectModeType type = SelectModeType.Fix, Selecter selecter = null)
        {
            Name = innerFields[0].Name;
            BitSize = 0;
            foreach (var inner in innerFields)
            {
                BitSize += inner.BitSize;
            }
            //
            InnerFields = new List<InnerField>(innerFields);
            // (Min,Max]
            Max = (UInt64)1 << BitSize;
            Min = 0;
            Mask = Max - 1;
            InvMask = ~Mask;
            //
            value = value & Mask;
            Value = new ReactivePropertySlim<UInt64>(value, mode: ReactivePropertyMode.DistinctUntilChanged);
            Value
                .Subscribe(x =>
                {
                    if (SelectType == SelectModeType.Dict)
                    {
                        if (SelectsValueCheckTable.TryGetValue(x, out int index))
                        {
                            SelectIndexSelects.Value = index;
                        }
                    }
                    else if(SelectType == SelectModeType.Unit)
                    {
                        if (SelectsValueMin <= x && x <= SelectsValueMax)
                        {
                            SelectIndexSelects.Value = (int)(x - SelectsValueMin);
                        }
                    }
                })
                .AddTo(Disposables);
            //
            Selects = new ReactiveCollection<Select>();
            Selects.AddTo(Disposables);
            SelectIndexSelects = new ReactivePropertySlim<int>(0, mode:ReactivePropertyMode.DistinctUntilChanged);
            SelectIndexSelects
                .Subscribe((int index) =>
                {
                    var select = Selects[index];
                    // Value側でもSelectIndexSelectsとの同期をとって値を変更する
                    // 値に変化がないとSubscribeは発火しない。
                    Value.Value = select.Value;
                })
                .AddTo(Disposables);
            //
            SelectType = type;
            MakeSelectMode(selecter);
        }


        private void MakeSelectMode(Selecter selecter)
        {
            int index;
            switch (SelectType)
            {
                case SelectModeType.Unit:
                    index = MakeSelectModeUnit(selecter);
                    SelectIndexSelects.Value = index;
                    break;
                case SelectModeType.Dict:
                    index = MakeSelectModeDict(selecter);
                    SelectIndexSelects.Value = index;
                    break;
                case SelectModeType.Edit:
                case SelectModeType.Fix:
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
                string disp = temp.ToString(selecter.Format) + selecter.Unit;
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
                if (value >= Max)
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
                if (item.Item1 < Max)
                {
                    Selects.Add(new Select(item.Item1, item.Item2));
                    //
                    if (item.Item1 == Value.Value)
                    {
                        selectIndex = index;
                    }
                    //
                    SelectsValueCheckTable.Add(item.Item1, index);
                    //
                    index++;
                }
            }

            return selectIndex;
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
