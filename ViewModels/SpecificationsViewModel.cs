using Apolo.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;
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
            IUserProfileService userProfileService,
            IStringLocalizer stringLocalizer)
            : base(userProfileService, stringLocalizer)
        {
            _specificationRepository = specificationRepository;
            _studentRepository = studentRepository;
            _serviceRepository = serviceRepository;
            _lessonRepository = lessonRepository;
        }

        // Messages
        private static string Message_Load_Error => "Message/Load_Specification_Error";
        private static string Message_Load_Success => "Message/Load_Specification_Success";
        private static string Message_Refresh_Error => "Message/Refresh_Specification_Error";
        private static string Message_Add_Error => "Message/Add_Specification_Error";
        private static string Message_Add_Success => "Message/Add_Specification_Success";
        private static string Message_Delete_Error => "Message/Delete_Specification_Error";
        private static string Message_Delete_Success => "Message/Delete_Specification_Success";
        private static string Message_Edit_Error => "Message/Edit_Specification_Error";
        private static string Message_Edit_Success => "Message/Edit_Specification_Success";
        private static string Message_Create_Lesson_Error => "Message/Create_Lesson_Specification_Error";
        private static string Message_Create_Lesson_Success => "Message/Create_Lesson_Specification_Success";

        public (SpecificationSummary value, int index) GetSpecification(Guid id)
        {
            var spec = Specifications.FirstOrDefault(s => s.Id == id);
            if (spec is null)
            {
                SetExitFunction();
                throw new InvalidDataException($"{_loc.Get(Message_Specification_Not_Loaded, id.ToString())}.");
            }
            return (spec, Specifications.IndexOf(spec));
        }

        public (ServiceSummary value, int index) GetService(Guid id)
        {
            var service = Services.FirstOrDefault(s => s.Id == id);
            if (service is null)
            {
                SetExitFunction();
                throw new InvalidDataException($"{_loc.Get(Message_Service_Not_Loaded, id.ToString())}.");
            }
            return (service, Services.IndexOf(service));
        }

        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Load_Error);
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

            SetExitFunction($"{_loc.Get(Message_Load_Success, Specifications.Count)}.", InfoBarType.Success);
        }

        public async Task RefreshSpecifications()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Refresh_Error);
                return;
            }

            var items = await _specificationRepository.GetSpecificationsAsync();

            Specifications.Clear();
            foreach (var item in items) Specifications.Add(item);
        }


        public async Task AddSpecificationAsync(string name, int durationMinutes, double? price,
            bool online, bool weekend,
            Guid studentId, Guid serviceId)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Add_Error);
                return;
            }

            SetEnterFunction();

            if (!ValidateSpecificationInput(ref name, durationMinutes, ref price))
                return;

            if (!Services.Any(s => s.Id == serviceId))
                throw new InvalidDataException($"{_loc.Get(Message_Service_Not_Loaded, serviceId)}.");
            
            if (!Students.Any(s => s.Id == studentId))
                throw new InvalidDataException($"{_loc.Get(Message_Student_Not_Loaded, studentId)}.");

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
                    IsWeekendOrHoliday = weekend
                };
                await _specificationRepository.AddSpecificationAsync(specification);

                var studentName = Students.First(s => s.Id == specification.StudentId).FullName;
                var serviceName = Services.First(s => s.Id == specification.ServiceId).Name;

                Specifications.Add(new SpecificationSummary(
                    specification.Id, specification.Name, specification.StudentId, studentName,
                    specification.ServiceId, serviceName, specification.DurationMinutes, (double?)specification.Price,
                    specification.IsOnline, specification.IsWeekendOrHoliday, specification.UsageCount));

                SetExitFunction($"{_loc.Get(Message_Add_Success, name, studentName)}.", InfoBarType.Success);
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
                SetExitBusy(Message_Delete_Error);
                return;
            }

            SetEnterFunction();

            var oldSpec = GetSpecification(id);

            try
            {
                await _specificationRepository.DeleteAsync(id);

                Specifications.Remove(oldSpec.value);
                SetExitFunction($"{_loc.Get(Message_Delete_Success, oldSpec.value.Name, oldSpec.value.StudentName)}.",
                    InfoBarType.Success);
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }

        public bool ValidateSpecificationInput(ref string name, int durationMinutes, ref double? price)
        {
            var errors = new List<string>();

            name = (name ?? "").Trim();
            if (string.IsNullOrEmpty(name))
                errors.Add(_loc.Get(Message_SpecificationNameValidation));

            if (durationMinutes <= 0)
                errors.Add(_loc.Get(Message_DurationValueValidation));

            if (price is not null) // TODO: test
            {
                price =  double.IsNaN(price.Value) ? null : price;
            }
            if (price <= 0)
                errors.Add(_loc.Get(Message_PriceValidation));

            if (errors.Count == 0)
                return true;

            SetExitFunction(string.Join(Environment.NewLine, errors), InfoBarType.Warning);
            return false;
        }

        public async Task UpdateSpecificationAsync(Guid id, string name, int durationMinutes, double? price,
            bool isOnline, bool isWeekend, Guid serviceId)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Edit_Error);
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
                    Name = name,
                    DurationMinutes = durationMinutes,
                    Price = price,
                    IsOnline = isOnline,
                    IsWeekendOrHoliday = isWeekend,
                    ServiceId = serviceId,
                    ServiceName = serviceName
                };
                SetExitFunction($"{_loc.Get(Message_Edit_Success, oldSpec.value.Name, oldSpec.value.StudentName)}.",
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
                SetExitBusy(Message_Create_Lesson_Error);
                return;
            }

            SetEnterFunction();

            if (tip < 0)
            {
                SetExitFunction(_loc.Get(Message_TipValidation), InfoBarType.Error);
                return;
            }

            var spec = GetSpecification(id);

            var (service, _) = GetService(spec.value.ServiceId);

            try
            {
                await _lessonRepository.AddLessonAsync(
                    date, spec.value.ServiceName, isPaid: false, spec.value.StudentId, null,
                    service.IsPricePerHour, spec.value.DurationMinutes, (decimal)(spec.value.Price ?? service.Price),
                    spec.value.IsOnline, TravelAllowance, spec.value.IsWeekendOrHoliday, WeekendFee,
                    tip, notes);

                await _specificationRepository.IncrementUsageAsync(id);

                SetExitFunction($"{_loc.Get(Message_Create_Lesson_Success, spec.value.ServiceName, spec.value.StudentName)}.",
                    InfoBarType.Success); 
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);
            }
        }
    }
}
