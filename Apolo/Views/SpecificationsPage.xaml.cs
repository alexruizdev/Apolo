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
        Guid? id = await ConfirmationDialog.ConfirmButtonItemAction(sender, Loc.Action_DeleteSpecification);
        if (id is not null)
        {
            await ViewModel.DeleteSpecificationAsync(id.Value);
        }
    }

    private async void EditSpecification_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b || b.DataContext is not SpecificationSummary s)
            return;

        var formControl = new SpecificationFormDialog(ViewModel, s);

        var dialog = new ContentDialog()
        {
            PrimaryButtonText = Loc.Buttons_Edit,
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        Binding operationsBinding = new()
        {
            Source = formControl.ViewModel,
            Path = new PropertyPath("IsPrimaryButtonEnabled"),
            Mode = BindingMode.OneWay
        };
        BindingOperations.SetBinding(dialog, ContentDialog.IsPrimaryButtonEnabledProperty, operationsBinding);

        Binding dynamicTitleBinding = new()
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
            await formControl.ViewModel.EditSpecificationAsync();
        }

    }

    private async void CreateLesson_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)
            return;

        if (btn.DataContext is not SpecificationSummary item)
            return;

        var formControl = new LessonFormDialog(ViewModel, item);

        var dialog = new ContentDialog()
        {
            PrimaryButtonText = Loc.Buttons_Create,
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        Binding operationsBinding = new()
        {
            Source = formControl.ViewModel,
            Path = new PropertyPath("IsPrimaryButtonEnabled"),
            Mode = BindingMode.OneWay
        };
        BindingOperations.SetBinding(dialog, ContentDialog.IsPrimaryButtonEnabledProperty, operationsBinding);

        Binding dynamicTitleBinding = new()
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
            await ViewModel.RefreshSpecifications(); // Refresh the specifications to update the usage count
        }
    }

    private async void NewSpecification_Click(object sender, RoutedEventArgs e)
    {
        var formControl = new SpecificationFormDialog(ViewModel);

        var dialog = new ContentDialog()
        {
            PrimaryButtonText = Loc.Buttons_Create,
            CloseButtonText = Loc.Buttons_Cancel,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot
        };

        Binding operationsBinding = new()
        {
            Source = formControl.ViewModel,
            Path = new PropertyPath("IsPrimaryButtonEnabled"),
            Mode = BindingMode.OneWay
        };
        BindingOperations.SetBinding(dialog, ContentDialog.IsPrimaryButtonEnabledProperty, operationsBinding);

        Binding dynamicTitleBinding = new()
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
            await formControl.ViewModel.SaveSpecificationAsync();
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
