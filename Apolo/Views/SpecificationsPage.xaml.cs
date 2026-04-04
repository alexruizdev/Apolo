using Apolo.Service;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Models;
using System;
using System.Linq;

namespace Apolo.Views;

public sealed partial class SpecificationsPage : Page
{
    public SpecificationsViewModel ViewModel => (SpecificationsViewModel)DataContext;
    public SpecificationsPage()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetService<SpecificationsViewModel>();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e) =>
            await ViewModel.LoadAsync();

    private async void DeleteSpecification_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;
        if (btn.DataContext is not SpecificationSummary item)
            return;

        var dialog = new ContentDialog()
        {
            Title = "Delete specification?",
            Content = $"This will delete specification '{item.StudentName} {item.ServiceName}'.",
            PrimaryButtonText = Loc.Buttons_Delete,
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.DeleteSpecificationAsync(item);
        }
    }

    private async void EditSpecification_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;
        if (btn.DataContext is not SpecificationSummary item)
            return;

        // Prefill with the current names
        var nameBox = new TextBox { Header = "Specification name", Text = item.SpecificationName, MinWidth = 320, MaxLength = 120 };
        var durationBox = new NumberBox { Header = "Duration (minutes):", Value = item.DurationMinutes, SmallChange = 15, LargeChange = 30 };
        var onlineBox = new CheckBox { Content = "Online", IsChecked = item.IsOnline };

        var serviceBox = new ComboBox
        {
            Header = "Service",
            ItemsSource = ViewModel.Services,
            SelectedValuePath = "Id",
            DisplayMemberPath = "Name",
            SelectedValue = item.ServiceId
        };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(nameBox);
        panel.Children.Add(durationBox);
        panel.Children.Add(onlineBox);
        panel.Children.Add(serviceBox);

        var dialog = new ContentDialog()
        {
            Title = "Edit specification",
            Content = panel,
            PrimaryButtonText = "Save",
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.UpdateSpecificationAsync(item.Id, nameBox.Text, (int)durationBox.Value, onlineBox.IsChecked == true, (Guid)serviceBox.SelectedValue);
        }
    }

    private async void CreateLesson_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;
        if (btn.DataContext is not SpecificationSummary item)
            return;

        // price = hourly * minutes / 60
        var service = ViewModel.Services.First(s => s.Id == item.ServiceId);
        //var price = Math.Round(service.PricePerHour * (item.DurationMinutes / 60m), 2, MidpointRounding.AwayFromZero);

        var datePicker = new CalendarDatePicker { Header = "Date", IsTodayHighlighted = true };
        var priceBox = new NumberBox
        {
            Header = "Price per hour:",
            Value = (double)service.PricePerHour,
            PlaceholderText = "0.00"
        };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(priceBox);
        panel.Children.Add(datePicker);

        var dialog = new ContentDialog()
        {
            Title = "Create lesson",
            Content = panel,
            PrimaryButtonText = "Create",
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };


        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var price = Math.Round((decimal)priceBox.Value, 2, MidpointRounding.AwayFromZero);
            var dto = datePicker.Date ?? DateTimeOffset.Now;
            var date = DateOnly.FromDateTime(dto.Date);
            await ViewModel.CreateLessonFromSpecificationAsync(item.studentId, service.Name, item.DurationMinutes, item.IsOnline, price, date);
        }
    }
}
