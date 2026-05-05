using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;

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

        private async void DeleteDatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var dialog = new ContentDialog
            {
                Title = "Confirm Deletion",
                Content = "Are you sure you want to delete the entire database? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;
            
            await ViewModel.ClearDatabaseAsync();
        }

        private async void ExportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);

            picker.CommitButtonText = "Pick Folder";
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.ViewMode = PickerViewMode.List;

            var folder = await picker.PickSingleFolderAsync();

            if (folder == null) return;

            var installedPath = Windows.ApplicationModel.Package.Current.InstalledPath;

            await ViewModel.ExportDatabaseToExcel(folder.Path, installedPath);
        }

        private async void ImportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var picker = new FileOpenPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);
            picker.FileTypeFilter.Add(".xlsx");
            picker.FileTypeFilter.Add(".xls"); 
            picker.FileTypeFilter.Add(".xlsm");

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.CommitButtonText = "Pick a file";

            var file = await picker.PickSingleFileAsync();
            if (file == null) return;

            await ViewModel.ImportDatabaseFromExcel(file.Path);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Call the refresh method on your ViewModel
            if (ViewModel != null)
            {
                await ViewModel.RefreshProfileAsync();
            }
        }
    }
}
