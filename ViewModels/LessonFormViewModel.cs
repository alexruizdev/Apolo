using Apolo.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Models;
using System.Collections.ObjectModel;
using System.Globalization;

namespace ViewModels
{
    public partial class LessonFormViewModel : BaseViewModel
    {

        private readonly LessonsViewModel _parentViewModel;

        // Dropdown Items Sources
        public ObservableCollection<StudentOption> Students => _parentViewModel.Students;
        public ObservableCollection<ServiceSummary> Services => _parentViewModel.Services;

        public ObservableCollection<SpecificationOption> Specifications = [];
        public ObservableCollection<StudentOption> FilteredStudents { get; } = [];

        // Form Fields
        [ObservableProperty] private StudentOption? _selectedStudent;
        [ObservableProperty] private SpecificationOption? _selectedSpecification;
        [ObservableProperty] private ServiceSummary? _selectedService;

        [ObservableProperty] private string _name = string.Empty;
        [ObservableProperty] private DateTimeOffset _date = DateTimeOffset.Now;
        [ObservableProperty] private double _duration = 60;
        [ObservableProperty] private bool _isOnline;
        [ObservableProperty] private bool _isWeekendOrHoliday;
        [ObservableProperty] private double _price;
        [ObservableProperty] private double _tip;
        [ObservableProperty] private string _notes = string.Empty;
        [ObservableProperty] private bool _isPricePerHour;
        [ObservableProperty] private double _travelAllowance;
        [ObservableProperty] private double _weekendFee;

        // Dynamic UI Configurations
        [ObservableProperty] private string _priceHeader;
        [ObservableProperty] private string _studentSearchText = string.Empty;
        [ObservableProperty] private bool _isSpecificationEnabled;
        [ObservableProperty] private bool _isPrimaryButtonEnabled;
        [ObservableProperty] private bool _isEditMode = false;

        [ObservableProperty] private string _dialogTitle = string.Empty;
        private readonly Guid? _studentId;

        // Messages
        protected static string Message_Edit_Title => "Message/Edit_Lesson";
        protected static string Message_New_Title => "Message/New_Lesson";

        public LessonFormViewModel(LessonsViewModel parentViewModel)
            : base(parentViewModel._loc)
        {
            _parentViewModel = parentViewModel;
            IsEditMode = false;

            TravelAllowance = _parentViewModel.Profile.TravelAllowance;
            WeekendFee = _parentViewModel.Profile.WeekendFee;

            PriceHeader = _loc.Get(Header_Price);

            foreach (var student in Students) FilteredStudents.Add(student);

            // Set initial defaults
            if (Services.Count > 0)
                SelectedService = Services[0];

            Validate();
        }

        public LessonFormViewModel(LessonsViewModel parentViewModel, LessonSummary lesson)
            : base(parentViewModel._loc)
        {
            _parentViewModel = parentViewModel;

            PriceHeader = _loc.Get(Header_Price);

            IsEditMode = true;
            Name = lesson.Name;
            Date = new DateTimeOffset(lesson.Date.ToDateTime(TimeOnly.MinValue));
            Duration = lesson.DurationMinutes ?? 0;
            IsOnline = lesson.IsOnline;
            TravelAllowance = (double)lesson.TravelAllowance;
            IsWeekendOrHoliday = lesson.IsWeekendOrHoliday;
            WeekendFee = (double)lesson.WeekendFee;
            IsPricePerHour = lesson.IsPricePerHour;
            Price = (double)lesson.BasePrice;
            Tip = (double)lesson.Tip;
            Notes = lesson.Notes ?? string.Empty;

            _studentId = lesson.Id;

            Validate();
        }

        // --- CASCADING LOGIC ---

        async partial void OnSelectedStudentChanged(StudentOption? value)
        {
            SetEnterFunction();

            Specifications.Clear();

            if (value == null)
            {
                IsSpecificationEnabled = false;
                SetExitFunction();
                Validate();
                return;
            }

            var ids = new List<Guid> { value.Id };
            var specs = await _parentViewModel.GetSpecificationOptionsAsync(ids);

            foreach (var spec in specs) Specifications.Add(spec);

            IsSpecificationEnabled = Specifications.Any();
            SetExitFunction();

            Validate();
        }

        partial void OnSelectedSpecificationChanged(SpecificationOption? value)
        {
            if (value == null)
                return;

            SetEnterFunction();

            SelectedService = Services.FirstOrDefault(x => x.Id == value.ServiceId);
            Duration = value.DurationMinutes;
            IsOnline = value.IsOnline;
            IsWeekendOrHoliday = value.IsWeekend;

            SetExitFunction();

            UpdateServiceDerivedFields(value.Price);
            Validate();

        }

        partial void OnSelectedServiceChanged(ServiceSummary? value)
        {
            UpdateServiceDerivedFields(null);

            SetEnterFunction();

            SelectedSpecification = null; // Clear specification selection to avoid confusion

            SetExitFunction();

            Validate();
        }

