using Apolo.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using PDF;
using Repository;
using System.Collections.ObjectModel;

namespace ViewModels
{
    public partial class ProposalViewModel(IServiceRepository serviceRepository, IUserProfileService userProfile,
        IReportWriter reportWriter, IStringLocalizer stringLocalizer) : BaseViewModel(stringLocalizer, userProfile)
    {

        // Input
        [ObservableProperty] private ServiceSummary? _selectedService;
        [ObservableProperty] private double _basePrice;
        [ObservableProperty] private bool _isOnline;
        [ObservableProperty] private bool _isWeekendOrHoliday;
        [ObservableProperty] private double _duration;
        [ObservableProperty] private int _frequency;
        [ObservableProperty] private FrequencyUnit _unit = FrequencyUnit.PerWeek;

        // Report
        [ObservableProperty] private ProposalReport _report = new();

        // Dynamic UI Configurations
        [ObservableProperty] private string _priceHeader = string.Empty;
        [ObservableProperty] private bool _isPricePerHour = false;
        [ObservableProperty] private bool _isPrimaryButtonEnabled;
        [ObservableProperty] private bool _includeFrequencyScalingBudgets = false;
        [ObservableProperty] private bool _includeAlternativeOperationalScenarios = false;

        public readonly ProposalInput _input = new();

        // Descriptive UI formatting tags matching unit values
        public string BudgetMinusFrequencyString => FormatFreq(Report.BudgetMinus);
        public string BudgetRequestedFrequencyString => FormatFreq(Report.BudgetRequested);
        public string BudgetPlusFrequencyString => FormatFreq(Report.BudgetPlus);
        public string AlternativeTravelFrequencyString => FormatFreq(Report.AlternativeTravel);
        public string AlternativeFeeFrequencyString => FormatFreq(Report.AlternativeFee);

        public ObservableCollection<ServiceSummary> Services { get; } = [];

        // Messages
        private static string Message_Load_Error => "Messages/Load_Proposal_Error";
        private static string Message_Generate_Error => "Messages/Generate_Proposal_Error";
        private static string Message_Generate_Success => "Messages/Generate_Proposal_Success";
        private static string Message_Week => "Message/Week";
        private static string Message_Month => "Message/Month";
        private static string Message_Times => "Message/Times";
        private static string Message_Sessions => "Message/Sessions";

        // --- COMMANDS ---
        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Load_Error);
                return;
            }

            SetEnterFunction();

            await RefreshProfileAsync();

            _input.TravelAllowance = (decimal)Profile.TravelAllowance;
            _input.WeekendFee = (decimal)Profile.WeekendFee;
            _input.PriceRounding = (decimal)Profile.PriceRounding;
            _input.IncludeCancellationPolicy = Profile.ShowProposalCancellationPolicy;
            _input.CancellationPolicyTitle = Profile.ProposalCancellationPolicyTitle;
            _input.CancellationPolicyItems = Profile.GetSections();
            Duration = 60;
            Frequency = 1;

            var serviceItems = await serviceRepository.GetServicesAsync();

            Services.Clear();
            foreach (var s in serviceItems) Services.Add(s);

            SetExitFunction();

            Validate();
        }

        [RelayCommand]
        public async Task GeneratePDF()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Generate_Error);
                return;
            }

            SetEnterFunction();

            if (!Directory.Exists(Profile.BillingFolder))
            {
                SetExitFunction($"{_loc.Get(Message_Generate_Error)}: {_loc.Get(Message_Generate_Error, Profile.BillingFolder)}.", 
                    InfoBarType.Error);
                return;
            }

            var date = DateTime.Now.ToString("dd-MM-yyyy");
            var directoryPath = Path.Combine(Profile.BillingFolder, "Budget");
            Directory.CreateDirectory(directoryPath);

            var cleanServiceName = SanitizeFileName(Report.ServiceName);
            var baseFilename = string.IsNullOrWhiteSpace(cleanServiceName)
                ? $"Proposal_{date}"
                : $"Proposal_{cleanServiceName}_{date}";

            var filename = Path.Combine(directoryPath, $"{baseFilename}.pdf");
            int counter = 1;
            while (File.Exists(filename))
            {
                filename = Path.Combine(directoryPath, $"{baseFilename} ({counter}).pdf");
                counter++;
            }

            Report.IncludeCancellationPolicy = Profile.ShowProposalCancellationPolicy;
            Report.CancellationPolicyTitle = Profile.ProposalCancellationPolicyTitle;
            Report.CancellationPolicyItems = Profile.GetSections();

            try
            {
                reportWriter.GenerateProposal(filename, Report);

                SetExitFunction($"{_loc.Get(Message_Generate_Success, filename)}.", InfoBarType.Success);
            }
            catch (Exception ex)
            {
                SetExitFunction($"{_loc.Get(Message_Generate_Error)} '{filename}': {ex.Message}", InfoBarType.Error);
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var invalidChars = Path.GetInvalidFileNameChars();
            return new string([.. name.Where(c => !invalidChars.Contains(c))]).Trim();
        }

        private string FormatFreq(BudgetColumn col) =>
            $"{col.Frequency} {_loc.Get(Message_Times)} / {(col.Unit == FrequencyUnit.PerWeek ? 
                _loc.Get(Message_Week) : _loc.Get(Message_Month))} ({col.SessionsPerMonth:F1} {_loc.Get(Message_Sessions)})";

        // --- UPDATING FORM LOGIC ---

        partial void OnSelectedServiceChanged(ServiceSummary? value)
        {
            if (SelectedService is ServiceSummary s)
            {
                IsPricePerHour = s.IsPricePerHour;
                BasePrice = s.Price;
                _input.ServiceName = s.Name;
            }
            else
            {
                IsPricePerHour = false;
                BasePrice = 0;
                _input.ServiceName = string.Empty;
            }

            Validate();
        }

        public int SelectedUnitIndex
        {
            get => Unit == FrequencyUnit.PerWeek ? 0 : 1;
            set
            {
                Unit = value == 0 ? FrequencyUnit.PerWeek : FrequencyUnit.PerMonth;
                Validate();
            }
        }

        partial void OnBasePriceChanged(double value)
        {
            _input.BasePrice = value;
            Validate();
        }
        partial void OnIsOnlineChanged(bool value) { 
            _input.IsOnline = value;
            Validate();
        }
        partial void OnIsWeekendOrHolidayChanged(bool value) {
            _input.IsWeekendOrHoliday = value;
            Validate();
        } 
        partial void OnDurationChanged(double value)
        {
            _input.Duration = value;
            Validate();
        }
        partial void OnIsPricePerHourChanged(bool value)
        {
            PriceHeader = IsPricePerHour ? PriceHeader = _loc.Get(Header_PricePerHour) : PriceHeader = _loc.Get(Header_Price);
            _input.IsPricePerHour = value;
        }
        partial void OnFrequencyChanged(int value)
        {
            _input.Frequency = value;
            Validate();
        }
        partial void OnUnitChanged(FrequencyUnit value)
        {
            _input.Unit = value;
            Validate();
        }
        partial void OnIncludeFrequencyScalingBudgetsChanged(bool value)
        {
            _input.IncludeFrequencyScalingBudgets = value;
            Report.IncludeFrequencyScalingBudgets = value;
        }
        partial void OnIncludeAlternativeOperationalScenariosChanged(bool value)
        {
            _input.IncludeAlternativeOperationalScenarios = value;
            Report.IncludeAlternativeOperationalScenarios = value;
        }

        // --- VALIDATION LOGIC ---

        private void Validate()
        {
            SetEnterFunction();

            var errors = new List<string>();

            if (SelectedService is null)
                errors.Add(_loc.Get(Message_SelectServiceValidation));
            if (IsPricePerHour && (double.IsNaN(Duration) || Duration <= 0))
                errors.Add(_loc.Get(Message_DurationValueValidation));
            if (BasePrice <= 0)
                errors.Add(_loc.Get(Message_PriceValidation));
            if (Frequency <= 0)
                errors.Add(_loc.Get(Message_FrequencyValidation));

            IsPrimaryButtonEnabled = errors.Count == 0;

            if (!IsPrimaryButtonEnabled)
            {
                Report = new ProposalReport
                {
                    IncludeFrequencyScalingBudgets = IncludeFrequencyScalingBudgets,
                    IncludeAlternativeOperationalScenarios = IncludeAlternativeOperationalScenarios,
                    IncludeCancellationPolicy = Profile.ShowProposalCancellationPolicy,
                    CancellationPolicyTitle = Profile.ProposalCancellationPolicyTitle,
                    CancellationPolicyItems = Profile.GetSections()
                };
                SetExitFunction(string.Join(Environment.NewLine, errors), InfoBarType.Error);
            }
            else
            {
                Report = ProposalService.CalculateProposal(_input);
                // Broadcast format string evaluations to columns
                OnPropertyChanged(nameof(BudgetMinusFrequencyString));
                OnPropertyChanged(nameof(BudgetRequestedFrequencyString));
                OnPropertyChanged(nameof(BudgetPlusFrequencyString));
                OnPropertyChanged(nameof(AlternativeTravelFrequencyString));
                OnPropertyChanged(nameof(AlternativeFeeFrequencyString));
                SetExitFunction();
            }
        }
    }
}
