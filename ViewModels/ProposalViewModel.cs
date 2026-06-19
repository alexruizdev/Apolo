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
        IReportWriter reportWriter) : UserProfileViewModel(userProfile)
    {

        // Input
        [ObservableProperty] private ServiceSummary? _selectedService;
        [ObservableProperty] private double _basePrice;
        [ObservableProperty] private bool _isOnline;
        [ObservableProperty] private bool _isWeekendOrHoliday;
        [ObservableProperty] private double _duration;
        [ObservableProperty] private int _frequency = 1;
        [ObservableProperty] private FrequencyUnit _unit = FrequencyUnit.PerWeek;

        // Report
        [ObservableProperty] private ProposalReport _report = new();

        // Dynamic UI Configurations
        [ObservableProperty] private string _priceHeader = "Price:";
        [ObservableProperty] private bool _isPricePerHour = false;
        [ObservableProperty] private bool _isPrimaryButtonEnabled;

        private readonly ProposalInput _input = new();

        // Descriptive UI formatting tags matching unit values
        public string BudgetMinusFrequencyString => FormatFreq(Report.BudgetMinus);
        public string BudgetRequestedFrequencyString => FormatFreq(Report.BudgetRequested);
        public string BudgetPlusFrequencyString => FormatFreq(Report.BudgetPlus);
        public string AlternativeTravelFrequencyString => FormatFreq(Report.AlternativeTravel);
        public string AlternativeFeeFrequencyString => FormatFreq(Report.AlternativeFee);

        public ObservableCollection<ServiceSummary> Services { get; } = [];

        // --- COMMANDS ---
        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitFunction("Can't load services while busy.", InfoBarType.Warning, false);
                return;
            }

            SetEnterFunction();

            _input.TravelAllowance = (decimal)Profile.TravelAllowance;
            _input.WeekendFee = (decimal)Profile.WeekendFee;

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
                SetExitFunction("Can't generate proposal while busy.", InfoBarType.Warning);
                return;
            }

            SetEnterFunction();

            if (!Directory.Exists(Profile.BillingFolder))
            {
                SetExitFunction("Can't export proposal without setting 'Billing Folder'", InfoBarType.Error);
                return;
            }

            var date = DateTime.Now.ToString("dd-MM-yyyy");
            var directoryPath = Path.Combine(Profile.BillingFolder, "Budget");
            Directory.CreateDirectory(directoryPath);
            var filename = Path.Combine(directoryPath, $"Proposal_{date}.pdf");
            try
            {
                reportWriter.GenerateProposal(filename, Report);

                SetExitFunction($"Generated proposal successfully: {filename}.", InfoBarType.Success);
            }
            catch (Exception ex)
            {
                SetExitFunction($"Error generating proposal '{filename}': {ex.Message}", InfoBarType.Error);
            }
        }

        private static string FormatFreq(BudgetColumn col) =>
            $"{col.Frequency} time(s) / {(col.Unit == FrequencyUnit.PerWeek ? "week" : "month")} ({col.SessionsPerMonth:F1} total sessions)";

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
            PriceHeader = IsPricePerHour ? "Price/Hour:" : "Price:";
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

        // --- VALIDATION LOGIC ---

        private void Validate()
        {
            SetEnterFunction();

            var errors = new List<string>();

            if (SelectedService is null)
                errors.Add("• You must select a service.");
            if (IsPricePerHour && (double.IsNaN(Duration) || Duration <= 0))
                errors.Add("• Duration must be a positive integer.");
            if (BasePrice <= 0)
                errors.Add("• Price must be a positive integer.");
            if (Frequency <= 0)
                errors.Add("• Frequency must be a positive integer.");

            IsPrimaryButtonEnabled = errors.Count == 0;

            if (!IsPrimaryButtonEnabled)
            {
                Report = new ProposalReport();
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
