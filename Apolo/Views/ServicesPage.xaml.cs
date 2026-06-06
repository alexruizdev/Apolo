using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Models;
using System;
using System.Threading.Tasks;

namespace Apolo.Views;

public sealed partial class ServicesPage : Page
{
    public ServicesViewModel ViewModel => (ServicesViewModel)DataContext;
    public ServicesPage()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetService<ServicesViewModel>();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e) =>
        await ViewModel.LoadAsync();

    private async void DeleteService_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;
        if (btn.DataContext is not ServiceSummary item)
            return;

        var dialog = new ContentDialog()
        {
            Title = "Delete service?",
            Content = $"This will delete service '{item.Name}'. \n"
             + $"Note: related specifications will also be removed.",
            PrimaryButtonText = Loc.Buttons_Delete,
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteServiceAsync(item.Id);
        }
    }

    private async void EditService_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;
        if (btn.DataContext is not ServiceSummary item)
            return;

        // Prefill with the current names
        var nameBox = new TextBox { Header = "Name", Text = item.Name, MinWidth = 320, MaxLength = 100 };
        var priceBox = new NumberBox {
            Header = "Service rate (price):", 
            Value = (double)item.Price, 
            PlaceholderText = "0.00"
        };
        var isPricePerHourCheck = new CheckBox { Content = "Is price per hour?", IsChecked = item.IsPricePerHour };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(nameBox);
        panel.Children.Add(priceBox);
        panel.Children.Add(isPricePerHourCheck);
        var dialog = new ContentDialog()
        {
            Title = "Edit service",
            Content = panel,
            PrimaryButtonText = "Save",
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.UpdateServiceAsync(item.Id, nameBox.Text, 
                isPricePerHourCheck.IsChecked == true, (decimal)priceBox.Value);
        }

    }

    private async void NewService_Click(object sender, RoutedEventArgs e)
    {
        var nameBox = new TextBox { Header = "Name", MinWidth = 320, MaxLength = 120 };
        var priceBox = new NumberBox { Header = "Service rate (price):", MinWidth = 320, PlaceholderText = "0.00", Value = 0 };
        var isPricePerHourCheck = new CheckBox { Content = "Is price per hour?", IsChecked = true };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(nameBox);
        panel.Children.Add(priceBox);
        panel.Children.Add(isPricePerHourCheck);
        var dialog = new ContentDialog()
        {
            Title = "Create service",
            Content = panel,
            PrimaryButtonText = "Create",
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return;

        await ViewModel.AddServiceAsync(nameBox.Text, isPricePerHourCheck.IsChecked == true,
            (decimal)priceBox.Value);
    }
}
