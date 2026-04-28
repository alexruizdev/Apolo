using Apolo.Service;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
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
                ItemsSource = ViewModel.Students,
                MaxHeight = 240
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

            var nameBox = new TextBox { Header = "Lesson name", MinWidth = 320 };

            var datePick = new CalendarDatePicker { Header = "Date", IsTodayHighlighted = true, Date = DateTimeOffset.Now };
            var durationBox = new NumberBox { Header = "Duration (minutes):", Value = 60, SmallChange = 15, LargeChange = 30 };
            var onlineBox = new CheckBox { Content = "Online" };
            var weekendBox = new CheckBox { Content = "Weekend or Holiday" };
            var priceBox = new NumberBox { Header = "Price:", PlaceholderText = "0.00" };

            var noteBox = new TextBox
            {
                Header = "Notes",
                MinWidth = 400,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };

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
            panel.Children.Add(nameBox);
            panel.Children.Add(datePick);
            panel.Children.Add(durationBox);
            panel.Children.Add(onlineBox);
            panel.Children.Add(weekendBox);
            panel.Children.Add(priceBox);
            panel.Children.Add(error);
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

            void UpdateServiceDerivedFields(double? price)
            {
                if (updating) return;
                if (serviceBox.SelectedItem is ServiceSummary s)
                {
                    updating = true;
                    nameBox.Text = s.Name;
                    priceBox.Value =  price ?? s.Price;
                    priceBox.Header = s.IsPricePerHour ? "Price/Hour:" : "Price:" ;
                    updating = false;
                }
                else
                {
                    updating = true;
                    nameBox.Text = string.Empty;
                    priceBox.Value = 0;
                    priceBox.Header = "Price:";
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
                weekendBox.IsChecked = sp.IsWeekend;

                updating = false;

                // Ensure name/price reflects the service choice
                UpdateServiceDerivedFields(sp.Price);
            };

            // If the user changes Service manually after picking a specification, keep their choice.
            serviceBox.SelectionChanged += (_, __) =>
            {
                if (updating) return;
                UpdateServiceDerivedFields(null);

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
                else if (((ServiceSummary)serviceBox.SelectedItem).IsPricePerHour &&
                    (double.IsNaN(durationBox.Value) || durationBox.Value <= 0))
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
                UpdateServiceDerivedFields(null);
                Validate();
                await RefreshSpecificationsAsync();
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var selectedIds = studentsList.SelectedItems.Cast<StudentOption>().Select(s => s.Id).ToList();
                var dto = datePick.Date ?? DateTimeOffset.Now;
                var date = DateOnly.FromDateTime(dto.Date);
                bool isOnline = onlineBox.IsChecked == true;
                bool isWeekend = weekendBox.IsChecked == true;
                await ViewModel.CreateLessonAsync(date, nameBox.Text, (ServiceSummary)serviceBox.SelectedItem, 
                    (int?)durationBox.Value, (decimal)priceBox.Value,
                    isOnline, isWeekend, noteBox.Text, selectedIds);
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
            var durationBox = new NumberBox { Header = "Duration (minutes):", Value = row.DurationMinutes ?? 0, SmallChange = 15, LargeChange = 30, IsEnabled = row.IsPricePerHour };
            var onlineBox = new CheckBox { Content = "Online", IsChecked = row.IsOnline };
            var travelAllowanceBox = new NumberBox { Header = "Travel allowance:", Value = (double)row.TravelAllowance, IsEnabled = !row.IsOnline };
            var weekenBox = new CheckBox { Content = "Weekend or Holiday", IsChecked = row.IsWeekenOrHoliday };
            var weekendFeeBox = new NumberBox { Header = "Weekend or Holiday Fee:", Value = (double)row.TravelAllowance, IsEnabled = !row.IsOnline };
            var isPricePerHourBox = new CheckBox { Content = "Price/hour", IsChecked = row.IsPricePerHour };
            var priceBox = new NumberBox
            {
                Header = "Price:",
                Value = (double)row.PricePerAttendance,
                PlaceholderText = "0.00"
            };
            string note = row.Notes ?? string.Empty;

            var noteBox = new TextBox
            {
                Header = "Notes",
                Text = note,
                MinWidth = 400,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap
            };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(nameBox);
            panel.Children.Add(datePick);
            panel.Children.Add(isPricePerHourBox);
            panel.Children.Add(durationBox);
            panel.Children.Add(priceBox);
            panel.Children.Add(onlineBox);
            panel.Children.Add(travelAllowanceBox);
            panel.Children.Add(weekenBox);
            panel.Children.Add(weekendFeeBox);
            panel.Children.Add(noteBox);

            isPricePerHourBox.Checked += (_, __) => durationBox.IsEnabled = true;
            isPricePerHourBox.Unchecked += (_, __) => durationBox.IsEnabled = false;
            onlineBox.Checked += (_, __) => travelAllowanceBox.IsEnabled = false;
            onlineBox.Unchecked += (_, __) => travelAllowanceBox.IsEnabled = true;
            weekenBox.Checked += (_, __) => weekendFeeBox.IsEnabled = true;
            weekenBox.Unchecked += (_, __) => weekendFeeBox.IsEnabled = false;

            var viewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Enabled,
                MaxHeight = 500,
                Content = panel
            };

            var dialog = new ContentDialog()
            {
                Title = "Edit lesson",
                Content = viewer,
                PrimaryButtonText = "Save",
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var dto = datePick.Date ?? new DateTimeOffset(row.Date.ToDateTime(TimeOnly.MinValue));
                var date = DateOnly.FromDateTime(dto.Date);
                bool isPricePerHour = isPricePerHourBox.IsChecked == true;
                int? duration = isPricePerHour ? null : (int)durationBox.Value;
                await ViewModel.UpdateLessonAsync(
                    row.Id, date, nameBox.Text, 
                    isPricePerHour, duration, (decimal)priceBox.Value,
                    onlineBox.IsChecked == true, (decimal)travelAllowanceBox.Value,
                    weekenBox.IsChecked == true, (decimal)weekendFeeBox.Value,
                    noteBox.Text);
            }
        }

        private async void Notes_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b || b.DataContext is not LessonSummary row) return;

            var noteBox = new TextBox { 
                Header = "Notes", 
                MinWidth = 400, 
                AcceptsReturn = true, 
                TextWrapping = TextWrapping.Wrap 
            };

            noteBox.Text = row.Notes;

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(noteBox);

            var viewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Enabled,
                MaxHeight = 500,
                Content = panel
            };

            var dialog = new ContentDialog
            {
                Title = $"{row.Name} Notes",
                Content = viewer,
                PrimaryButtonText = "Save",
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.UpdateLessonNoteAsync(row.Id, noteBox.Text);
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
                ItemsSource = options,
                MaxHeight = 240
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

            var viewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollMode = ScrollMode.Enabled,
                MaxHeight = 500,
                Content = panel
            };

            var dialog = new ContentDialog
            {
                Title = "Add attendances",
                Content = viewer,
                PrimaryButtonText = "Add",
                CloseButtonText = Loc.Buttons_Cancel,
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
            if (sender is not Button b)
                return;
            if (b.CommandParameter is not AttendanceSummary attendance) 
                return;
            if (b.DataContext is not LessonSummary lesson)
                return;

            // find parent lesson id
            if (b.Parent as FrameworkElement is null) return;

            var paidBox = new CheckBox { Content = "Paid", IsChecked = attendance.IsPaid };

            var panel = new StackPanel { Spacing = 8 };
            panel.Children.Add(paidBox);

            var dialog = new ContentDialog
            {
                Title = "Edit attendance",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = Loc.Buttons_Cancel,
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
            if (sender is not Button b)
                return;
            if (b.CommandParameter is not AttendanceSummary attendance)
                return;
            if (b.DataContext is not LessonSummary lesson)
                return;

            var dialog = new ContentDialog()
            {
                Title = "Delete attendace?",
                Content = $"Remove {attendance.StudentName} from '{lesson.Name}'?",
                PrimaryButtonText = Loc.Buttons_Delete,
                CloseButtonText = Loc.Buttons_Cancel,
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ViewModel.RemoveAttendanceAsync(lesson.Id, attendance.Id);
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
