using System;
using System.Linq;
using System.Windows.Data;

namespace ScreenSaver.Services.UI.Converters
{
    internal class BoolEqConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) 
            => values.All(v => v is bool boolean && boolean);
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotSupportedException("BooleanAndConverter is a OneWay converter.");
    }
}
