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
            if (sender is not Button button) return;

            //disable the button to avoid double-clicking
            button.IsEnabled = false;

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

            // re-enable the button
            button.IsEnabled = true;
        }

        private async void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            //disable the button to avoid double-clicking
            button.IsEnabled = false;

            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);

            picker.CommitButtonText = "Pick Folder";
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.ViewMode = PickerViewMode.List;


            var folder = await picker.PickSingleFolderAsync();

            if (folder == null) return;

            try
            {
                await ViewModel.ExportDatabaseToExcel(folder.Path);

                var successDialog = new ContentDialog
                {
                    Title = "Success",
                    Content = $"Database exported to {folder.Path}",
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

            // re-enable the button
            button.IsEnabled = true;

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
                string result = await ViewModel.ImportDatabaseFromExcel(file.Path);

                var successDialog = new ContentDialog
                {
                    Title = "Success",
                    Content = $"Database imported from {file.Path}\nSummary saved to {result}",
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
                    Content = $"Failed to import database: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }

            // re-enable the button
            button.IsEnabled = true;
        }
    }
}
