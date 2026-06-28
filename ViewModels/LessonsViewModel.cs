using Apolo.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;
using ViewModels;

namespace Apolo.ViewModels
{
    public partial class LessonsViewModel : UserProfileViewModel
    {
        ILessonRepository _lessonRepository;
        IStudentRepository _studentRepository;
        IServiceRepository _serviceRepository;
        ISpecificationRepository _specificationRepository;

        public ObservableCollection<LessonSummary> Lessons { get; } = new();
        public ObservableCollection<StudentOption> Students { get; } = new();
        public ObservableCollection<ServiceSummary> Services { get; } = new();

        // Filter
        [ObservableProperty] private string _filterStudentName = string.Empty;
        [ObservableProperty] private string _filterPayerName = string.Empty;
        [ObservableProperty] private int _selectedPaymentStatusIndex = 0; // 0 = All, 1 = Paid, 2 = Unpaid
        [ObservableProperty] private DateTimeOffset? _filterStartDate;
        [ObservableProperty] private DateTimeOffset? _filterEndDate;
        [ObservableProperty] private bool _areFiltersActive;
        public bool CheckFilters =>
            !string.IsNullOrWhiteSpace(FilterStudentName) ||
            !string.IsNullOrWhiteSpace(FilterPayerName) ||
            SelectedPaymentStatusIndex != 0 ||
            FilterStartDate.HasValue ||
            FilterEndDate.HasValue;


        public LessonsViewModel(ILessonRepository lessonRepository, IStudentRepository studentRepository, 
            IServiceRepository serviceRepository, ISpecificationRepository specificationRepository, IUserProfileService userProfile,
            IStringLocalizer stringLocalizer)
            : base(userProfile, stringLocalizer)
        {
            _lessonRepository = lessonRepository;
            _studentRepository = studentRepository;
            _serviceRepository = serviceRepository;
            _specificationRepository = specificationRepository;
            FilterStartDate = DateTimeOffset.Now.AddMonths(-2);
        }

