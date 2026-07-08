using Apolo.Controls;
using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Models;
using System;

namespace Apolo.Views
{
    public sealed partial class InvoicesPage : Page
    {
        public static string ConfigureBillText => Loc.S("Common/ConfigureBill");
        public static string SearchBillPlaceholderText => Loc.S("Common/SearchBillPlaceholder");

        public BillingViewModel ViewModel => (BillingViewModel)DataContext;
        public InvoicesPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<BillingViewModel>();
        }

        private static RadioButtons CreateDocumentTypeOptions(DocumentType selectedType)
        {
            var typeOption = new RadioButtons
            {
                Header = Loc.S("Common/DocumentType"),
                SelectedIndex = selectedType == DocumentType.Invoice ? 0 : 1
            };

            typeOption.Items.Add(Loc.S("Common/Invoice"));
            typeOption.Items.Add(Loc.S("Common/Ticket"));

            return typeOption;
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
            if (await ConfirmationDialog.ConfirmButtonAction(sender, Loc.Action_RemoveSelectedLessons))
                await ViewModel.RemoveSelectedLessonsAsync();
        }

        private async void DeleteBill_Click(object sender, RoutedEventArgs e)
        {
            if (await ConfirmationDialog.ConfirmButtonAction(sender, $"{Loc.Action_DeleteBill} {ViewModel.Bill.Name}"))
                await ViewModel.DeleteBillAsync();
        }

        private async void EditBill_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Bill.Id is null)
                return; 

            var typeOption = CreateDocumentTypeOptions(ViewModel.Bill.Type);
            var sequence = new NumberBox
            {
                Header = Loc.S("Common/SequenceNumber"),
                Minimum = 0,
                Value = ViewModel.Bill.SequenceNumber
            };
            var datePicker = new DatePicker
            {
                Header = Loc.S("Common/CreationDate"),
                Date = ViewModel.Bill.CreatedUTC
            };
            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(typeOption);
            panel.Children.Add(sequence);
            panel.Children.Add(datePicker);

            var dialog = new ContentDialog()
            {
                Title = Loc.F("Dialogs/EditBillTitle", ViewModel.Bill.Name),
                Content = panel,
                PrimaryButtonText = Loc.Buttons_Save,
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                DocumentType newType = (typeOption.SelectedIndex == 0) ? DocumentType.Invoice : DocumentType.Ticket;
                int newSequence = (int)sequence.Value;
                DateTime newDate = datePicker.Date.DateTime;
                await ViewModel.EditBill(newType, newSequence, newDate);
            }
        }

        private async void CreateBill_Click(object sender, RoutedEventArgs e)
        {
            var typeOption = CreateDocumentTypeOptions(ViewModel.Bill.Type);
            var datePicker = new DatePicker
            {
                Header = Loc.S("Common/CreationDate"),
                Date = DateTime.Now
            };
            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(typeOption);
            panel.Children.Add(datePicker);

            var dialog = new ContentDialog()
            {
                Title = Loc.S("Dialogs/CreateBillTitle"),
                Content = panel,
                PrimaryButtonText = Loc.Buttons_Create,
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                DocumentType newType = (typeOption.SelectedIndex == 0) ? DocumentType.Invoice : DocumentType.Ticket;
                DateTime newDate = datePicker.Date.DateTime;
                await ViewModel.CreateBill(newType, newDate);
            }
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
