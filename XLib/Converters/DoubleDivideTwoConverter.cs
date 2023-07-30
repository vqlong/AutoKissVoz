using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XLib.Converters
{
    public class DoubleDivideTwoConverter : System.Windows.Data.IValueConverter
    {
        public static DoubleDivideTwoConverter Default { get; } = new DoubleDivideTwoConverter();
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double doubleValue) return doubleValue / 2;
            return System.Windows.DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
