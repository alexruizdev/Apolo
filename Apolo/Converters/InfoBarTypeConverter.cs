using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using ViewModels;

namespace Apolo.Converters
{
    public sealed class InfoBarTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targeType, object parameter, string language)
        {
            InfoBarType b = value is InfoBarType v ? v : InfoBarType.Info;

            return b switch
            {
                InfoBarType.Success => InfoBarSeverity.Success,
                InfoBarType.Warning => InfoBarSeverity.Warning,
                InfoBarType.Error => InfoBarSeverity.Error,
                InfoBarType.Info => InfoBarSeverity.Informational,
                _ => InfoBarSeverity.Informational
            };
        }

        public object ConvertBack(object value, Type targeType, object parameter, string language)
        {
            if (value is InfoBarSeverity v)
            {
                return v switch
                {
                    InfoBarSeverity.Success => InfoBarType.Success,
                    InfoBarSeverity.Warning => InfoBarType.Warning,
                    InfoBarSeverity.Error => InfoBarType.Error,
                    InfoBarSeverity.Informational => InfoBarType.Info,
                    _ => InfoBarType.Info
                };
            }
            return InfoBarSeverity.Informational;
        }
    }
}
