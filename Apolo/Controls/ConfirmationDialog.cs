using Apolo.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Models;
using System;
using System.Threading.Tasks;

namespace Apolo.Controls
{
    public static class ConfirmationDialog
    {
        public static async Task<bool> ConfirmAction(object sender, string action)
        {
            if (sender is not Button button) return false;

            var sl = new StringLocalizer();

            var dialog = new ContentDialog
            {
                Title = Loc.F("Confirmation/Title", action),
                Content = Loc.F("Confirmation/Content", action),
                PrimaryButtonText = Loc.Buttons_Delete,
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = button.XamlRoot
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        public static async Task<Guid?> ConfirmItemAction(object sender, string action)
        {
            if (sender is not Button button) return null;

            if (button.DataContext is not ISummary item)
                return null;

            var sl = new StringLocalizer();

            var dialog = new ContentDialog
            {
                Title = Loc.F("Confirmation/Title", action),
                Content = Loc.F("Confirmation/Content", action, item.Name),
                PrimaryButtonText = Loc.Buttons_Delete,
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = button.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return null;

            return item.Id;
        }
    }
}
