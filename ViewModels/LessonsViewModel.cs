using Apolo.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;

namespace Apolo.ViewModels
{
    public partial class LessonsViewModel : ObservableObject
    {
        ILessonRepository _lessonRepository;
        IStudentRepository _studentRepository;
        IServiceRepository _serviceRepository;
        ISpecificationRepository _specificationRepository;
        IUserProfileService _userProfileService;
        UserProfile _userProfile;

        public ObservableCollection<LessonSummary> Lessons { get; } = new();
        public ObservableCollection<StudentOption> Students { get; } = new();
        public ObservableCollection<ServiceSummary> Services { get; } = new();

        [ObservableProperty] private bool shownOnlyUnpaid; // UI filter
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        public decimal TravelAllowance => (decimal)_userProfile.TravelAllowance;
        public decimal WeekendFee => (decimal)_userProfile.WeekendFee;

        public LessonsViewModel(ILessonRepository lessonRepository, IStudentRepository studentRepository, 
            IServiceRepository serviceRepository, ISpecificationRepository specificationRepository, IUserProfileService userProfile)
        {
            _lessonRepository = lessonRepository;
            _studentRepository = studentRepository;
            _serviceRepository = serviceRepository;
            _specificationRepository = specificationRepository;
            _userProfileService = userProfile;
            _userProfile = userProfile.LoadProfileAsync().Result;
        }

        public async Task RefreshProfileAsync()
        {
            _userProfile = await _userProfileService.LoadProfileAsync();
        }

        partial void OnShownOnlyUnpaidChanged(bool value)
        {
            _ = LoadAsync();
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var studentsItem = await _studentRepository.GetStudentOptionsAsync();

                Students.Clear();
                foreach (var item in studentsItem) Students.Add(item);

                var serviceItems = await _serviceRepository.GetServicesAsync();

                Services.Clear();
                foreach (var s in serviceItems) Services.Add(s);

                var items = await _lessonRepository.GetLessonsAsync(ShownOnlyUnpaid, 1); // TODO

                Lessons.Clear();
                foreach (var item in items) Lessons.Add(item);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public bool ValidateLessonInput(string name, int? duration, bool isPricePerHour, decimal pricePerAttendance)
        {
            name = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "Service name is required.";
                return false;
            }

            if (isPricePerHour)
            {
                if (duration is null)
                {
                    ErrorMessage = "Duration is required when the lesson is priced per hour.";
                    return false;   
                }
                if (duration <= 0)
                {
                    ErrorMessage = "Enter a valid non-negative duration (e.g., 60).";
                    return false;
                }
            }

            if (pricePerAttendance <= 0)
            {
                ErrorMessage = "Enter a valid non-negative price per student (e.g., 42.5).";
                return false;
            }
            return true;
        }

        public async Task CreateLessonAsync(
            DateOnly date, string name, ServiceSummary service,
            int? duration, decimal pricePerAttendance,
            bool isOnline, bool isWeekendOrHoliday,
            string? note,
            IReadOnlyList<Guid> studentIds)
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            if (!ValidateLessonInput(name, duration, service.IsPricePerHour, pricePerAttendance))
            {
                IsBusy = false;
                return; 
            }

            try
            {
                var lesson = await _lessonRepository.AddLessonAsync(date, name, 
                    service.IsPricePerHour, duration, pricePerAttendance,
                    isOnline, TravelAllowance, isWeekendOrHoliday, WeekendFee, 
                    note, studentIds);

                // Add to UI
                var attendanceRows = new List<AttendanceSummary>();
                var namesById = Students.ToDictionary(x => x.Id, x => x.FullName);
                for (int i = 0; i < lesson.Attendaces.Count; i++)
                {
                    var a = lesson.Attendaces.ElementAt(i);
                    attendanceRows.Add(new AttendanceSummary(
                        a.Id,
                        a.StudentId,
                        namesById.TryGetValue(a.StudentId, out var nm) ? nm : $"#{a.StudentId}",
                        a.IsPaid));
                }
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
                    attendanceRows));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task UpdateLessonAsync(Guid id, DateOnly date, string name,
            bool isPricePerHour, int? duration, decimal pricePerAttendance,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, string? note)
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            if (!ValidateLessonInput(name, duration, isPricePerHour, pricePerAttendance))
            {
                IsBusy = false;
                return;
            }

