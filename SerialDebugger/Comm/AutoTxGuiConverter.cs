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
}
