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

        protected void SetExitFunction(string message, InfoBarType severity, bool resetBusy = true)
        {
            if (resetBusy)
            {
                IsBusy = false;
            }
            InfoBarType = severity;
            InfoMessage = message;
            OpenInfoBar = true;
        }
    }

    public partial class UserProfileViewModel : BaseViewModel
    {
        protected IUserProfileService _userProfileService;
        [ObservableProperty]
        protected UserProfile profile;

        public decimal TravelAllowance => (decimal)Profile.TravelAllowance;
        public decimal WeekendFee => (decimal)Profile.WeekendFee;

        public UserProfileViewModel(IUserProfileService userProfileService)
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
