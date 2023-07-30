using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace XLib.Converters
{
    //Nghịch đảo 1 giá trị double và cộng thêm phần bù (parameter)
    public class ValueInverseConverter : IValueConverter
    {
        public static ValueInverseConverter Default { get; } = new ValueInverseConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue) return -1 * doubleValue + (double.TryParse(parameter.ToString(), out double result) ? result : 0);
            if (value is bool boolValue) return !boolValue;
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
