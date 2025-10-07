using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Apolo.Converters
{
    public sealed class NullToVisibilityConverter : IValueConverter
    {
        // If true, null/empty -> Visible (and non-null -> Collapsed)
        public bool Invert { get; set; }

        // Treat empty strings as null 
        public bool TreatEmptyStringAsNull { get; set; } = true;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isNull = value is null ||
                (TreatEmptyStringAsNull && value is string s && string.IsNullOrWhiteSpace(s));

            if (Invert) isNull = !isNull;

            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
