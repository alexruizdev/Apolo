using Apolo.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Apolo.ViewModels
{
    public partial class SpecificationsViewModel : ObservableObject
    {
        SpecificationRepository _repository;
        UserProfileService _userProfileService;
        UserProfile _userProfile;

        public ObservableCollection<SpecificationSummary> Specifications { get; } = new();
        public ObservableCollection<StudentOption> Students { get; } = new();
        public ObservableCollection<ServiceSummary> Services { get; } = new();

        public decimal TravelAllowance => (decimal)_userProfile.TravelAllowance;
        public decimal WeekendFee => (decimal)_userProfile.WeekendFee;

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        public SpecificationsViewModel(SpecificationRepository repository, UserProfileService userProfileService)
        {
            _repository = repository;
            _userProfileService = userProfileService;
            _userProfile = userProfileService.LoadProfileAsync().Result;
        }

        public async Task RefreshProfileAsync()
        {
            _userProfile = await _userProfileService.LoadProfileAsync();
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

        public async Task AddSpecificationAsync(
            string name,
            int duration,
            double? price,
            bool online,
            bool weekend,
            Guid? studentId,
            Guid? serviceId)
        {
            if (IsBusy) return;

            name = (name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorMessage = "Specification name is required.";
                return;
            }
            if (duration <= 0)
            {
                ErrorMessage = "Enter a valid non-negative price (e.g., 60).";
                return;
            }
            if (serviceId is null)
            {
                ErrorMessage = "Select a service.";
                return;
            }
            if (studentId is null)
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
                    StudentId = studentId.Value,
                    ServiceId = serviceId.Value,
                    DurationMinutes = duration,
                    Price = (decimal?)price,
                    IsOnline = online,
                    IsWeekenOrHoliday = weekend
                };
                await _repository.AddSpecificationAsync(specification);

                var studentName = Students.First(s => s.Id == specification.StudentId).FullName;
                var serviceName = Services.First(s => s.Id == specification.ServiceId).Name;

                Specifications.Add(new SpecificationSummary(
                    specification.Id, specification.Name, specification.StudentId, studentName,
                    specification.ServiceId, serviceName, specification.DurationMinutes, (double?)specification.Price,
                    specification.IsOnline, specification.IsWeekenOrHoliday));
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

        public async Task UpdateSpecificationAsync(Guid id, string name, int durationMinutes, double? price,
            bool isOnline, bool isWeekend, Guid serviceId)
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
                await _repository.UpdateAsync(id, serviceId, name, durationMinutes, (decimal?)price, isOnline, isWeekend);

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
                        Price = price,
                        IsOnline = isOnline,
                        IsWeekenOrHoliday = isWeekend,
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

        public async Task CreateLessonFromSpecificationAsync(SpecificationSummary specification, 
            DateOnly date, ServiceSummary service, decimal travelAllowance, decimal weekendFee)
        {
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var instance = new Lesson
                {
                    Name = specification.ServiceName,
                    Date = date,
                    IsPricePerHour = service.IsPricePerHour,
                    DurationMinutes = specification.DurationMinutes,
                    PricePerAttendance = (decimal)(specification.Price ?? service.Price),
                    IsOnline = specification.IsOnline,
                    TravelAllowance = travelAllowance,
                    IsWeekenOrHoliday = specification.IsWeekenOrHoliday,
                    WeekendFee = weekendFee
                };
                instance.Attendaces.Add(new Attendance
                {
                    LessonId = instance.Id,
                    StudentId = specification.StudentId,
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
