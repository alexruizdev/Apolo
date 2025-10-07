using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace Apolo.Converters
{
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        // Optional : invert the mapping (true -> Collapsed, false -> Visible)
        public bool Invert { get; set; }

        // Optional: treat null as false (default true)
        public bool NullIsFalse { get; set; } = true;

        public object Convert (object value, Type targeType, object parameter, string language)
        {
            bool b = value is bool v ? v : (!NullIsFalse && value == null) ? true : false;

            if (Invert)
                b = !b;

            return b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility v)
            {
                var result = v == Visibility.Visible;
                return Invert ? !result : result;
            }
            return false;
        }
    }
}
