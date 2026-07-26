using Apolo.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.ObjectModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ViewModels
{
    public enum InfoBarType
    {
        Success,
        Warning,
        Error,
        Info
    }
    public partial class BaseViewModel(IStringLocalizer stringLocalizer, IUserProfileService userProfileService) : ObservableObject
    {
        [ObservableProperty] protected bool isBusy;
        [ObservableProperty] protected string? infoMessage;
        [ObservableProperty] protected bool openInfoBar;
        [ObservableProperty] protected InfoBarType infoBarType;
        [ObservableProperty] protected UserProfile profile = userProfileService.LoadProfileAsync().Result;

        public IStringLocalizer _loc = stringLocalizer;
        public IUserProfileService _userProfileService = userProfileService;


        public decimal TravelAllowance => (decimal)Profile.TravelAllowance;
        public decimal WeekendFee => (decimal)Profile.WeekendFee;

        [RelayCommand]
        public async Task RefreshProfileAsync()
        {
            Profile = await _userProfileService.LoadProfileAsync();
        }

        protected void SetEnterFunction()
        {
            IsBusy = true;
            InfoMessage = null;
            OpenInfoBar = false;
            InfoBarType = InfoBarType.Success;
        }

        protected void SetExitFunction()
        {
            IsBusy = false;
            OpenInfoBar = false;
            InfoMessage = null;
            InfoBarType = InfoBarType.Success;
        }

        protected void SetExitFunction(string message, InfoBarType severity)
        {
            IsBusy = false;
            InfoBarType = severity;
            InfoMessage = message;
            OpenInfoBar = true;
        }

        protected void SetExitBusy(string message_error)
        {
            InfoBarType = InfoBarType.Warning;
            InfoMessage = $"{_loc.Get(message_error)}: {_loc.Get(Message_Reason_Busy)}.";
            OpenInfoBar = true;
        }

        // Messages
        protected static string Message_Reason_Busy => "Messages/Reason_Busy";
        protected static string Message_Student_Not_Loaded => "Messages/Student_Not_Loaded";
        protected static string Message_Service_Not_Loaded => "Messages/Service_Not_Loaded";
        protected static string Message_Payer_Not_Loaded => "Messages/Payer_Not_Loaded";
        protected static string Message_Lesson_Not_Loaded => "Messages/Lesson_Not_Loaded";
        protected static string Message_Bill_NotLoaded => "Messages/Bill_Not_Loaded";
        protected static string Message_Specification_Not_Loaded => "Messages/Specification_Not_Loaded";
        protected static string Message_TipValidation => "Messages/Tip_Validation";
        protected static string Message_SelectStudentValidation => "Messages/Student_Selection_Validation";
        protected static string Message_SelectServiceValidation => "Messages/Service_Selection_Validation";
        protected static string Message_LessonNameValidation => "Messages/Lesson_Name_Validation";
        protected static string Message_ServiceNameValidation => "Messages/Service_Name_Validation";
        protected static string Message_PersonNameValidation => "Messages/Person_Name_Validation";
        protected static string Message_SpecificationNameValidation => "Messages/Specification_Name_Validation";
        protected static string Message_DurationValidation => "Messages/Duration_Validation";
        protected static string Message_DurationValueValidation => "Messages/Duration_Value_Validation";
        protected static string Message_PriceValidation => "Messages/Price_Validation";
        protected static string Message_FrequencyValidation => "Messages/Frequency_Validation";
        protected static string Message_LessonPaidValidation => "Messages/Lesson_Paid_Validation";
        protected static string Message_LessonBillValidation => "Messages/Lesson_Bill_Validation";
        protected static string Message_Change_Payment_Error => "Messages/Change_Payment_Error";
        protected static string Message_Mark_Paid => "Messages/Mark_Paid_Success";
        protected static string Message_Lessons_Mark_Paid => "Messages/Lessons_Mark_Paid_Success";
        protected static string Message_Mark_Paid_Reason => "Messages/Mark_Paid_Reason";
        protected static string Message_Mark_Unpaid => "Messages/Mark_Unpaid_Success";
        protected static string Message_Lessons_Mark_Unpaid => "Messages/Lessons_Mark_Unpaid_Success";
        protected static string Message_Mark_Unpaid_Reason => "Messages/Mark_Unpaid_Reason";
        protected static string Message_Bill_Folder_Reason => "Messages/Billing_Folder_Reason";

        protected static string Header_Price => "Messages/Price";
        protected static string Header_PricePerHour => "Messages/PricePerHour";
    }

    public partial class LessonsBaseViewModel (ILessonRepository lessonRepository, IStudentRepository studentRepository, IServiceRepository serviceRepository, IStringLocalizer stringLocalizer, IUserProfileService userProfileService) : 
        BaseViewModel(stringLocalizer, userProfileService)
    {
        readonly IStudentRepository _studentRepository = studentRepository;
        readonly IServiceRepository _serviceRepository = serviceRepository;
        protected readonly ILessonRepository _lessonRepository = lessonRepository;
        public ObservableCollection<StudentOption> Students { get; } = [];
        public ObservableCollection<ServiceSummary> Services { get; } = [];

        protected static string Message_Add_Error => "Messages/Add_Lesson_Error";
        protected static string Message_Add_Success => "Messages/Add_Lesson_Success";

        public virtual async Task LoadAsync()
        {
            var studentItems = await _studentRepository.GetStudentOptionsAsync();

            Students.Clear();
            foreach (var s in studentItems) Students.Add(s);

            var serviceItems = await _serviceRepository.GetServicesAsync();

            Services.Clear();
            foreach (var s in serviceItems) Services.Add(s);
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

        public virtual async Task<IEnumerable<SpecificationOption>> GetSpecificationOptionsAsync(List<Guid> studentsIds) => [];


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

        public virtual async Task<Lesson?> AddLessonAsync(DateOnly date, string name, ServiceSummary service,
            int? duration, decimal pricePerLesson, bool isOnline, bool isWeekendOrHoliday, decimal tip,
            string? note, Guid studentId)
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Add_Error);
                return null;
            }

            SetEnterFunction();

            if (!ValidateLessonInput(ref name, ref duration, service.IsPricePerHour, pricePerLesson, tip))
                return null;

            try
            {
                var lesson = await _lessonRepository.AddLessonAsync(date, name, isPaid: false, studentId, null,
                    service.IsPricePerHour, duration, pricePerLesson,
                    isOnline, TravelAllowance, isWeekendOrHoliday, WeekendFee, tip, note);


                SetExitFunction($"{_loc.Get(Message_Add_Success)}: '{lesson.Name}'.", InfoBarType.Success);

                return lesson;
            }
            catch (DbUpdateException ex)
            {
                SetExitFunction(ex.Message, InfoBarType.Error);

                return null;
            }
        }

        public virtual async Task UpdateLessonAsync(Guid id, DateOnly date, string name,
            bool isPricePerHour, int? duration, decimal basePrice,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, decimal tip, string? note)
        { }
    }
}
