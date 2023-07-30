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
    public class ErrorCornerRadiusConverter : IValueConverter
    {
        public static ErrorCornerRadiusConverter Default { get; } = new ErrorCornerRadiusConverter();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CornerRadius radius && radius.BottomLeft == radius.BottomRight && radius.BottomRight == radius.TopRight && radius.TopRight == radius.TopLeft)
            {
                var r = radius.TopRight - 1.5;
                if (r > 0) return new CornerRadius(r);
                else return radius;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
