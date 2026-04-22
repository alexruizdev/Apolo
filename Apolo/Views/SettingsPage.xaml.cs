using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.IO;
using WinRT.Interop;


namespace Apolo.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel => (SettingsViewModel)DataContext;
        public SettingsPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<SettingsViewModel>();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e) =>
            await ViewModel.LoadAsync();

        private async void DeleteDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Confirm Deletion",
                Content = "Are you sure you want to delete the entire database? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    await ViewModel.ClearDatabaseAsync();
                    var successDialog = new ContentDialog
                    {
                        Title = "Success",
                        Content = "Database deleted successfully.",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "Error",
                        Content = $"Failed to delete database: {ex.Message}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        private async void ExportDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;
            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.CommitButtonText = "Pick a folder";

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;

            try
            {
                //string destinationPath = await ViewModel.ExportDatabaseAsync(folder);
                string destinationPath = string.Empty;

                var successDialog = new ContentDialog
                {
                    Title = "Success",
                    Content = $"Database exported to {destinationPath}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to export database: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void ImportDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            //disable the button to avoid double-clicking
            button.IsEnabled = false;

            var picker = new FileOpenPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);
            picker.FileTypeFilter.Add(".db");

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.CommitButtonText = "Pick a database";

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            try
            {
                //string destinationPath = await ViewModel.ImportDatabaseAsync(file);
                string destinationPath = string.Empty;

                var successDialog = new ContentDialog
                {
                    Title = "Success",
                    Content = $"Database imported to {destinationPath}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to export database: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        private async void ImportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            //disable the button to avoid double-clicking
            button.IsEnabled = false;

            var picker = new FileOpenPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);
            picker.FileTypeFilter.Add(".xlsx");
            picker.FileTypeFilter.Add(".xls"); 
            picker.FileTypeFilter.Add(".xlsm");

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.CommitButtonText = "Pick a file";

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            try
            {
                await ViewModel.ImportDatabaseFromExcel(file.Path);

                var successDialog = new ContentDialog
                {
                    Title = "Success",
                    Content = $"Database imported from {file.Path}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var errorDialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"Failed to export database: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }
}
