using Apolo.Controls;
using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using Models;
using System;

namespace Apolo.Views
{
    public sealed partial class LessonsPage : Page
    {
        public LessonsViewModel ViewModel => (LessonsViewModel)DataContext;
        public LessonsPage()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<LessonsViewModel>();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e) =>
            await ViewModel.LoadAsync();

        private async void NewLesson_Click(object sender, RoutedEventArgs e)
        {
            var formControl = new LessonFormDialog(ViewModel);

            var dialog = new ContentDialog()
            {
                PrimaryButtonText = "Create",
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            Binding operationsBinding = new Binding
            {
                Source = formControl.ViewModel,
                Path = new PropertyPath("IsPrimaryButtonEnabled"),
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(dialog, ContentDialog.IsPrimaryButtonEnabledProperty, operationsBinding);

            Binding dynamicTitleBinding = new Binding
            {
                Source = formControl.ViewModel,
                Path = new PropertyPath("DialogTitle"),
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(dialog, ContentDialog.TitleProperty, dynamicTitleBinding);

            dialog.Content = formControl;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await formControl.ViewModel.SaveLessonAsync();
            }

        }

        private async void DeleteLesson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.DataContext is not LessonSummary row) return;

            await ViewModel.DeleteLessonAsync(row.Id);
        }

        private async void ChangePayment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.DataContext is not LessonSummary row) return;
            await ViewModel.ChangePayment(row.Id);
        }

        private async void EditLesson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.DataContext is not LessonSummary row) return;

            var formControl = new LessonFormDialog(ViewModel, row);

            var dialog = new ContentDialog()
            {
                PrimaryButtonText = "Edit",
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            Binding operationsBinding = new Binding
            {
                Source = formControl.ViewModel,
                Path = new PropertyPath("IsPrimaryButtonEnabled"),
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(dialog, ContentDialog.IsPrimaryButtonEnabledProperty, operationsBinding);

            Binding dynamicTitleBinding = new Binding
            {
                Source = formControl.ViewModel,
                Path = new PropertyPath("DialogTitle"),
                Mode = BindingMode.OneWay
            };
            BindingOperations.SetBinding(dialog, ContentDialog.TitleProperty, dynamicTitleBinding);

            dialog.Content = formControl;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await formControl.ViewModel.EditLessonAsync();
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
}
