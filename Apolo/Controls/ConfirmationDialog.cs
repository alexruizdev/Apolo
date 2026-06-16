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

            var dialog = new ContentDialog
            {
                Title = $"Confirm: {action}",
                Content = $"Are you sure you want to {action}? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
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

            var dialog = new ContentDialog
            {
                Title = $"Confirm: {action}",
                Content = $"Are you sure you want to {action} '{item.Name}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = button.XamlRoot
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return null;

            return item.Id;
        }
    }
}
