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
            IServiceRepository serviceRepository, ISpecificationRepository specificationRepository, IUserProfileService userProfile)
            : base(userProfile)
        {
            _lessonRepository = lessonRepository;
            _studentRepository = studentRepository;
            _serviceRepository = serviceRepository;
            _specificationRepository = specificationRepository;
            _userProfileService = userProfile;
            profile = userProfile.LoadProfileAsync().Result;
            FilterStartDate = DateTimeOffset.Now.AddMonths(-2);
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load lessons while busy.", InfoBarType.Warning, false);
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

            SetExitFunction($"{Lessons.Count} loaded", InfoBarType.Success);
        }

        public (StudentOption item, int index) GetStudent(Guid id)
        {
            var student = Students.FirstOrDefault(s => s.Id == id);
            if (student is null)
            {
                SetExitFunction();
                throw new InvalidDataException("Student not loaded.");
            }
            return (student, Students.IndexOf(student));
        }

        public bool ValidateLessonInput(ref string name, ref int? duration, bool isPricePerHour, decimal basePrice, decimal tip)
        {
            name = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                SetExitFunction("Lesson name is required.", InfoBarType.Warning);
                return false;
            }

            if (tip < 0)
            {
                SetExitFunction("Enter a valid non-negative tip (e.g., 15.5).", InfoBarType.Error);
                return false;
            }

            if (isPricePerHour)
            {
                if (duration is null)
                {
                    SetExitFunction("Duration is required when the lesson is priced per hour.", InfoBarType.Warning);
                    return false;
                }
                if (duration <= 0)
                {
                    SetExitFunction("Enter a valid non-negative duration (e.g., 60).", InfoBarType.Warning);
                    return false;
                }
            }
            else
            {
                duration = null; // Normalize to null for easier handling in the database and UI
            }

            if (basePrice <= 0)
            {
                SetExitFunction("Enter a valid non-negative price per student (e.g., 42.5).", InfoBarType.Warning);
                return false;
            }

            return true;
        }

        public (LessonSummary lesson, int index) GetLesson(Guid id) 
        {
            var lesson = Lessons.FirstOrDefault(l => l.Id == id);
            if (lesson is null)
            {
                SetExitFunction();
                throw new InvalidDataException("Lesson not loaded.");
            }
            return (lesson, Lessons.IndexOf(lesson));
        }

        public async Task AddLessonAsync(DateOnly date, string name, ServiceSummary service,
            int? duration, decimal pricePerLesson, bool isOnline, bool isWeekendOrHoliday, decimal tip,
            string? note, Guid studentId)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't add lesson while busy.", InfoBarType.Warning, false);
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
                    string.Empty,  // TODO
                    lesson.IsPricePerHour,
                    lesson.DurationMinutes,
                    lesson.BasePrice,
                    lesson.IsOnline,
                    lesson.TravelAllowance,
                    lesson.IsWeekendOrHoliday,
                    lesson.WeekendFee,
                    lesson.Tip,
                    lesson.Notes));
                
                SetExitFunction($"Lesson '{lesson.Name}' added successfully.", InfoBarType.Success);
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
                SetExitFunction("Can't change payment while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var (item, idx) = GetLesson(id);
            try
            {
                await _lessonRepository.UpdateLessonsPayment(new List<Guid> { id }, !item.IsPaid);
                Lessons[idx] = item with { IsPaid = !item.IsPaid };
                SetExitFunction($"Lesson '{item.Name}' marked as {(Lessons[idx].IsPaid ? "paid" : "unpaid")}.",
                    InfoBarType.Success);
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
                SetExitFunction("Can't delete lesson while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var (oldItem, idx) = GetLesson(id);

            if (oldItem.BillingDocumentId is not null)
            {
                SetExitFunction($"Can't delete lesson '{oldItem.Name}' for '{oldItem.StudentName}' because it's associated" +
                    $" to bill '{oldItem.BillingName}'", InfoBarType.Error);
                return;
            }

            try
            {
                await _lessonRepository.DeleteAsync(id);
                Lessons.Remove(oldItem);
                SetExitFunction($"Lesson '{oldItem.Name}' deleted successfully for '{oldItem.StudentName}'", 
                    InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task UpdateLessonAsync(Guid id, DateOnly date, string name,
            bool isPricePerHour, int? duration, decimal pricePerLesson,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, decimal tip, string? note)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't update lesson while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidateLessonInput(ref name, ref duration, isPricePerHour, pricePerLesson, tip))
                return;

            var (oldItem, idx) = GetLesson(id);

            try
            {
                var entity = await _lessonRepository.UpdateLesson(id, date, name, 
                    isPricePerHour, duration, pricePerLesson,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, tip, note);

                // Update item in UI list
                Lessons[idx] = oldItem with
                {
                    Name = entity.Name,
                    Date = entity.Date,
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
                SetExitFunction($"Lesson '{entity.Name}' updated successfully.", InfoBarType.Success);
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
                SetExitFunction("Can't clear filters while busy.", InfoBarType.Warning, false);
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
