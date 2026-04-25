using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Apolo.ViewModels
{
    public partial class LessonsViewModel : ObservableObject
    {
        LessonRepository _repository;

        public ObservableCollection<LessonSummary> Lessons { get; } = new();
        public ObservableCollection<StudentOption> Students { get; } = new();
        public ObservableCollection<ServiceSummary> Services { get; } = new();

        [ObservableProperty] private bool shownOnlyUnpaid; // UI filter
        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        public LessonsViewModel(LessonRepository repository)
        {
            _repository = repository;
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
                var studentsItem = await _repository.GetStudentOptionsAsync();

                Students.Clear();
                foreach (var item in studentsItem) Students.Add(item);

                var serviceItems = await _repository.GetServicesAsync();

                Services.Clear();
                foreach (var s in serviceItems) Services.Add(s);

                var items = await _repository.GetLessonsAsync(ShownOnlyUnpaid, 1); // TODO

                Lessons.Clear();
                foreach (var item in items) Lessons.Add(item);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task CreateLessonAsync(
            string name,
            DateOnly date,
            int duration,
            bool isOnline,
            bool isTotalPrice,
            decimal price,
            IReadOnlyList<Guid> studentIds)
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            name = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                IsBusy = false;
                ErrorMessage = "Service name is required.";
                return;
            }
            if (duration <= 0 && !isTotalPrice)
            {
                IsBusy = false;
                ErrorMessage = "Enter a valid non-negative duration (e.g., 60).";
                return;
            }
            if (studentIds.Count <= 0)
            {
                IsBusy = false;
                ErrorMessage = "Select at least one student.";
                return;
            }

            try
            {
                var lesson = await _repository.CreateLesson(name, date, duration, isOnline, isTotalPrice, price, studentIds);

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
                    lesson.DurationMinutes,
                    lesson.IsOnline,
                    lesson.IsTotalPrice,
                    lesson.PricePerStudent,
                    attendanceRows));
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task UpdateLessonAsync(Guid id, string name, DateOnly date, int duration, bool isOnline, bool isTotalPrice, decimal price)
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            name = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                IsBusy = false;
                ErrorMessage = "Service name is required.";
                return;
            }

            if (duration <= 0 && !isTotalPrice)
            {
                IsBusy = false;
                ErrorMessage = "Enter a valid non-negative duration (e.g., 60).";
                return;
            }

            if (price <= 0)
            {
                IsBusy = false;
                ErrorMessage = "Enter a valid non-negative price per student (e.g., 42.5).";
                return;
            }

            try
            {
                var entity = await _repository.UpdateLesson(id, name, date, duration, isOnline, isTotalPrice, price);

                // Update item in UI list
                var idx = Lessons.Select((s, i) => (s, i)).FirstOrDefault(t => t.s.Id == id).i;
                if (idx >= 0)
                {
                    var attendances = Lessons[idx].Attendances;
                    Lessons[idx] = new LessonSummary(
                        id,
                        entity.Name,
                        entity.Date,
                        entity.DurationMinutes,
                        entity.IsOnline,
                        entity.IsTotalPrice,
                        entity.PricePerStudent,
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

        public async Task AddAttendanceAsync(Guid lessonId, IReadOnlyCollection<Guid> studentIds)
        {
            if (IsBusy || studentIds.Count == 0) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var lesson = await _repository.AddAttendanceAsync(lessonId, studentIds);

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
                var lesson = await _repository.RemoveAttendanceAsync(lessonId, attendanceId);

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
                var lesson = await _repository.UpdateAttendanceAsync(lessonId, attendanceId, isPaid);

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
            return await _repository.GetSpecificationsForStudentAsync(studentsIds);
        }
    }
}
