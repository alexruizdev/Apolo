using Apolo.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;

namespace ViewModels
{
    public enum InfoBarType
    {
        Success,
        Warning,
        Error,
        Info
    }
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty] protected bool isBusy;
        [ObservableProperty] protected string? infoMessage;
        [ObservableProperty] protected bool openInfoBar;
        [ObservableProperty] protected InfoBarType infoBarType;
        public IStringLocalizer _loc;

        public BaseViewModel(IStringLocalizer stringLocalizer)
        {
            _loc = stringLocalizer;
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

        // Headers
        // Headers
        protected static string Header_Price => "Messages/Price";
        protected static string Header_PricePerHour => "Messages/PricePerHour";
    }

    public partial class UserProfileViewModel : BaseViewModel
    {
        protected IUserProfileService _userProfileService;
        [ObservableProperty]
        protected UserProfile profile;

        public decimal TravelAllowance => (decimal)Profile.TravelAllowance;
        public decimal WeekendFee => (decimal)Profile.WeekendFee;

        public UserProfileViewModel(IUserProfileService userProfileService, IStringLocalizer stringLocalizer)
            : base (stringLocalizer)
        {
            _userProfileService = userProfileService;
            profile = userProfileService.LoadProfileAsync().Result;
        }

        [RelayCommand]
        public async Task RefreshProfileAsync()
        {
            Profile = await _userProfileService.LoadProfileAsync();
        }

    }
}