        protected static string Message_Load_Error => "Messages/Load_Lesson_Error";
        protected static string Message_Load_Success => "Messages/Load_Lesson_Success";
        protected static string Message_Add_Error => "Messages/Add_Lesson_Error";
        protected static string Message_Add_Success => "Messages/Add_Lesson_Success";
        protected static string Message_Delete_Error => "Messages/Delete_Lesson_Error";
        protected static string Message_Lesson_Assigned => "Messages/LessonIsAssigned";
        protected static string Message_Delete_Success => "Messages/Delete_Lesson_Success";
        protected static string Message_Edit_Error => "Messages/Edit_Lesson_Error";
        protected static string Message_Edit_Success => "Messages/Edit_Lesson_Success";
        protected static string Message_Clear_Filters_Error => "Messages/Clear Filters_Error";

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Load_Error);
                return;
            }

            SetEnterFunction();

            var studentsItem = await _studentRepository.GetStudentOptionsAsync();

            Students.Clear();
            foreach (var item in studentsItem) Students.Add(item);

            var serviceItems = await _serviceRepository.GetServicesAsync();

            Services.Clear();
            foreach (var s in serviceItems) Services.Add(s);

            // Filters

            // 1. Map ComboBox Index (0=All, 1=Paid, 2=Unpaid) to nullable bool
            bool? repoIsPaid = SelectedPaymentStatusIndex switch
            {
                1 => true,
                2 => false,
                _ => null
            };

            // 2. Map DateTimeOffset? from WinUI DatePickers to EF-friendly DateOnly?
            DateOnly? repoStartDate = FilterStartDate.HasValue
                ? DateOnly.FromDateTime(FilterStartDate.Value.DateTime)
                : null;

            DateOnly? repoEndDate = FilterEndDate.HasValue
                ? DateOnly.FromDateTime(FilterEndDate.Value.DateTime)
                : null;

            var items = await _lessonRepository.GetLessonsAsync(FilterStudentName,
                FilterPayerName,
                repoIsPaid,
                repoStartDate,
                repoEndDate);

            Lessons.Clear();
            foreach (var item in items) Lessons.Add(item);

            AreFiltersActive = CheckFilters;

            SetExitFunction($"{_loc.Get(Message_Load_Success, Lessons.Count)}.", InfoBarType.Success);
        }

        public (StudentOption item, int index) GetStudent(Guid id)
        {
            var student = Students.FirstOrDefault(s => s.Id == id);
            if (student is null)
            {
                SetExitFunction();
                throw new InvalidDataException(_loc.Get(Message_Student_Not_Loaded));
            }
            return (student, Students.IndexOf(student));
        }

        public bool ValidateLesson(LessonSummary l)
        {
            var errors = new List<string>();

            if (l.IsPaid)
                errors.Add(_loc.Get(Message_LessonPaidValidation));
            if (l.BillingDocumentId != null)
                errors.Add(_loc.Get(Message_LessonBillValidation));

            if (errors.Count == 0)
                return true;

            SetExitFunction(string.Join(Environment.NewLine, errors), InfoBarType.Warning);
            return false;
        }
        public bool ValidateLessonInput(ref string name, ref int? duration, bool isPricePerHour, decimal basePrice, decimal tip)
        {
            var errors = new List<string>();

            name = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                errors.Add(_loc.Get(Message_LessonNameValidation));

            if (tip < 0)
                errors.Add(_loc.Get(Message_TipValidation));

            if (isPricePerHour)
            {
                if (duration is null)
                    errors.Add(_loc.Get(Message_DurationValidation));
                
                if (duration <= 0)
                    errors.Add(_loc.Get(Message_DurationValueValidation));
            }
            else
            {
                duration = null; // Normalize to null for easier handling in the database and UI
            }

            if (basePrice <= 0)
                errors.Add(_loc.Get(Message_PriceValidation));

            if (errors.Count == 0)
                return true;

            SetExitFunction(string.Join(Environment.NewLine, errors), InfoBarType.Warning);
            return false;
        }

        public (LessonSummary lesson, int index) GetLesson(Guid id) 
        {
            var lesson = Lessons.FirstOrDefault(l => l.Id == id);
            if (lesson is null)
            {
                SetExitFunction();
                throw new InvalidDataException(_loc.Get(Message_Lesson_Not_Loaded));
            }
            return (lesson, Lessons.IndexOf(lesson));
        }

        public async Task AddLessonAsync(DateOnly date, string name, ServiceSummary service,
            int? duration, decimal pricePerLesson, bool isOnline, bool isWeekendOrHoliday, decimal tip,
            string? note, Guid studentId)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Add_Error);
                return;
            }

            SetEnterFunction();

            var student = GetStudent(studentId);

            if (!ValidateLessonInput(ref name, ref duration, service.IsPricePerHour, pricePerLesson, tip))
                return; 

            try
            {
                var lesson = await _lessonRepository.AddLessonAsync(date, name, isPaid: false, studentId, null,
                    service.IsPricePerHour, duration, pricePerLesson,
                    isOnline, TravelAllowance, isWeekendOrHoliday, WeekendFee, tip, note);

                // Add to UI
                Lessons.Insert(0, new LessonSummary(
                    lesson.Id,
                    lesson.Date,
                    lesson.Name,
                    lesson.FinalPrice,
                    lesson.IsPaid,
                    lesson.StudentId,
                    student.item.FullName,
                    lesson.BillingDocumentId,
                    string.Empty, 
                    lesson.IsPricePerHour,
                    lesson.DurationMinutes,
                    lesson.BasePrice,
                    lesson.IsOnline,
                    lesson.TravelAllowance,
                    lesson.IsWeekendOrHoliday,
                    lesson.WeekendFee,
                    lesson.Tip,
                    lesson.Notes));
                
                SetExitFunction($"{_loc.Get(Message_Add_Success)}: '{lesson.Name}'.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task ChangePayment(Guid id)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Change_Payment_Error);
                return;
            }

            SetEnterFunction();

            var (item, idx) = GetLesson(id);
            try
            {
                await _lessonRepository.UpdateLessonsPayment(new List<Guid> { id }, !item.IsPaid);
                Lessons[idx] = item with { IsPaid = !item.IsPaid };
                if (Lessons[idx].IsPaid)
                    SetExitFunction($"{_loc.Get(Message_Mark_Paid, item.Name)}.", InfoBarType.Success);
                else
                    SetExitFunction($"{_loc.Get(Message_Mark_Unpaid, item.Name)}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task DeleteLessonAsync(Guid id)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Delete_Error);
                return;
            }

            SetEnterFunction();

            var (oldItem, idx) = GetLesson(id);

            if (oldItem.BillingDocumentId is not null)
            {
                SetExitFunction($"{_loc.Get(Message_Delete_Error)}: {_loc.Get(Message_Lesson_Assigned, oldItem.Name, oldItem.StudentName, oldItem.BillingName)}'{oldItem.Name}'.", InfoBarType.Error);
                return;
            }

            try
            {
                await _lessonRepository.DeleteAsync(id);
                Lessons.Remove(oldItem);
                SetExitFunction($"{_loc.Get(Message_Delete_Success, oldItem.Name, oldItem.StudentName)}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task UpdateLessonAsync(Guid id, DateOnly date, string name,
            bool isPricePerHour, int? duration, decimal basePrice,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, decimal tip, string? note)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Edit_Error);
                return;
            }

            SetEnterFunction();

            var (oldItem, idx) = GetLesson(id);

            // Check if lesson can be edit
            if (!ValidateLesson(oldItem))
                return;

            // Check if new values are valid
            if (!ValidateLessonInput(ref name, ref duration, isPricePerHour, basePrice, tip))
                return;

            try
            {
                var entity = await _lessonRepository.UpdateLesson(id, date, name, 
                    isPricePerHour, duration, basePrice,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, tip, note);

                // Update item in UI list
                Lessons[idx] = oldItem with
                {
                    Name = entity.Name,
                    Date = entity.Date,
                    FinalPrice = entity.FinalPrice,
                    IsPricePerHour = entity.IsPricePerHour,
                    DurationMinutes = entity.DurationMinutes,
                    BasePrice = entity.BasePrice,
                    IsOnline = entity.IsOnline,
                    TravelAllowance = entity.TravelAllowance,
                    IsWeekendOrHoliday = entity.IsWeekendOrHoliday,
                    WeekendFee = entity.WeekendFee,
                    Tip = entity.Tip,
                    Notes = entity.Notes,
                };
                SetExitFunction($"{_loc.Get(Message_Edit_Success, entity.Name)}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task<IEnumerable<SpecificationOption>> GetSpecificationOptionsAsync(List<Guid> studentsIds)
        {
            return await _specificationRepository.GetSpecificationsForStudentAsync(studentsIds);
        }

        [RelayCommand]
        public async Task ClearFiltersAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Clear_Filters_Error);
                return;
            }

            SetEnterFunction();

            FilterStudentName = string.Empty;
            FilterPayerName = string.Empty;
            SelectedPaymentStatusIndex = 0;
            FilterStartDate = null;
            FilterEndDate = null;

            SetExitFunction();

            await LoadAsync();
        }
}}
