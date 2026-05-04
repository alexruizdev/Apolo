using Microsoft.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using Apolo.ViewModels;
using Microsoft.UI.Xaml;
using Models;
using System;
using Apolo.Services;

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
            if (sender is not Button btn)
                return;
            if (btn.DataContext is not PayerSummary item)
                return;

            var dialog = new ContentDialog()
            {
                Title = "Delete payer?",
                Content = $"This will delete payer '{item.FullName}'. \n"
                 + $"Note: You can only delete payers with no students.",
                PrimaryButtonText = Loc.Buttons_Delete,
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeletePayerAsync(item);
            }
        }

        private async void EditPayer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;
            if (btn.DataContext is not PayerSummary item)
                return;

            // Prefill with the current names
            var firstBox = new TextBox { Header = "First name", Text = item.FirstName, MinWidth = 300 };
            var lastBox = new TextBox { Header = "Last name", Text = item.LastName, MinWidth = 300 };
            var addressBox = new TextBox { Header = "Address", Text = item.LastName, MinWidth = 300 };
            var zipBox = new TextBox { Header = "Zip code", Text = item.LastName, MinWidth = 300 };
            var cityBox = new TextBox { Header = "City", Text = item.LastName, MinWidth = 300 };
            var taxBox = new TextBox { Header = "NIF/CIF", Text = item.LastName, MinWidth = 300 };


            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(firstBox);
            panel.Children.Add(lastBox);
            panel.Children.Add(addressBox);
            panel.Children.Add(zipBox);
            panel.Children.Add(cityBox);
            panel.Children.Add(taxBox);

            var dialog = new ContentDialog()
            {
                Title = "Edit payer",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.UpdatePayerAsync(item.Id, firstBox.Text, lastBox.Text, addressBox.Text, zipBox.Text, cityBox.Text, taxBox.Text);
            }

        }

        private async void NewPayer_Click(object sender, RoutedEventArgs e)
        {
            var firstNameBox = new TextBox { Header = "First name", MinWidth = 320 };
            var lastNameBox = new TextBox { Header = "Last name", MinWidth = 320 };
            var addressBox = new TextBox { Header = "Address", MinWidth = 320 };
            var zipCodeBox = new TextBox { Header = "Zip code", MinWidth = 320 };
            var cityBox = new TextBox { Header = "City", MinWidth = 320 };
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
                Title = "Create payer",
                Content = panel,
                PrimaryButtonText = "Create",
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
