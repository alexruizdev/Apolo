using Apolo.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Models;
using System.Collections.ObjectModel;

namespace ViewModels
{
    public partial class SpecificationFormViewModel : BaseViewModel
    {
        private readonly SpecificationsViewModel _parentViewModel;

        // Dropdown Items Sources
        public ObservableCollection<StudentOption> Students => _parentViewModel.Students;
        public ObservableCollection<ServiceSummary> Services => _parentViewModel.Services;
        public ObservableCollection<StudentOption> FilteredStudents { get; } = new();

        // Form Fields
        [ObservableProperty] private StudentOption? _selectedStudent;
        [ObservableProperty] private ServiceSummary? _selectedService;

        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private double _duration = 60;
        [ObservableProperty] private bool _isOnline;
        [ObservableProperty] private bool _isWeekendOrHoliday;
        [ObservableProperty] private double _price = double.NaN;
        [ObservableProperty] private bool _isPricePerHour;

        // Dynamic UI Configurations
        [ObservableProperty] private string _priceHeader = "Price:";
        [ObservableProperty] private string _studentSearchText = string.Empty;
        [ObservableProperty] private bool _isPrimaryButtonEnabled;
        [ObservableProperty] private bool _isEditMode = false;

        [ObservableProperty] private string _dialogTitle = string.Empty;
        private Guid? _specificationId;

        public SpecificationFormViewModel(SpecificationsViewModel parentViewModel)
        {
            _parentViewModel = parentViewModel;
            IsEditMode = false;

            foreach (var student in Students) FilteredStudents.Add(student);

            Validate();
        }

        public SpecificationFormViewModel(SpecificationsViewModel parentViewModel, SpecificationSummary specification)
        {
            _parentViewModel = parentViewModel;

            IsEditMode = true;
            SelectedStudent = Students.First(s => s.Id == specification.StudentId);
            SelectedService = Services.First(s => s.Id == specification.ServiceId);
            Duration = specification.DurationMinutes;
            if (specification.Price != null)
                Price = specification.Price.Value;
            IsOnline = specification.IsOnline;
            IsWeekendOrHoliday = specification.IsWeekendOrHoliday;
            Name = specification.SpecificationName;

            _specificationId = specification.Id;

            Validate();
        }

        // --- CASCADING LOGIC ---

        partial void OnSelectedServiceChanged(ServiceSummary? value)
        {
            UpdateServiceDerivedFields(null);

            Validate();
        }

        partial void OnStudentSearchTextChanged(string value)
        {
            FilteredStudents.Clear();

            if (string.IsNullOrWhiteSpace(value))
            {
                SelectedStudent = null;

                foreach (var student in Students) FilteredStudents.Add(student);
                return;
            }

            // Perform case-insensitive containment filter
            var matches = Students.Where(s => s.FullName.Contains(value, StringComparison.OrdinalIgnoreCase));
            foreach (var student in matches)
            {
                FilteredStudents.Add(student);
            }

            if (SelectedStudent != null && !string.Equals(SelectedStudent.FullName, value, StringComparison.OrdinalIgnoreCase))
            {
                SelectedStudent = null;
            }
        }

        partial void OnIsPricePerHourChanged(bool value)
        {
            PriceHeader = IsPricePerHour ? "Price/Hour:" : "Price:";
        }

        partial void OnSelectedStudentChanged(StudentOption? value) => Validate();
        partial void OnPriceChanged(double value) => Validate();
        partial void OnIsWeekendOrHolidayChanged(bool value) => Validate();
        partial void OnIsOnlineChanged(bool value) => Validate();
        partial void OnDurationChanged(double value) => Validate();
        partial void OnNameChanged(string value) => Validate();

        // --- UPDATE FINAL PRICE ---

        private void UpdateServiceDerivedFields(double? forcedPrice)
        {
            SetEnterFunction();

            if (SelectedService is ServiceSummary s)
            {
                Name = s.Name;
                Price = double.NaN;
                IsPricePerHour = s.IsPricePerHour;

            }
            else
            {
                Price = double.NaN;
                IsPricePerHour = false;
            }
            SetExitFunction();
        }

        private void UpdateDialogTitle(bool errors)
        {
            string title = IsEditMode ? "Edit" : "New";
            if (SelectedService == null || errors)
            {
                DialogTitle = $"{title} Specification — Total: €-.--";
                return;
            }

            // Convert double properties to decimal for precise currency calculation
            decimal basePrice = (decimal)SelectedService.Price;
            if (!double.IsNaN(Price))
                basePrice = (decimal)Price;

            decimal finalPrice = Lesson.GetPrice(IsOnline, _parentViewModel.TravelAllowance,
                IsWeekendOrHoliday, _parentViewModel.WeekendFee,
                basePrice, IsPricePerHour, (int)Duration);


            // Update the string property bound to the dialog title
            DialogTitle = $"{title} Specification — Total: {finalPrice:C2}";
        }


        // --- VALIDATION LOGIC ---

        private void Validate()
        {
            SetEnterFunction();

            var errors = new List<string>();

            IsPricePerHour = SelectedService != null && SelectedService.IsPricePerHour;

            if (!IsEditMode)
                if (SelectedStudent == null)
                    errors.Add("• Select one student.");

            if (SelectedService == null)
                errors.Add("• Select a service.");
            if (IsPricePerHour && (double.IsNaN(Duration) || Duration <= 0))
                errors.Add("• Duration must be a positive integer.");
            if (!double.IsNaN(Price) && Price < 0)
                errors.Add("• Price must be a positive integer.");
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add("• Specification name cannot be empty.");

            IsPrimaryButtonEnabled = !errors.Any();

            UpdateDialogTitle(!IsPrimaryButtonEnabled);

            if (IsPrimaryButtonEnabled)
                SetExitFunction();
            else
                SetExitFunction(string.Join(Environment.NewLine, errors), InfoBarType.Error);
        }

        // --- SAVE EXECUTION ---

        public int GetDuration() => (int)Duration;

        public double? GetBasePrice() => double.IsNaN(Price) || Price <= 0 ? null: Price; 

        public async Task SaveSpecificationAsync()
        {
            if (SelectedStudent == null || SelectedService == null)
                throw new ArgumentException("Student or service is null");

            await _parentViewModel.AddSpecificationAsync(Name, GetDuration(), Price, IsOnline, IsWeekendOrHoliday,
                SelectedStudent.Id, SelectedService.Id);
        }

        public async Task EditSpecificationAsync()
        {
            if (_specificationId is null)
                throw new InvalidDataException("Specification doesn't have Id.");

            if (SelectedService is null)
                throw new InvalidCastException("Service is not selected to edit specification.");

            await _parentViewModel.UpdateSpecificationAsync(_specificationId.Value, Name, GetDuration(), GetBasePrice(), IsOnline,
                IsWeekendOrHoliday, SelectedService.Id);
        }
    }
}
