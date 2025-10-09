using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using Windows.Storage;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Linq;
using WinRT.Interop;

namespace Apolo.Views
{
    public sealed partial class InvoicesPage : Page
    {
        public InvoicesViewModel ViewModel => (InvoicesViewModel)DataContext;
        public InvoicesPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<InvoicesViewModel>();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
            => await ViewModel.LoadAsync();

        private async void LoadForPayer_Click(object sender, RoutedEventArgs e)
            => await ViewModel.LoadAttendancesAsync();

        private async void MarkPaid_Click(object sender, RoutedEventArgs e)
        {
            var selectedCount = ViewModel.Attendances.Where(a => a.IsSelected).Count();

            if (selectedCount == 0)
                return;

            var dialog = new ContentDialog
            {
                Title = "Mark as paid?",
                Content = $"Mark {selectedCount} attendance(s) as paid?",
                PrimaryButtonText = "Mark paid",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
                await ViewModel.MarkSelectedAsPaidAsync();
        }

        private async void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            // Gather rows to include (selected; if none selected, include all)
            var attendances = ViewModel.Attendances.Where(a => a.IsSelected).ToArray();
            if (attendances.Length == 0) attendances = ViewModel.Attendances.ToArray();
            if (attendances.Length == 0) return;

            var payer = ViewModel.Payers.FirstOrDefault(p => p.Id == ViewModel.SelectedPayerId);
            var payerName = payer?.FullName ?? "Unknown";

            // Ask where to save
            var picker = new FolderPicker(button.XamlRoot.ContentIslandEnvironment.AppWindowId);
            picker.CommitButtonText = "Pick a folder";

            var folder = await picker.PickSingleFolderAsync();
            if (folder == null) return;

            var filePath = Path.Combine(folder.Path, $"Invoice_{payerName.Replace(' ', '_')}_{DateTime.Now:ddMMyyyy}.pdf");

            // Build PDF
            ViewModel.BuildInvoicePdf(payerName, attendances, filePath);

            var done = new ContentDialog
            {
                Title = "Invoice saved",
                Content = $"Saved to: {filePath}",
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };

            _ = await done.ShowAsync();
        }
    }
}
