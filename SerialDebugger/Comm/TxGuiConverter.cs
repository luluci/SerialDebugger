using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SerialDebugger.Comm
{
    class TxGuiConverter
    {
    }

    /// <summary>
    /// Value列に表示する文字列を作成する
    /// </summary>
    internal class GuiValueColConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var temp = (Int64)value;
            var field = (Field)parameter;
            // 16進数表示
            return $"{field.MakeDispHex(temp)}h";
            /*
            var field = (Field)value;
            // 16進数表示
            return $"{field.Value.Value:X}h";
            */
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 送信バイトシーケンス列に表示する文字列を作成する
    /// </summary>
    internal class GuiTxBufferColConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var temp = (byte)value;
            // 16進数表示
            return $"{temp:X2}h";
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Bit列背景色を決定する
    /// </summary>
    internal class GuiBitColBgConverter : IValueConverter
    {
        private readonly Int64 mask;

        public GuiBitColBgConverter(Int64 m)
        {
            mask = m;
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (Int64)value;
            var field = (Field)parameter;
            // Endianチェック
            if (field.IsReverseEndian)
            {
                val = field.ReverseEndian(val);
            }
            if ((val & mask) != 0)
            {
                return Brushes.Salmon;
            }
            else
            {
                return SystemColors.ControlLightLightBrush;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Select列:テキストボックスに表示する文字列を作成する
    /// </summary>
    internal class GuiHexEditConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var temp = (Int64)value;
            var field = (Field)parameter;
            // 16進数表示
            return $"{field.MakeDispHex(temp)}";
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var temp = Convert.ToInt64((string)value, 16);
                var field = parameter as Field;
                if ((field.Min <= temp) && (temp <= field.Max))
                {
                    return temp;
                }
            }
            catch
            {
                // Convert失敗
            }
            // 範囲外は読み捨て
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Select列:テキストボックスに表示する文字列を作成する
    /// </summary>
    internal class GuiDecEditConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var temp = (Int64)value;
            // 10進数表示
            return $"{temp}";
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var temp = Convert.ToInt64((string)value, 10);
                var field = parameter as Field;
                if ((field.Min <= temp) && (temp <= field.Max))
                {
                    return temp;
                }
            }
            catch
            {
                // Convert失敗
            }
            // 範囲外は読み捨て
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>
    /// Select列:テキストボックスに表示する文字列を作成する
    /// </summary>
    internal class GuiCharEditConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var temp = (char)((Int64)value & 0xFF);
            var field = (Field)parameter;
            // 16進数表示
            return $"{temp}";
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var temp = (string)value;
            var field = parameter as Field;
            return temp[0];
        }
    }

    /// <summary>
    /// 送信バイトシーケンス列に表示する文字列を作成する
    /// </summary>
    internal class GuiTxSendFixNameConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //var temp = ((ReactivePropertySlim<Field.ChangeStates>)value).Value;
            var temp = (Field.ChangeStates)value;
            switch (temp)
            {
                case Field.ChangeStates.Changed:
                    return "確定";
                default:
                    return "送信";
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 変更Fieldあり時の背景色
    /// </summary>
    internal class GuiTxSendFixBGColorConverter : IValueConverter
    {
        public static SolidColorBrush ChangedColor = new SolidColorBrush(Color.FromArgb(0xFF, 0xEE, 0x44, 0x44));
        //public static SolidColorBrush FixedColor = SystemColors.ControlBrush;  // ボタン表面色
        public static SolidColorBrush FixedColor = Brushes.Black;

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //var temp = ((ReactivePropertySlim<Field.ChangeStates>)value).Value;
            var temp = (Field.ChangeStates)value;
            switch (temp)
            {
                case Field.ChangeStates.Changed:
                    return ChangedColor;
                default:
                    return FixedColor;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class GuiCharMultiBindConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return (values[0], values[1], values[2]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
