using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Models;
using System;
using System.Threading.Tasks;

namespace Apolo.Controls
{
    public static class ConfirmationDialog
    {
        public static async Task<bool> ConfirmMenuAction(object sender, string action)
        {
            if (sender is not MenuFlyoutItem item) return false;

            return await ConfirmAction(item.XamlRoot, action);
        }
        public static async Task<bool> ConfirmButtonAction(object sender, string action)
        {
            if (sender is not Button button) return false;

            return await ConfirmAction(button.XamlRoot, action);
        }

        private static async Task<bool> ConfirmAction(XamlRoot root, string action)
        {
            var dialog = new ContentDialog
            {
                Title = $"Confirm: {action}",
                Content = $"Are you sure you want to {action}? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = root
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        public static async Task<Guid?> ConfirmMenuItemAction(object sender, string action)
        {
            if (sender is not MenuFlyoutItem button) return null;

            return await ConfirmItemAction(button.DataContext, button.XamlRoot, action);
        }

        public static async Task<Guid?> ConfirmButtonItemAction(object sender, string action)
        {
            if (sender is not Button button) return null;

            return await ConfirmItemAction(button.DataContext, button.XamlRoot, action);
        }

        private static async Task<Guid?> ConfirmItemAction(object data, XamlRoot root, string action)
        {
            if (data is not ISummary item)
                return null;

            var dialog = new ContentDialog
            {
                Title = $"Confirm: {action}",
                Content = $"Are you sure you want to {action} '{item.Name}'? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = root
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return null;

            return item.Id;
        }
    }
}
