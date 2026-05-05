using Apolo.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Models;
using System;
using Windows.Devices.Enumeration;

namespace Apolo.ViewModels
{
    public sealed partial class StudentsPage : Page
    {
        public StudentsViewModel ViewModel => (StudentsViewModel)DataContext;
        public StudentsPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<StudentsViewModel>();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e) =>
            await ViewModel.LoadAsync();

        private async void DeleteStudent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;
            if (btn.DataContext is not StudentSummary item)
                return;

            var dialog = new ContentDialog()
            {
                Title = "Delete student?",
                Content = $"This will delete student '{item.FullName}'. \n"
                 + $"Note: related specifications and attendances will also be removed.",
                PrimaryButtonText = Loc.Buttons_Delete,
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteStudentAsync(item.Id);
            }
        }

        private async void NewStudent_Click(object sender, RoutedEventArgs e)
        {
            var firstNameBox = new TextBox { Header = "First name", MinWidth = 320 };
            var lastNameBox = new TextBox { Header = "Last name", MinWidth = 320 };
            var payersBox = new ComboBox
            {
                Header = "Payer (optional if student is payer)",
                ItemsSource = ViewModel.Payers,
                DisplayMemberPath = "FullName",
                SelectedValuePath = "Id"
            };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(firstNameBox);
            panel.Children.Add(lastNameBox);
            panel.Children.Add(payersBox);

            var dialog = new ContentDialog()
            {
                Title = "Create student",
                Content = panel,
                PrimaryButtonText = "Create",
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.AddStudentAsync(
                    firstNameBox.Text,
                    lastNameBox.Text,
                    (Guid?)payersBox.SelectedValue);
            }
        }

        private async void EditStudent_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn)
                return;
            if (btn.DataContext is not StudentSummary item)
                return;

            // Prefill with the current names
            var firstBox = new TextBox { Header = "First name", Text = item.FirstName, MinWidth = 320, MaxLength = 100 };
            var lastBox = new TextBox { Header = "Last name", Text = item.LastName, MinWidth = 320, MaxLength = 100 };

            var payerBox = new ComboBox
            {
                Header = Loc.Box_Payer,
                ItemsSource = ViewModel.Payers,
                SelectedValuePath = "Id",
                DisplayMemberPath = "FullName",
                SelectedValue = item.PayerId
            };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(firstBox);
            panel.Children.Add(lastBox);
            panel.Children.Add(payerBox);

            var dialog = new ContentDialog()
            {
                Title = "Edit student",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.UpdateStudentAsync(item.Id, firstBox.Text, lastBox.Text, (Guid)payerBox.SelectedValue);
            }
        }
    }
}
