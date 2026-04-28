using Apolo.Service;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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
        var priceBox = new NumberBox { Header = "Price:", PlaceholderText = "leave empty to use service price", SmallChange = 15, LargeChange = 30 };
        var onlineBox = new CheckBox { Content = "Online", IsChecked = item.IsOnline };
        var weekendBox = new CheckBox { Content = "Weekend or holiday", IsChecked = item.IsWeekenOrHoliday };

        if (item.Price is not null)
            priceBox.Value = item.Price.Value;

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
        panel.Children.Add(priceBox);
        panel.Children.Add(onlineBox);
        panel.Children.Add(weekendBox);
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
            double? price = priceBox.Value == double.NaN ? null : priceBox.Value;
            await ViewModel.UpdateSpecificationAsync(item.Id, nameBox.Text, (int)durationBox.Value, price,
                onlineBox.IsChecked == true, weekendBox.IsChecked == true, (Guid)serviceBox.SelectedValue);
        }
    }

    private async void CreateLesson_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;
        if (btn.DataContext is not SpecificationSummary item)
            return;

        var service = ViewModel.Services.First(s => s.Id == item.ServiceId);

        var datePicker = new CalendarDatePicker { Header = "Date", IsTodayHighlighted = true };
        var noteBox = new TextBox
        {
            Header = "Notes",
            MinWidth = 400,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap
        };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(datePicker);
        panel.Children.Add(noteBox);

        var viewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollMode = ScrollMode.Enabled,
            MaxHeight = 500,
            Content = panel
        };

        var dialog = new ContentDialog()
        {
            Title = "Create lesson",
            Content = viewer,
            PrimaryButtonText = "Create",
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };


        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var dto = datePicker.Date ?? DateTimeOffset.Now;
            var date = DateOnly.FromDateTime(dto.Date);
            await ViewModel.CreateLessonFromSpecificationAsync(item, date, service, 
                ViewModel.TravelAllowance, ViewModel.WeekendFee);
        }
    }

    private async void NewSpecification_Click(object sender, RoutedEventArgs e)
    {
        var nameBox = new TextBox { Header = "Specification name", MinWidth = 320 };
        var studentBox = new ComboBox
        {
            Header = "Student",
            ItemsSource = ViewModel.Students,
            DisplayMemberPath = "FullName",
            SelectedValuePath = "Id"
        };
        var serviceBox = new ComboBox
        {
            Header = "Service",
            ItemsSource = ViewModel.Services,
            SelectedValuePath = "Id",
            DisplayMemberPath = "Name"
        };
        var durationBox = new NumberBox { Header = "Duration (minutes):", Value = 60, SmallChange = 15, LargeChange = 30 };
        var priceBox = new NumberBox { Header = "Price:", PlaceholderText = "leave empty to use service price", SmallChange = 15, LargeChange = 30 };
        var onlineBox = new CheckBox { Content = "Online" };
        var weekendBox = new CheckBox { Content = "Weekend or Holiday" };

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(nameBox);
        panel.Children.Add(studentBox);
        panel.Children.Add(serviceBox);
        panel.Children.Add(durationBox);
        panel.Children.Add(priceBox);
        panel.Children.Add(onlineBox);
        panel.Children.Add(weekendBox);

        var dialog = new ContentDialog()
        {
            Title = "Create specification",
            Content = panel,
            PrimaryButtonText = "Create",
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await ViewModel.AddSpecificationAsync(
                nameBox.Text,
                (int)durationBox.Value,
                priceBox.Value == double.NaN ? null : priceBox.Value,
                onlineBox.IsChecked == true,
                weekendBox.IsChecked == true,
                (Guid?)studentBox.SelectedValue,
                (Guid?)serviceBox.SelectedValue);
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
}
