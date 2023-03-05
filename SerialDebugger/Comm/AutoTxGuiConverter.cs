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
    internal class AutoTxGuiActiveActionBGColorConverter : IValueConverter
    {
        //public static SolidColorBrush ChangedColor = new SolidColorBrush(Color.FromArgb(0xFF, 0xEE, 0x44, 0x44));
        //public static SolidColorBrush FixedColor = SystemColors.ControlBrush;  // ボタン表面色
        public static SolidColorBrush Active = Brushes.Aquamarine;
        public static SolidColorBrush Negative = SystemColors.ControlBrush;

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var active = (bool)value;
            if (active)
            {
                return Active;
            }
            else
            {
                return Negative;
            }
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    internal class AutoTxGuiActiveJobBGColorConverter : IValueConverter
    {
        public static SolidColorBrush Active = new SolidColorBrush(Color.FromArgb(0xFF, 40, 60, 255));
        public static SolidColorBrush Negative = new SolidColorBrush(Color.FromArgb(0xFF, 11, 40, 75));

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var active = (bool)value;
            if (active)
            {
                return Active;
            }
            else
            {
                return Negative;
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
    internal class AutoTxGuiWaitEditConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var temp = (int)value;
            // 10進数表示
            return $"{temp}";
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var temp = Convert.ToInt32((string)value, 10);
                if (temp > 0)
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

}
