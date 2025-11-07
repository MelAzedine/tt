using System;
using System.Globalization;
using System.Windows.Data;

namespace Trident.MITM
{
    public sealed class EqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.Ordinal);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? parameter?.ToString() : Binding.DoNothing;
    }
}
