using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Models;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            var studentsList = new ListView
            {
                Header = "Students",
                SelectionMode = ListViewSelectionMode.Multiple,
                ItemsSource = ViewModel.Students
            };
            studentsList.DisplayMemberPath = "FullName";

            var specificationBox = new ComboBox
            {
                Header = "Specification (optional)",
                IsEnabled = false,
                DisplayMemberPath = "Display",
                SelectedValuePath = "Id"
            };

            var serviceBox = new ComboBox
            {
                Header = "Service",
                ItemsSource = ViewModel.Services,
                SelectedValuePath = "Name",
                DisplayMemberPath = "Name"
            };

            var nameBox = new TextBox { Header = "Lesson name (from service)", IsReadOnly = true, MinWidth = 320 };

            var datePick = new CalendarDatePicker { Header = "Date", IsTodayHighlighted = true, Date = DateTimeOffset.Now };
            var durationBox = new NumberBox { Header = "Duration (minutes):", Value = 60, SmallChange = 15, LargeChange = 30 };
            var onlineBox = new CheckBox { Content = "Online" };
            var priceBox = new NumberBox { Header = "Total Price to distribute:", PlaceholderText = "0.00" };

            var error = new TextBlock
            {
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.IndianRed),
                Visibility = Visibility.Collapsed,
                TextWrapping = TextWrapping.Wrap
            };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(studentsList);
            panel.Children.Add(specificationBox);
            panel.Children.Add(serviceBox);
            panel.Children.Add(datePick);
            panel.Children.Add(durationBox);
            panel.Children.Add(onlineBox);
            panel.Children.Add(priceBox);
            panel.Children.Add(error);

            var dialog = new ContentDialog()
            {
                Title = "Create lesson",
                Content = panel,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            // Guard to avoid event recursion
            bool updating = false;

            async Task RefreshSpecificationsAsync ()
            {
                var ids = studentsList.SelectedItems.Cast<StudentOption>().Select(s => s.Id).ToList();
                if (ids.Count == 0)
                {
                    specificationBox.ItemsSource = null;
                    specificationBox.IsEnabled = false;
                    return;
                }
                var specifications = await ViewModel.GetSpecificationOptionsAsync(ids);
                specificationBox.ItemsSource = specifications;
                specificationBox.IsEnabled = specifications.Any();
            }

            void UpdateServiceDerivedFields()
            {
                if (updating) return;
                if (serviceBox.SelectedItem is ServiceSummary s)
                {
                    updating = true;
                    nameBox.Text = s.Name;
                    priceBox.Value = (double)s.PricePerHour;
                    updating = false;
                }
                else
                {
                    updating = true;
                    nameBox.Text = string.Empty;
                    priceBox.Value = 0;
                    updating = false;
                }
            }

            specificationBox.SelectionChanged += (_, __) =>
            {
                if (updating) return;
                if (specificationBox.SelectedItem is not SpecificationOption sp) return;

                updating = true;

                // Select the service that the specification uses (this also updates name/price via UpdateServiceDerivedFields)
                var service = ViewModel.Services.FirstOrDefault(x => x.Id == sp.ServiceId);
                serviceBox.SelectedItem = service;

                // Override the form controls
                durationBox.Value = sp.DurationMinutes;
                onlineBox.IsChecked = sp.IsOnline;

                updating = false;

                // Ensure name/price reflects the service choice
                UpdateServiceDerivedFields();
            };

            // If the user changes Service manually after picking a specification, keep their choice.
            serviceBox.SelectionChanged += (_, __) =>
            {
                if (updating) return;
                UpdateServiceDerivedFields();

                // Clear specification selection to avoid confusion
                updating = true;
                specificationBox.SelectedItem = null;
                updating = false;
            };

            studentsList.SelectionChanged += async (_, __) =>
            {
                // reload specifications for current selection
                await RefreshSpecificationsAsync();
            };

            void Validate()
            {
                error.Text = string.Empty;
                error.Visibility = Visibility.Collapsed;

                if (studentsList.SelectedItems.Count == 0)
                    error.Text = "Select at least one student.";
                else if (serviceBox.SelectedItem is not ServiceSummary)
                    error.Text = "Select a service.";
                else if (double.IsNaN(durationBox.Value) || durationBox.Value <= 0)
                    error.Text = "Duration must be a positive integer.";

                if (!string.IsNullOrEmpty(error.Text))
                    error.Visibility = Visibility.Visible;

                dialog.IsPrimaryButtonEnabled = error.Visibility == Visibility.Collapsed;
            }

            durationBox.ValueChanged += (_, __) => Validate();
            serviceBox.SelectionChanged += (_, __) => Validate();
            studentsList.SelectionChanged += (_, __) => Validate();

            dialog.Loaded += async (_, __) =>
            {
                if (ViewModel.Services.Count > 0) serviceBox.SelectedIndex = 0;
                UpdateServiceDerivedFields();
                Validate();
                await RefreshSpecificationsAsync();
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var selectedIds = studentsList.SelectedItems.Cast<StudentOption>().Select(s => s.Id).ToList();
                var dto = datePick.Date ?? DateTimeOffset.Now;
                var date = DateOnly.FromDateTime(dto.Date);
                await ViewModel.CreateLessonAsync((string)serviceBox.SelectedValue, date, (int)durationBox.Value, onlineBox.IsChecked == true, (decimal)priceBox.Value, selectedIds);
            }
        }

        private async void EditLesson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.DataContext is not LessonSummary row) return;

            var nameBox = new TextBox { Header = "Name", Text = row.Name, MinWidth = 320, MaxLength = 120 };
            var datePick = new CalendarDatePicker
            {
                Header = "Date",
                IsTodayHighlighted = true,
                Date = new DateTimeOffset(row.Date.ToDateTime(TimeOnly.MinValue))
            };
            var durationBox = new NumberBox { Header = "Duration (minutes):", Value = row.DurationMinutes, SmallChange = 15, LargeChange = 30 };
            var onlineBox = new CheckBox { Content = "Online", IsChecked = row.IsOnline };
            var priceBox = new NumberBox
            {
                Header = "Price per hour:",
                Value = (double)row.PricePerStudent,
                PlaceholderText = "0.00"
            };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(nameBox);
            panel.Children.Add(datePick);
            panel.Children.Add(durationBox);
            panel.Children.Add(onlineBox);
            panel.Children.Add(priceBox);

            var dialog = new ContentDialog()
            {
                Title = "Edit lesson",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var dto = datePick.Date ?? new DateTimeOffset(row.Date.ToDateTime(TimeOnly.MinValue));
                var date = DateOnly.FromDateTime(dto.Date);
                await ViewModel.UpdateLessonAsync(
                    row.Id, 
                    nameBox.Text, 
                    date, 
                    (int)durationBox.Value, 
                    onlineBox.IsChecked == true, 
                    (decimal)priceBox.Value);
            }
        }

        private async void AddAttendances_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.DataContext is not LessonSummary row) return;

            // choose students not already in this lesson
            var existingIds = row.Attendances.Select(a => a.StudentId).ToHashSet();
            var options = ViewModel.Students.Where(s => !existingIds.Contains(s.Id)).ToList();

            var list = new ListView
            {
                Header = "Students",
                SelectionMode = ListViewSelectionMode.Multiple,
                ItemsSource = options
            };
            list.DisplayMemberPath = "FullName";
            var error = new TextBlock
            {
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.IndianRed),
                Visibility = Visibility.Collapsed
            };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(list);
            panel.Children.Add(error);

            var dialog = new ContentDialog
            {
                Title = "Add attendances",
                Content = panel,
                PrimaryButtonText = "Add",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            void UpdateUI()
            {
                error.Visibility = Visibility.Collapsed;
                error.Text = string.Empty;
                if (list.SelectedItems.Count == 0)
                {
                    error.Text = "Select at least one customer.";
                    error.Visibility = Visibility.Visible;
                }
                dialog.IsPrimaryButtonEnabled = error.Visibility == Visibility.Collapsed;
            }

            list.SelectionChanged += (_, __) => UpdateUI();
            dialog.Loaded += (_, __) => UpdateUI();

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var ids = list.SelectedItems.Cast<StudentOption>().Select(x => x.Id).ToList();
                await ViewModel.AddAttendanceAsync(row.Id, ids);
            }
        }

        private async void EditAttendances_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.DataContext is not AttendanceSummary attendance) return;

            // find parent lesson id
            if (b.Parent as FrameworkElement is null) return;

            var lesson = FindAncestorDataContext<LessonSummary>(b);
            if (lesson is null) return;

            var paidBox = new CheckBox { Content = "Paid", IsChecked = attendance.IsPaid };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(paidBox);

            var dialog = new ContentDialog
            {
                Title = "Edit attendance",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.UpdateAttendanceAsync(lesson.Id, attendance.Id, paidBox.IsChecked == true);
            }
        } 

        private async void RemoveAttendance_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.DataContext is not AttendanceSummary attendance)
                return;
            var lesson = FindAncestorDataContext<LessonSummary>(b);
            if (lesson is null) return;

            var dialog = new ContentDialog()
            {
                Title = "Delete attendace?",
                Content = $"Remove {attendance.StudentName} from '{lesson.Name}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.RemoveAttendanceAsync(lesson.Id, attendance.Id);
            }
        }

        private static T? FindAncestorDataContext<T>(FrameworkElement child) where T : class
        {
            // Climb visual tree to find the ListViewItem for LEssonSummary
            DependencyObject? current = child;
            while (current is not null)
            {
                if (current is FrameworkElement fe && fe.DataContext is T t)
                    return t;
                current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
            }
            return null;
        }
    }
}
