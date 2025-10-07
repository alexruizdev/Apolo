using System;
using Microsoft.UI.Xaml.Data;

namespace Apolo.Converters
{
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var b = value is bool v && v;
            return !b;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            var b = value is bool v && v;
            return !b;
        }
    }
}
