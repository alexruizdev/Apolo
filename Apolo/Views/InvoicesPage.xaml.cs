using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Linq;

namespace Apolo.Views
{
    public sealed partial class InvoicesPage : Page
    {
        public BillingViewModel ViewModel => (BillingViewModel)DataContext;
        public InvoicesPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<BillingViewModel>();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
            => await ViewModel.LoadAsync();

        private async void LoadForPayer_Click(object sender, RoutedEventArgs e)
            => await ViewModel.LoadLessonsAsync();

        private async void MarkPaid_Click(object sender, RoutedEventArgs e)
        {
            var selectedCount = ViewModel.Lessons.Where(a => a.IsSelected).Count();

            if (selectedCount == 0)
                return;

            var dialog = new ContentDialog
            {
                Title = "Mark as paid?",
                Content = $"Mark {selectedCount} lesson(s) as paid?",
                PrimaryButtonText = "Mark paid",
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
                await ViewModel.MarkSelectedAsPaidAsync();
        }

        private async void CreateInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            // Ask where to save
            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);
            picker.CommitButtonText = "Pick a folder";

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;

            await ViewModel.GenerateInvoice(folder.Path, isInvoice: true);
        }

        private async void CreateTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            // Ask where to save
            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);
            picker.CommitButtonText = "Pick a folder";

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;

            await ViewModel.GenerateInvoice(folder.Path, isInvoice: false);
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
