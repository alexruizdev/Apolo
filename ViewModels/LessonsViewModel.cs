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
        [ObservableProperty] private int showLastNMonths;


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
        }

        // TODO: This is a bit hacky, but it works for now. Consider a more elegant solution if more filters are added in the future.
        partial void OnShownOnlyUnpaidChanged(bool value)
        {
            _ = LoadAsync();
        }

        partial void OnShowLastNMonthsChanged(int value)
        {
            _ = LoadAsync();
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

            int months = ShowLastNMonths > 0 ? ShowLastNMonths : 1; // Default to last 1 month if invalid value
            var items = await _lessonRepository.GetLessonsAsync(ShownOnlyUnpaid, months);

            Lessons.Clear();
            foreach (var item in items) Lessons.Add(item);

            SetExitFunction();
        }

        public bool ValidateStudentIds(IReadOnlyCollection<Guid> studentIds)
        {
            var uniqueReceivedIds = new HashSet<Guid>(studentIds);
            if (uniqueReceivedIds.Count != studentIds.Count)
            {
                SetExitFunction();
                throw new InvalidDataException("Duplicate student IDs found in the attendance list.");
            }
            var existintIds = Students.Select(s => s.Id).ToHashSet();
            if (uniqueReceivedIds.All(id => existintIds.Contains(id)) == false)
            {
                SetExitFunction();
                throw new InvalidDataException("One or more student IDs in the attendance list do not exist.");
            }
            if (studentIds.Count == 0)
            {
                SetExitFunction("No student IDs provided.", InfoBarType.Warning);
                return false;
            }
            return true;
        }

        public bool ValidateLessonInput(ref string name, ref int? duration, bool isPricePerHour, decimal pricePerAttendance)
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

            if (pricePerAttendance <= 0)
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
            int? duration, decimal pricePerAttendance, bool isOnline, bool isWeekendOrHoliday,
            string? note, IReadOnlyList<Guid> studentIds)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't add lesson while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidateStudentIds(studentIds))
                return;

            if (!ValidateLessonInput(ref name, ref duration, service.IsPricePerHour, pricePerAttendance))
                return; 

            try
            {
                var lesson = await _lessonRepository.AddLessonAsync(date, name, 
                    service.IsPricePerHour, duration, pricePerAttendance,
                    isOnline, TravelAllowance, isWeekendOrHoliday, WeekendFee, 
                    note, studentIds);

                // Add to UI
                Lessons.Insert(0, new LessonSummary(
                    lesson.Id,
                    lesson.Name,
                    lesson.Date,
                    lesson.IsPricePerHour,
                    lesson.DurationMinutes,
                    lesson.PricePerAttendance,
                    lesson.IsOnline,
                    lesson.TravelAllowance,
                    lesson.IsWeekenOrHoliday,
                    lesson.WeekendFee,
                    lesson.Notes,
                    lesson.AttendancesSummary(Students)));
                
                SetExitFunction($"Lesson '{lesson.Name}' added successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task UpdateLessonAsync(Guid id, DateOnly date, string name,
            bool isPricePerHour, int? duration, decimal pricePerAttendance,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, string? note)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't update lesson while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidateLessonInput(ref name, ref duration, isPricePerHour, pricePerAttendance))
                return;

            var (oldItem, idx) = GetLesson(id);

            try
            {
                var entity = await _lessonRepository.UpdateLesson(id, date, name, 
                    isPricePerHour, duration, pricePerAttendance,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, note);

                // Update item in UI list
                Lessons[idx] = oldItem with
                {
                    Name = entity.Name,
                    Date = entity.Date,
                    IsPricePerHour = entity.IsPricePerHour,
                    DurationMinutes = entity.DurationMinutes,
                    PricePerAttendance = entity.PricePerAttendance,
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

        public async Task UpdateLessonNoteAsync(Guid id, string? note)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't update lesson note while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var (oldItem, idx) = GetLesson(id);

            if (string.IsNullOrWhiteSpace(note))
                note = null; // Normalize to null for easier handling in the database and UI

            try
            {
                var entity = await _lessonRepository.UpdateLessonNoteAsync(id, note);

                // Update item in UI list
                Lessons[idx] = oldItem with
                {
                    Notes = entity.Notes,
                };
                SetExitFunction($"Lesson '{entity.Name}' note updated successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task AddAttendanceAsync(Guid lessonId, IReadOnlyCollection<Guid> studentIds)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't add attendance while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidateStudentIds(studentIds))
                return;



            var (oldItem, idx) = GetLesson(lessonId);

            try
            {
                var lesson = await _lessonRepository.AddAttendanceAsync(lessonId, studentIds);

                // Update UI
                Lessons[idx] = oldItem with
                {
                    Attendances = lesson.AttendancesSummary(Students)
                };
                SetExitFunction($"{studentIds.Count} student(s) were added to Lesson '{lesson.Name}' successfully.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task RemoveAttendanceAsync(Guid lessonId, Guid attendanceId)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't remove attendance while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var (oldItem, idx) = GetLesson(lessonId);

            try
            {
                var lesson = await _lessonRepository.RemoveAttendanceAsync(lessonId, attendanceId);

                // Delete lesson
                if (lesson.Attendances.Count == 0)
                {
                    Lessons.Remove(oldItem);
                    SetExitFunction($"Lesson '{lesson.Name}' was deleted after removing last attendant.", InfoBarType.Success);
                }
                // Update UI
                else
                {
                    Lessons[idx] = oldItem with
                    {
                        Attendances = lesson.AttendancesSummary(Students)
                    };
                    SetExitFunction($"Student was removed from lesson '{lesson.Name}' successfully.", InfoBarType.Success);
                }
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task UpdateAttendanceAsync(Guid lessonId, Guid attendanceId, bool isPaid)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't update attendance while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var (oldItem, idx) = GetLesson(lessonId);

            try
            {
                var lesson = await _lessonRepository.UpdateAttendanceAsync(lessonId, attendanceId, isPaid);

                Lessons[idx] = oldItem with
                {
                    Attendances = lesson.AttendancesSummary(Students)
                };

                SetExitFunction($"Lesson '{lesson.Name}' attendant was updated successfully.", InfoBarType.Success);
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
