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

        [ObservableProperty] private bool shownOnlyUnpaid;
        [ObservableProperty] private DateTimeOffset dateFiler;


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
            DateFiler = DateTimeOffset.Now.AddMonths(-2);
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

            var now = DateTimeOffset.UtcNow;
            var months = ((now.Year - DateFiler.Year) * 12) + now.Month - DateFiler.Month;
            var items = await _lessonRepository.GetLessonsAsync(ShownOnlyUnpaid, months > 0 ? months : null);

            Lessons.Clear();
            foreach (var item in items) Lessons.Add(item);

            SetExitFunction();
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

        public bool ValidateLessonInput(ref string name, ref int? duration, bool isPricePerHour, decimal basePrice)
        {
            name = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                SetExitFunction("Lesson name is required.", InfoBarType.Warning);
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
            int? duration, decimal pricePerLesson, bool isOnline, bool isWeekendOrHoliday,
            string? note, Guid studentId)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't add lesson while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var student = GetStudent(studentId);

            if (!ValidateLessonInput(ref name, ref duration, service.IsPricePerHour, pricePerLesson))
                return; 

            try
            {
                var lesson = await _lessonRepository.AddLessonAsync(date, name, isPaid: false, studentId, null,
                    service.IsPricePerHour, duration, pricePerLesson,
                    isOnline, TravelAllowance, isWeekendOrHoliday, WeekendFee, note);

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
                    lesson.IsWeekenOrHoliday,
                    lesson.WeekendFee,
                    lesson.Notes));
                
                SetExitFunction($"Lesson '{lesson.Name}' added successfully.", InfoBarType.Success);
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
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, string? note)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't update lesson while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidateLessonInput(ref name, ref duration, isPricePerHour, pricePerLesson))
                return;

            var (oldItem, idx) = GetLesson(id);

            try
            {
                var entity = await _lessonRepository.UpdateLesson(id, date, name, 
                    isPricePerHour, duration, pricePerLesson,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, note);

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
                    IsWeekenOrHoliday = entity.IsWeekenOrHoliday,
                    WeekendFee = entity.WeekendFee,
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
    }
}
