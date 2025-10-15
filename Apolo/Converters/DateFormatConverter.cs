using System;
using Microsoft.UI.Xaml.Data;

namespace Apolo.Converters
{
    public class DateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateOnly date)
            {
                return date.ToString("dd/MM/yyyy");
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (DateOnly.TryParse(value as string, out DateOnly result))
            {
                return result;
            }
            return DateOnly.FromDateTime(DateTime.Now);
        }
    }

}