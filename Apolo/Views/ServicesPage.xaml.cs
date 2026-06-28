using Apolo.Controls;
using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Models;
using System;

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
        Guid? id = await ConfirmationDialog.ConfirmItemAction(sender, Loc.Action_DeleteService);
        if (id is not null)
            await ViewModel.DeleteServiceAsync(id.Value);
    }

    private async void EditService_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;
        if (btn.DataContext is not ServiceSummary item)
            return;

        // Prefill with the current names
        var nameBox = new TextBox { Header = Loc.Common_Name, Text = item.Name, MinWidth = 320, MaxLength = 100 };
        var priceBox = new NumberBox {
            Header = Loc.Common_Price, 
            Value = (double)item.Price, 
            PlaceholderText = "0.00"
        };
        var isPricePerHourCheck = new CheckBox { Content = Loc.Common_PerHour, IsChecked = item.IsPricePerHour };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(nameBox);
        panel.Children.Add(priceBox);
        panel.Children.Add(isPricePerHourCheck);
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
            await ViewModel.UpdateServiceAsync(item.Id, nameBox.Text, 
                isPricePerHourCheck.IsChecked == true, (decimal)priceBox.Value);
        }

    }

    private async void NewService_Click(object sender, RoutedEventArgs e)
    {
        var nameBox = new TextBox { Header = Loc.Common_Name, MinWidth = 320, MaxLength = 120 };
        var priceBox = new NumberBox { Header = Loc.Common_Price, MinWidth = 320, PlaceholderText = "0.00", Value = 0 };
        var isPricePerHourCheck = new CheckBox { Content = Loc.Common_PerHour, IsChecked = true };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(nameBox);
        panel.Children.Add(priceBox);
        panel.Children.Add(isPricePerHourCheck);
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
        if (result != ContentDialogResult.Primary)
            return;

        await ViewModel.AddServiceAsync(nameBox.Text, isPricePerHourCheck.IsChecked == true,
            (decimal)priceBox.Value);
    }
}