        private void UpdateServiceDerivedFields(double? forcedPrice)
        {
            SetEnterFunction();

            if (SelectedService is ServiceSummary s)
            {
                Name = s.Name;
                Price = forcedPrice ?? (double)s.Price;
                IsPricePerHour = s.IsPricePerHour;
                
            }
            else
            {
                Name = string.Empty;
                Price = 0;
                IsPricePerHour = false;
            }
            SetExitFunction();
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

        partial void OnPriceChanged(double value) => Validate();
        partial void OnTipChanged(double value) => Validate();
        partial void OnIsWeekendOrHolidayChanged(bool value) => Validate();
        partial void OnIsOnlineChanged(bool value) => Validate();
        partial void OnDurationChanged(double value) => Validate();
        partial void OnNameChanged(string value) => Validate();

        partial void OnTravelAllowanceChanged(double value) => Validate();
        partial void OnWeekendFeeChanged(double value) => Validate();

        partial void OnIsPricePerHourChanged(bool value)
        {
            PriceHeader = IsPricePerHour ? PriceHeader = _loc.Get(Header_PricePerHour) : PriceHeader = _loc.Get(Header_Price); 
        }

        // --- UPDATE FINAL PRICE ---

        private void UpdateNewDialogTitle(bool errors)
        {
            if (SelectedService == null || errors)
            {
                DialogTitle = $"{_loc.Get(Message_New_Title, "€-.--")}";
                return;
            }

            // Convert double properties to decimal for precise currency calculation
            decimal basePrice = double.IsNaN(Price) ? 0 : (decimal)Price;

            decimal finalPrice = Lesson.GetPrice(IsOnline, _parentViewModel.TravelAllowance,
                IsWeekendOrHoliday, _parentViewModel.WeekendFee,
                basePrice, IsPricePerHour, (int)Duration);


            // Update the string property bound to the dialog title
            DialogTitle = $"{_loc.Get(Message_New_Title, finalPrice.ToString("C2", CultureInfo.CurrentCulture))}";
        }

        private void UpdateEditDialogTitle(bool errors)
        {
            if (errors)
            {
                DialogTitle = $"{_loc.Get(Message_Edit_Title, "€-.--")}";
                return;
            }

            // Convert double properties to decimal for precise currency calculation
            decimal basePrice = double.IsNaN(Price) ? 0 : (decimal)Price;

            decimal finalPrice = Lesson.GetPrice(IsOnline, (decimal)TravelAllowance,
                IsWeekendOrHoliday, (decimal)WeekendFee,
                basePrice, IsPricePerHour, (int)Duration);


            // Update the string property bound to the dialog title
            DialogTitle = $"{_loc.Get(Message_Edit_Title, finalPrice.ToString("C2", CultureInfo.CurrentCulture))}";
        }

        // --- VALIDATION LOGIC ---

        private void ValidateNewLesson(ref List<string> errors)
        {
            IsPricePerHour = SelectedService != null && SelectedService.IsPricePerHour;

            if (SelectedStudent == null)
                errors.Add(_loc.Get(Message_SelectStudentValidation));
            if (SelectedService == null)
                errors.Add(_loc.Get(Message_SelectServiceValidation));
        }

        private void Validate()
        {
            SetEnterFunction();

            var errors = new List<string>();

            if (!IsEditMode)
                ValidateNewLesson(ref errors);
            
            if (IsPricePerHour && (double.IsNaN(Duration) || Duration <= 0))
                errors.Add(_loc.Get(Message_DurationValueValidation));
            if (Price <= 0)
                errors.Add(_loc.Get(Message_PriceValidation));
            if (Tip < 0)
                errors.Add(_loc.Get(Message_TipValidation));
            if (string.IsNullOrWhiteSpace(Name))
                errors.Add(_loc.Get(Message_LessonNameValidation));

            IsPrimaryButtonEnabled = errors.Count == 0;

            if (IsEditMode)
                UpdateEditDialogTitle(!IsPrimaryButtonEnabled);
            else
                UpdateNewDialogTitle(!IsPrimaryButtonEnabled);

            if (IsPrimaryButtonEnabled)
                SetExitFunction();
            else
                SetExitFunction(string.Join(Environment.NewLine, errors), InfoBarType.Error);
        }

        // --- SAVE EXECUTION ---
        public DateOnly GetLessonDate() => DateOnly.FromDateTime(Date.DateTime);
        public int? GetDuration() => (int?)Duration;
        public decimal GetBasePrice() => (decimal)Price;
        public decimal GetTip() => double.IsNaN(Tip) ? 0 : (decimal)Tip;

        public async Task SaveLessonAsync()
        {
            if (SelectedStudent == null || SelectedService == null) return;

            await _parentViewModel.AddLessonAsync(
                GetLessonDate(),
                Name,
                SelectedService,
                GetDuration(),
                GetBasePrice(),
                IsOnline,
                IsWeekendOrHoliday,
                GetTip(),
                Notes,
                SelectedStudent.Id
            );
        }


        public async Task EditLessonAsync()
        {
            if (_studentId == null) return;

            await _parentViewModel.UpdateLessonAsync(
                _studentId.Value,
                GetLessonDate(),
                Name,
                IsPricePerHour,
                GetDuration(),
                GetBasePrice(),
                IsOnline,
                (decimal)TravelAllowance,
                IsWeekendOrHoliday,
                (decimal)WeekendFee,
                GetTip(),
                Notes
            );
        }
    }
}
