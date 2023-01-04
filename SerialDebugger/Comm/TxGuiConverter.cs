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
    internal class TxGuiValueColConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var temp = (UInt64)value;
            // 16進数表示
            return $"{temp:X2}h";
            /*
            var field = (TxField)value;
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
    internal class TxGuiTxBufferColConverter : IValueConverter
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
    internal class TxGuiBitColBgConverter : IValueConverter
    {
        private readonly UInt64 mask;

        public TxGuiBitColBgConverter(UInt64 m)
        {
            mask = m;
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = (UInt64)value;
            var field = (TxField)parameter;
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
    internal class TxGuiEditConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var temp = (UInt64)value;
            // 16進数表示
            return $"{temp:X}";
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            UInt64 temp = Convert.ToUInt64((string)value, 16);
            var field = (TxField)parameter;
            if ((field.Min <= temp) && (temp <= field.Max))
            {
                return temp;
            }
            else
            {
                // 範囲外は読み捨て
                return DependencyProperty.UnsetValue;
            }
        }
    }

}
