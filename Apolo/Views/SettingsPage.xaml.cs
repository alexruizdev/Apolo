using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using Models;
using System;
using System.Linq;

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

        private async void DeleteArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var dialog = new ContentDialog
            {
                Title = "Confirm Deletion",
                Content = "Are you sure you want to delete the entire archive? This action cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
                return;

            await ViewModel.ClearArchiveAsync();
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

        private async void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var payers = await ViewModel.GetPayersActivity();

            var payersList = new ListView
            {
                Header = "Payers",
                SelectionMode = ListViewSelectionMode.Multiple,
                ItemsSource = payers,
                MaxHeight = 240
            };
            payersList.DisplayMemberPath = "Display";

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(payersList);

            var viewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Enabled,
                MaxHeight = 500,
                Content = panel
            };

            var dialog = new ContentDialog()
            {
                Title = "Archive old data",
                Content = viewer,
                PrimaryButtonText = "Archive",
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            var ids = payersList.SelectedItems.Cast<PayerActivityInfo>().Select(s => s.PayerId).ToList();
            await ViewModel.ArchiveOldData(ids);
        }

        private async void RetrieveFromArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var payers = await ViewModel.GetPayersFromArchive();

            var payersList = new ListView
            {
                Header = "Payers",
                SelectionMode = ListViewSelectionMode.Multiple,
                ItemsSource = payers,
                MaxHeight = 240
            };
            payersList.DisplayMemberPath = "FullName";

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(payersList);

            var viewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Enabled,
                MaxHeight = 500,
                Content = panel
            };

            var dialog = new ContentDialog()
            {
                Title = "Select payers to retrieve from archive.",
                Content = viewer,
                PrimaryButtonText = "Retrieve",
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary)
                return;

            var ids = payersList.SelectedItems.Cast<PayerOption>().Select(s => s.Id).ToList();
            await ViewModel.RetrieveDataFromArchive(ids);
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
