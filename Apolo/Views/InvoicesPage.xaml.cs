using Apolo.Controls;
using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Vml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Models;
using System;

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
            if (await ConfirmationDialog.ConfirmButtonAction(sender, "remove selected lessons"))
                await ViewModel.RemoveSelectedLessonsAsync();
        }

        private async void DeleteBill_Click(object sender, RoutedEventArgs e)
        {
            if (await ConfirmationDialog.ConfirmButtonAction(sender, $"delete bill {ViewModel.Bill.Name}"))
                await ViewModel.DeleteBillAsync();
        }

        private async void EditBill_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.Bill.Id is null)
                return; 

            var typeOption = new RadioButtons
            {
                Header = "Document Type",
                Items = { "Invoice", "Ticket" },
                SelectedIndex = (ViewModel.Bill.Type == DocumentType.Invoice) ? 0 : 1
            };
            var sequence = new NumberBox
            {
                Header = "Sequence Number",
                Minimum = 0,
                Value = ViewModel.Bill.SequenceNumber
            };
            var datePicker = new DatePicker
            {
                Header = "Creation Date",
                Date = ViewModel.Bill.CreatedUTC
            };
            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(typeOption);
            panel.Children.Add(sequence);
            panel.Children.Add(datePicker);

            var dialog = new ContentDialog()
            {
                Title = $"Edit {ViewModel.Bill.Name}",
                Content = panel,
                PrimaryButtonText = "Save",
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
            var typeOption = new RadioButtons
            {
                Header = "Document Type",
                Items = { "Invoice", "Ticket" },
                SelectedIndex = (ViewModel.Bill.Type == DocumentType.Invoice) ? 0 : 1
            };
            var datePicker = new DatePicker
            {
                Header = "Creation Date",
                Date = DateTime.Now
            };
            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(typeOption);
            panel.Children.Add(datePicker);

            var dialog = new ContentDialog()
            {
                Title = $"Edit {ViewModel.Bill.Name}",
                Content = panel,
                PrimaryButtonText = "Save",
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
