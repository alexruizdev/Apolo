using Apolo.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using ViewModels;

namespace Apolo.ViewModels
{
    public partial class SpecificationsViewModel : UserProfileViewModel
    {
        ISpecificationRepository _specificationRepository;
        IStudentRepository _studentRepository;
        IServiceRepository _serviceRepository;
        ILessonRepository _lessonRepository;

        public ObservableCollection<SpecificationSummary> Specifications { get; } = new();
        public ObservableCollection<StudentOption> Students { get; } = new();
        public ObservableCollection<ServiceSummary> Services { get; } = new();

        public SpecificationsViewModel(ISpecificationRepository specificationRepository,
            IStudentRepository studentRepository,
            IServiceRepository serviceRepository,
            ILessonRepository lessonRepository,
            IUserProfileService userProfileService)
            : base(userProfileService)
        {
            _specificationRepository = specificationRepository;
            _studentRepository = studentRepository;
            _serviceRepository = serviceRepository;
            _lessonRepository = lessonRepository;
            _userProfileService = userProfileService;
            profile = userProfileService.LoadProfileAsync().Result;
        }

        public (SpecificationSummary value, int index) GetSpecification(Guid id)
        {
            var spec = Specifications.FirstOrDefault(s => s.Id == id);
            if (spec is null)
            {
                SetExitFunction();
                throw new InvalidDataException("Specification not loaded.");
            }
            return (spec, Specifications.IndexOf(spec));
        }

        public (ServiceSummary value, int index) GetService(Guid id)
        {
            var service = Services.FirstOrDefault(s => s.Id == id);
            if (service is null)
            {
                SetExitFunction();
                throw new InvalidDataException("Service not loaded.");
            }
            return (service, Services.IndexOf(service));
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load specifications while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var studentItems = await _studentRepository.GetStudentOptionsAsync();

            Students.Clear();
            foreach (var s in studentItems) Students.Add(s);

            var serviceItems = await _serviceRepository.GetServicesAsync();

            Services.Clear();
            foreach (var s in serviceItems) Services.Add(s);

            var items = await _specificationRepository.GetSpecificationsAsync();

            Specifications.Clear();
            foreach (var item in items) Specifications.Add(item);

            SetExitFunction();
        }

        public async Task AddSpecificationAsync(string name, int durationMinutes, double? price,
            bool online, bool weekend,
            Guid studentId, Guid serviceId)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't add specification while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidateSpecificationInput(ref name, durationMinutes, ref price))
                return;

            if (!Services.Any(s => s.Id == serviceId))
                throw new InvalidDataException("Service ID is not recognize.");
            
            if (!Students.Any(s => s.Id == studentId))
                throw new InvalidDataException("Student ID is not recognize.");
            

            try
            {
                var specification = new Specification
                {
                    Name = name,
                    StudentId = studentId,
                    ServiceId = serviceId,
                    DurationMinutes = durationMinutes,
                    Price = (decimal?)price,
                    IsOnline = online,
                    IsWeekenOrHoliday = weekend
                };
                await _specificationRepository.AddSpecificationAsync(specification);

                var studentName = Students.First(s => s.Id == specification.StudentId).FullName;
                var serviceName = Services.First(s => s.Id == specification.ServiceId).Name;

                Specifications.Add(new SpecificationSummary(
                    specification.Id, specification.Name, specification.StudentId, studentName,
                    specification.ServiceId, serviceName, specification.DurationMinutes, (double?)specification.Price,
                    specification.IsOnline, specification.IsWeekenOrHoliday));

                SetExitFunction($"Specification '{name}' added for {studentName}.", InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        [RelayCommand]
        public async Task DeleteSpecificationAsync(Guid id)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't delete specification while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            var oldSpec = GetSpecification(id);

            try
            {
                await _specificationRepository.DeleteAsync(id);

                Specifications.Remove(oldSpec.value);
                SetExitFunction($"Specification '{oldSpec.value.SpecificationName}' deleted for {oldSpec.value.StudentName}.",
                    InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public bool ValidateSpecificationInput(ref string name, int durationMinutes, ref double? price)
        {
            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                SetExitFunction("Specification name is required.", InfoBarType.Warning);
                return false;
            }
            if (durationMinutes <= 0)
            {
                SetExitFunction("Enter a valid non-negative duration (e.g., 60).", InfoBarType.Warning);
                return false;
            }
            if (price is not null) // TODO: test
            {
                price =  double.IsNaN(price.Value) ? null : price;
            }
            return true;
        }

        public async Task UpdateSpecificationAsync(Guid id, string name, int durationMinutes, double? price,
            bool isOnline, bool isWeekend, Guid serviceId)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't update specification while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (!ValidateSpecificationInput(ref name, durationMinutes, ref price))
            {
                return;
            }

            var oldSpec = GetSpecification(id);

            try
            {
                await _specificationRepository.UpdateAsync(id, serviceId, name, durationMinutes, (decimal?)price, isOnline, isWeekend);


                var serviceName = Services.First(s => s.Id == serviceId).Name;
                Specifications[oldSpec.index] = oldSpec.value with
                {
                    SpecificationName = name,
                    DurationMinutes = durationMinutes,
                    Price = price,
                    IsOnline = isOnline,
                    IsWeekenOrHoliday = isWeekend,
                    ServiceId = serviceId,
                    ServiceName = serviceName
                };
                SetExitFunction($"Specification '{oldSpec.value.SpecificationName}' updated for {oldSpec.value.StudentName}.",
                    InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public async Task CreateLessonFromSpecificationAsync(Guid id, DateOnly date, decimal tip, string? notes)
        {
            if (IsBusy)
            {
                SetExitFunction("Can't create lesson while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            if (tip < 0)
            {
                SetExitFunction("Tip can't be negative.", InfoBarType.Error);
                return;
            }

            var spec = GetSpecification(id);

            var (service, _) = GetService(spec.value.ServiceId);

            try
            {
                await _lessonRepository.AddLessonAsync(
                    date, spec.value.ServiceName, isPaid: false, spec.value.StudentId, null,
                    service.IsPricePerHour, spec.value.DurationMinutes, (decimal)(spec.value.Price ?? service.Price),
                    spec.value.IsOnline, TravelAllowance, spec.value.IsWeekenOrHoliday, WeekendFee,
                    tip, notes);

                SetExitFunction($"Lesson '{spec.value.ServiceName}' created for {spec.value.StudentName}.",
                    InfoBarType.Success); ;
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