            try
            {
                var entity = await _lessonRepository.UpdateLesson(id, date, name, 
                    isPricePerHour, duration, pricePerAttendance,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, note);

                // Update item in UI list
                var idx = Lessons.Select((s, i) => (s, i)).FirstOrDefault(t => t.s.Id == id).i;
                if (idx >= 0)
                {
                    var attendances = Lessons[idx].Attendances;
                    Lessons[idx] = new LessonSummary(
                        id,
                        entity.Name,
                        entity.Date,
                        entity.IsPricePerHour,
                        entity.DurationMinutes,
                        entity.PricePerAttendance,
                        entity.IsOnline,
                        entity.TravelAllowance,
                        entity.IsWeekenOrHoliday,
                        entity.WeekendFee,
                        note,
                        Lessons[idx].Attendances);
                }
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "Save failed due to related data. Check constraints.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task UpdateLessonNoteAsync(Guid id, string? note)
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            if (string.IsNullOrWhiteSpace(note))
                note = null; // Normalize to null for easier handling in the database and UI
            try
            {
                var entity = await _lessonRepository.UpdateLessonNoteAsync(id, note);
                // Update item in UI list
                var idx = Lessons.Select((s, i) => (s, i)).FirstOrDefault(t => t.s.Id == id).i;
                if (idx >= 0)
                {
                    Lessons[idx] = Lessons[idx] with
                    {
                        Notes = entity.Notes
                    };
                }
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "Save failed due to related data. Check constraints.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task AddAttendanceAsync(Guid lessonId, IReadOnlyCollection<Guid> studentIds)
        {
            if (IsBusy || studentIds.Count == 0) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var lesson = await _lessonRepository.AddAttendanceAsync(lessonId, studentIds);

                // Update UI
                var idx = Lessons.Select((s, i) => (s, i)).FirstOrDefault(t => t.s.Id == lessonId).i;
                if (idx >= 0)
                {
                    var namesById = Students.ToDictionary(x => x.Id, x => x.FullName);
                    var attendances = lesson.Attendaces
                        .Select(a => new AttendanceSummary(
                            a.Id,
                            a.StudentId,
                            namesById.TryGetValue(a.StudentId, out var nm) ? nm : $"#{a.StudentId}",
                            a.IsPaid))
                        .OrderBy(x => x.StudentName)
                        .ToList();

                    var current = Lessons[idx];
                    Lessons[idx] = current with
                    {
                        Attendances = attendances
                    };
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task RemoveAttendanceAsync(Guid lessonId, Guid attendanceId)
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var lesson = await _lessonRepository.RemoveAttendanceAsync(lessonId, attendanceId);

                // Delete lesson
                if (lesson.Attendaces.Count == 0)
                {
                    var toRemove = Lessons.FirstOrDefault(l => l.Id == lesson.Id);
                    if (toRemove != null) Lessons.Remove(toRemove);
                }
                // Update UI
                else
                {
                    var idx = Lessons.Select((s, i) => (s, i)).FirstOrDefault(t => t.s.Id == lessonId).i;
                    if (idx >= 0)
                    {
                        var namesById = Students.ToDictionary(x => x.Id, x => x.FullName);
                        var attendances = lesson.Attendaces
                            .Select(a => new AttendanceSummary(
                                a.Id,
                                a.StudentId,
                                namesById.TryGetValue(a.StudentId, out var nm) ? nm : $"#{a.StudentId}",
                                a.IsPaid))
                            .OrderBy(x => x.StudentName)
                            .ToList();

                        var current = Lessons[idx];
                        Lessons[idx] = current with
                        {
                            Attendances = attendances
                        };
                    }
                }

            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task UpdateAttendanceAsync(Guid lessonId, Guid attendanceId, bool isPaid)
        {
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var lesson = await _lessonRepository.UpdateAttendanceAsync(lessonId, attendanceId, isPaid);

                // Update UI
                var idx = Lessons.Select((s, i) => (s, i)).FirstOrDefault(t => t.s.Id == lessonId).i;
                if (idx >= 0)
                {
                    var namesById = Students.ToDictionary(x => x.Id, x => x.FullName);
                    var attendances = lesson.Attendaces
                        .Select(a => new AttendanceSummary(
                            a.Id,
                            a.StudentId,
                            namesById.TryGetValue(a.StudentId, out var nm) ? nm : $"#{a.StudentId}",
                            a.IsPaid))
                        .OrderBy(x => x.StudentName)
                        .ToList();

                    var current = Lessons[idx];
                    Lessons[idx] = current with
                    {
                        Attendances = attendances
                    };
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task<IEnumerable<SpecificationOption>> GetSpecificationOptionsAsync(List<Guid> studentsIds)
        {
            return await _specificationRepository.GetSpecificationsForStudentAsync(studentsIds);
        }
    }
}
