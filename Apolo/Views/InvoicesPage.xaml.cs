using Apolo.Controls;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Models;

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

        private async void LoadForBill_Click(object sender, RoutedEventArgs e)
            => await ViewModel.LoadBillLessonsAsync();

        private async void MarkPaid_Click(object sender, RoutedEventArgs e)
            => await ViewModel.MarkSelectedPaymentAsync(markAsPaid: true);

        private async void MarkUnpaid_Click(object sender, RoutedEventArgs e)
            => await ViewModel.MarkSelectedPaymentAsync(markAsPaid: false);

        private async void RemoveLesson_Click(object sender, RoutedEventArgs e)
        {
            if (await ConfirmationDialog.ConfirmAction(sender, "remove selected lessons"))
                await ViewModel.RemoveSelectedLessonsAsync();
        }

        private async void DeleteBill_Click(object sender, RoutedEventArgs e)
        {
            if (await ConfirmationDialog.ConfirmAction(sender, $"delete bill {ViewModel.Bill.Name}"))
                await ViewModel.DeleteBillAsync();
        }

        private async void CreateInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            await ViewModel.GenerateInvoice(isInvoice: true);
        }

        private async void CreateTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button) return;

            await ViewModel.GenerateInvoice(isInvoice: false);
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

        private async void BillSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only query if the user actually typed something (ignore changes caused by code)
            if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
                return;

            await ViewModel.SuggestBills(sender.Text);
        }

        private void BillSearch_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Cast the selected item back to a BillingDocument
            if (args.SelectedItem is BillingDocument selectedBill)
            {
                ViewModel.SelectBillToEdit(selectedBill);
            }
        }
    }
}
