using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Apolo.Converters
{
    public class NumberSignToColorConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string trend)
            {
                if (trend.Contains("+"))
                    return Application.Current.Resources["SystemFillColorSuccessBrush"] as Brush;
                if (trend.Contains("-"))
                    return Application.Current.Resources["SystemFillColorCriticalBrush"] as Brush;
            }

            // Default fallback for 0 or invalid types
             return Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
