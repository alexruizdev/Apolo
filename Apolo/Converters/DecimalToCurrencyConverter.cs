using System;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace Apolo.Converters
{
    public sealed class DecimalToCurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var culture = new CultureInfo("es-ES");
            if (value is decimal d)
                return d.ToString("C", culture);
            if (value is double db)
                return db.ToString("C", culture);
            if (value is float f)
                return ((decimal)f).ToString("C", culture);
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
