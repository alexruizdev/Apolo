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
    public partial class SpecificationsViewModel : ObservableObject
    {
        SpecificationRepository _repository;

        public ObservableCollection<SpecificationSummary> Specifications { get; } = new();
        public ObservableCollection<StudentOption> Students { get; } = new();
        public ObservableCollection<ServiceSummary> Services { get; } = new();

        [ObservableProperty] private string newSpecificationName = string.Empty;
        [ObservableProperty] private int newDuration;
        [ObservableProperty] private bool newIsOnline;
        [ObservableProperty] private Guid? newSelectedStudentId;
        [ObservableProperty] private Guid? newSelectedServiceId;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        public SpecificationsViewModel(SpecificationRepository repository)
        {
            _repository = repository;
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = null;

            try
            {
                // Student options
                var studentItems = await _repository.GetStudentOptionsAsync();

                Students.Clear();
                foreach (var s in studentItems) Students.Add(s);

                var serviceItems = await _repository.GetServicesAsync();

                Services.Clear();
                foreach (var s in serviceItems) Services.Add(s);

                var items = await _repository.GetSpecificationsAsync();

                Specifications.Clear();
                foreach (var item in items)
                {
                    Specifications.Add(item);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task AddSpecificationAsync()
        {
            if (IsBusy) return;

            var name = (NewSpecificationName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "Specification name is required.";
                return;
            }
            if (NewDuration <= 0)
            {
                ErrorMessage = "Enter a valid non-negative price (e.g., 60).";
                return;
            }
            if (NewSelectedServiceId is null)
            {
                ErrorMessage = "Select a service.";
                return;
            }
            if (NewSelectedStudentId is null)
            {
                ErrorMessage = "Select a student.";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;
            try
            {
                var specification = new Specification
                {
                    Name = name,
                    StudentId = NewSelectedStudentId.Value,
                    ServiceId = NewSelectedServiceId.Value,
                    DurationMinutes = NewDuration,
                    IsOnline = NewIsOnline
                };
                await _repository.AddSpecificationAsync(specification);

                var studentName = Students.First(s => s.Id == specification.StudentId).FullName;
                var serviceName = Services.First(s => s.Id == specification.ServiceId).Name;

                Specifications.Add(new SpecificationSummary(
                    specification.Id, specification.Name, specification.StudentId, studentName,
                    specification.ServiceId, serviceName, specification.DurationMinutes, specification.IsOnline
                    ));

                // reset form
                NewSpecificationName = string.Empty;
                NewDuration = 0;
                NewIsOnline = false;
                NewSelectedServiceId = null;
                NewSelectedStudentId = null;
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "Could not create the specification (constraints or duplicates).";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        public async Task DeleteSpecificationAsync(SpecificationSummary? item)
        {
            if (item is null || IsBusy) { return; }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await _repository.DeleteAsync(item.Id);

                var toRemove = Specifications.FirstOrDefault(s => s.Id == item.Id);
                if (toRemove != null) Specifications.Remove(toRemove);
            }
            catch (DbUpdateException)
            {
                ErrorMessage = "Delete failed due to related data. Check constraints.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }

        public async Task UpdateSpecificationAsync(Guid id, string name, int durationMinutes, bool isOnline, Guid serviceId)
        {
            if (IsBusy) return;
            name = (name ?? "").Trim();

            if (string.IsNullOrEmpty(name))
            {
                ErrorMessage = "Name is required";
            }
            if (durationMinutes <= 0)
            {
                ErrorMessage = "Enter a valid non-negative duration (e.g., 60).";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                await _repository.UpdateAsync(id, serviceId, name, durationMinutes, isOnline);

                // Update item in UI list
                var idx = Specifications.Select((s, i) => (s, i)).FirstOrDefault(t => t.s.Id == id).i;
                if (idx >= 0)
                {
                    var serviceName = Services.First(s => s.Id == serviceId).Name;
                    var current = Specifications[idx];
                    Specifications[idx] = current with
                    {
                        SpecificationName = name,
                        DurationMinutes = durationMinutes,
                        IsOnline = isOnline,
                        ServiceId = serviceId,
                        ServiceName = serviceName
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

        public async Task CreateLessonFromSpecificationAsync(Guid studentId, string serviceName, int duration, bool isOnline, decimal price, DateOnly date)
        {
            if (IsBusy) return;

            serviceName = (serviceName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                ErrorMessage = "Name is required.";
                return;
            }
            if (duration <= 0)
            {
                ErrorMessage = "Enter a valid non-negative duration (e.g., 60).";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {

                var instance = new Lesson
                {
                    Name = serviceName,
                    Date = date,
                    DurationMinutes = duration,
                    IsOnline = isOnline,
                    GrandTotal = price
                };
                instance.Attendaces.Add(new Attendance
                {
                    StudentId = studentId,
                    Price = price,
                    IsPaid = false,
                });

                await _repository.AddLessonFromSpecificationAsync(instance);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally { IsBusy = false; }
        }
    }
}
