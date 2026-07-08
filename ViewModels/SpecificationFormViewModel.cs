using Apolo.Services;
using Apolo.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel.__Internals;
using Models;
using System.Collections.ObjectModel;
using System.Globalization;

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
        [ObservableProperty] private string _priceHeader = string.Empty;
        [ObservableProperty] private string _studentSearchText = string.Empty;
        [ObservableProperty] private bool _isPrimaryButtonEnabled;
        [ObservableProperty] private bool _isEditMode = false;

        [ObservableProperty] private string _dialogTitle = string.Empty;
        private Guid? _specificationId;

        // Messages 
        protected static string Message_Edit_Title => "Message/Edit_Specification";
        protected static string Message_New_Title => "Message/New_Specification";
        protected static string Message_Name_Validation => "Message/Specification_Name_Validation";
        protected static string Message_Save_Error => "Message/Save_Specification_Error";
        protected static string Message_Edit_Error => "Message/Edit_Specification_Error";
        protected static string Message_Edit_Service_Error => "Message/Edit_Specification_Service_Error";

        public SpecificationFormViewModel(SpecificationsViewModel parentViewModel)
            : base(parentViewModel._loc)
        {
            _parentViewModel = parentViewModel;
            IsEditMode = false;

            foreach (var student in Students) FilteredStudents.Add(student);

            Validate();
        }

        public SpecificationFormViewModel(SpecificationsViewModel parentViewModel, SpecificationSummary specification)
            : base(parentViewModel._loc)
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
            Name = specification.Name;

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
            PriceHeader = IsPricePerHour ? PriceHeader = _loc.Get(Header_PricePerHour) : PriceHeader = _loc.Get(Header_Price);
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
            if (SelectedService == null || errors)
            {
                if (IsEditMode)
                    DialogTitle = $"{_loc.Get(Message_Edit_Title, "€-.--")}";
                else
                    DialogTitle = $"{_loc.Get(Message_New_Title, "€-.--")}";
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
            if (IsEditMode)
                DialogTitle = $"{_loc.Get(Message_Edit_Title, finalPrice.ToString("C2", CultureInfo.CurrentCulture))}";
            else
                DialogTitle = $"{_loc.Get(Message_New_Title, finalPrice.ToString("C2", CultureInfo.CurrentCulture))}";
            return;
        }


        // --- VALIDATION LOGIC ---

        private void Validate()
        {
            SetEnterFunction();

            var errors = new List<string>();

            IsPricePerHour = SelectedService != null && SelectedService.IsPricePerHour;

            if (!IsEditMode)
                if (SelectedStudent == null)
                    errors.Add(_loc.Get(Message_SelectStudentValidation));

            if (SelectedService == null)
                errors.Add(_loc.Get(Message_SelectServiceValidation));
            if (IsPricePerHour && (double.IsNaN(Duration) || Duration <= 0))
                errors.Add(_loc.Get(Message_DurationValueValidation));
            if (!double.IsNaN(Price) && Price < 0)
                errors.Add(_loc.Get(Message_PriceValidation));
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add(_loc.Get(Message_Name_Validation));

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
                throw new ArgumentException(_loc.Get(Message_Save_Error));

            await _parentViewModel.AddSpecificationAsync(Name, GetDuration(), Price, IsOnline, IsWeekendOrHoliday,
                SelectedStudent.Id, SelectedService.Id);
        }

        public async Task EditSpecificationAsync()
        {
            if (_specificationId is null)
                throw new InvalidDataException($"{_loc.Get(Message_Edit_Error)}.");

            if (SelectedService is null)
                throw new InvalidCastException($"{_loc.Get(Message_Edit_Service_Error)}.");

            await _parentViewModel.UpdateSpecificationAsync(_specificationId.Value, Name, GetDuration(), GetBasePrice(), IsOnline,
                IsWeekendOrHoliday, SelectedService.Id);
        }
    }
}
