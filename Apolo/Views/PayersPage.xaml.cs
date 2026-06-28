using Apolo.Controls;
using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Models;
using System;

namespace Apolo.Pages
{
    public sealed partial class PayersPage : Page
    {

        public PayersViewModel ViewModel => (PayersViewModel)DataContext;
        public PayersPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<PayersViewModel>();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e) =>
            await ViewModel.LoadAsync();

        private async void DeletePayer_Click(object sender, RoutedEventArgs e)
        {
            Guid? id = await ConfirmationDialog.ConfirmItemAction(sender, Loc.Action_DeletePayer);
            if (id is not null)
            {
                await ViewModel.DeletePayerAsync(id.Value);
            }
        }

        private async void EditPayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;
            if (btn.DataContext is not PayerSummary item)
                return;

            // Prefill with the current names
            var firstBox = new TextBox { Header = Loc.Common_FirstName, Text = item.FirstName, MinWidth = 300 };
            var lastBox = new TextBox { Header = Loc.Common_LastName, Text = item.LastName, MinWidth = 300 };
            var addressBox = new TextBox { Header = Loc.Common_Address, Text = item.Address, MinWidth = 300 };
            var zipBox = new TextBox { Header = Loc.Common_ZipCode, Text = item.Zip, MinWidth = 300 };
            var cityBox = new TextBox { Header = Loc.Common_City, Text = item.City, MinWidth = 300 };
            var taxBox = new TextBox { Header = "NIF/CIF", Text = item.TaxId, MinWidth = 300 };


            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(firstBox);
            panel.Children.Add(lastBox);
            panel.Children.Add(addressBox);
            panel.Children.Add(zipBox);
            panel.Children.Add(cityBox);
            panel.Children.Add(taxBox);

            var dialog = new ContentDialog()
            {
                Title = Loc.Buttons_Edit,
                Content = panel,
                PrimaryButtonText = Loc.Buttons_Save,
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.UpdatePayerAsync(item.Id, firstBox.Text, lastBox.Text,
                    addressBox.Text ?? string.Empty, zipBox.Text ?? string.Empty, 
                    cityBox.Text ?? string.Empty, taxBox.Text ?? string.Empty);
            }

        }

        private async void NewPayer_Click(object sender, RoutedEventArgs e)
        {
            var firstNameBox = new TextBox { Header = Loc.Common_FirstName, MinWidth = 320 };
            var lastNameBox = new TextBox { Header = Loc.Common_LastName, MinWidth = 320 };
            var addressBox = new TextBox { Header = Loc.Common_Address, MinWidth = 320 };
            var zipCodeBox = new TextBox { Header = Loc.Common_ZipCode, MinWidth = 320 };
            var cityBox = new TextBox { Header = Loc.Common_City, MinWidth = 320 };
            var idBox = new TextBox { Header = "CIF/NIF", MinWidth = 320 };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(firstNameBox);
            panel.Children.Add(lastNameBox);
            panel.Children.Add(addressBox);
            panel.Children.Add(zipCodeBox);
            panel.Children.Add(cityBox);
            panel.Children.Add(idBox);

            var dialog = new ContentDialog()
            {
                Title = Loc.Buttons_Create,
                Content = panel,
                PrimaryButtonText = Loc.Buttons_Create,
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.AddPayerAsync(
                    firstNameBox.Text,
                    lastNameBox.Text,
                    addressBox.Text,
                    zipCodeBox.Text,
                    cityBox.Text,
                    idBox.Text);
            }
        }
    }
}
