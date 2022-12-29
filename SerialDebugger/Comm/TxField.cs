﻿using System;
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
        internal int BytePos { get; set; }

        internal bool IsByteDisp { get; set; } = false;

        public UInt64 Max { get; }
        public UInt64 Min { get; }
        public UInt64 Mask { get; }

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
            //
            value = value & Mask;
            Value = new ReactivePropertySlim<UInt64>(value);
            Value.AddTo(Disposables);
            //
            SelectType = type;
            MakeSelectMode(selecter);
        }


        private void MakeSelectMode(Selecter selecter)
        {
            switch (SelectType)
            {
                case SelectModeType.Unit:
                    Selects = new ReactiveCollection<Select>();
                    MakeSelectModeUnit(selecter);
                    break;
                case SelectModeType.Dict:
                    Selects = new ReactiveCollection<Select>();
                    MakeSelectModeDict(selecter);
                    break;
                case SelectModeType.Edit:
                case SelectModeType.Fix:
                default:
                    break;
            }
        }

        private void MakeSelectModeUnit(Selecter selecter)
        {
            double temp = selecter.DispMin;
            UInt64 value = selecter.ValueMin;
            while (temp <= selecter.DispMax)
            {
                string disp = temp.ToString(selecter.Format) + selecter.Unit;
                Selects.Add(new Select(value, disp));
                //
                temp += selecter.Lsb;
                value++;
                // BitSize定義の上限到達で終了
                if (value >= Max)
                {
                    break;
                }
            }
        }

        private void MakeSelectModeDict(Selecter selecter)
        {
            foreach (var item in selecter.Dict)
            {
                // BitSize定義の範囲チェック
                if (item.Item1 < Max)
                {
                    Selects.Add(new Select(item.Item1, item.Item2));
                }
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
