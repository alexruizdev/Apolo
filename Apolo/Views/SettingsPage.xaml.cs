using Apolo.Controls;
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
            if (await ConfirmationDialog.ConfirmButtonAction(sender, Loc.Action_DeleteDatabase, Loc.Buttons_Delete))
                await ViewModel.ClearDatabaseAsync();
        }

        private async void AddDummuDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (await ConfirmationDialog.ConfirmButtonAction(sender, Loc.Action_AddDummyData, Loc.Buttons_Add))
                await ViewModel.AddDummyData();
        }

        private async void DeleteArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (await ConfirmationDialog.ConfirmButtonAction(sender, Loc.Action_DeleteArchive, Loc.Buttons_Delete))
                await ViewModel.ClearArchiveAsync();
        }

        private async void ExportBackupButton_Click(object sender, RoutedEventArgs e)
        private async void ExportBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button) return;

            await ViewModel.ExportDatabaseToCSV();
        }

        private async void ExportArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button) return;

            var installedPath = Windows.ApplicationModel.Package.Current.InstalledPath;

            await ViewModel.ExportArchiveToCSV(installedPath);
        }

        private async void ImportBackupButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                CommitButtonText = Loc.Buttons_PickFile
            };

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;

            await ViewModel.ImportDatabaseFromCSV(folder.Path);
        }

        private async void ImportArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                CommitButtonText = Loc.Buttons_PickFile
            };

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;

            await ViewModel.ImportArchiveFromCSV(folder.Path);
        }

        private async void ArchiveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var payers = await ViewModel.GetPayersActivity();

            var payersList = new ListView
            {
                Header = Loc.Common_Payer,
                SelectionMode = ListViewSelectionMode.Multiple,
                ItemsSource = payers,
                MaxHeight = 240,
                DisplayMemberPath = "Display"
            };

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
                Title = Loc.Settings_ArchiveOldData,
                Content = viewer,
                PrimaryButtonText = Loc.Buttons_Archive,
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
                Header = Loc.Common_Payer,
                SelectionMode = ListViewSelectionMode.Multiple,
                ItemsSource = payers,
                MaxHeight = 240,
                DisplayMemberPath = "Name"
            };

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
                Title = Loc.Settings_SelectPayersArchive,
                Content = viewer,
                PrimaryButtonText = Loc.Buttons_Retrieve,
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

        private async void PickBillingFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                CommitButtonText = Loc.Buttons_PickFolder,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };

            // Show the picker dialog window
            var folder = await picker.PickSingleFolderAsync();
            if (folder == null)
                return;

            BillingFolderTextBlock.Text = folder.Path;
        }

        private async void PickBackupFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId)
            {
                CommitButtonText = Loc.Buttons_PickFolder,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };

            // Show the picker dialog window
            var folder = await picker.PickSingleFolderAsync();
            if (folder == null)
                return;

            BackupFolderTextBlock.Text = folder.Path;
        }
    }
}
