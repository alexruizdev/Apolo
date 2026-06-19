using Apolo.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Models;
using PDF;
using Repository;
using System.Collections.ObjectModel;

namespace ViewModels
{
    public partial class ProposalViewModel : UserProfileViewModel
    {
        readonly IServiceRepository _serviceRepository;
        private IReportWriter _pdfWriter;

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

        // Descriptive UI formatting tags matching unit values
        public string BudgetMinusFrequencyString => FormatFreq(Report.BudgetMinus);
        public string BudgetRequestedFrequencyString => FormatFreq(Report.BudgetRequested);
        public string BudgetPlusFrequencyString => FormatFreq(Report.BudgetPlus);
        public string AlternativeTravelFrequencyString => FormatFreq(Report.AlternativeTravel);
        public string AlternativeFeeFrequencyString => FormatFreq(Report.AlternativeFee);

        public ObservableCollection<ServiceSummary> Services { get; } = new();

        public ProposalViewModel(IServiceRepository serviceRepository, IUserProfileService userProfile,
            IReportWriter reportWriter)
            : base(userProfile)
        {
            _serviceRepository = serviceRepository;
            _pdfWriter = reportWriter;
        }

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

            var serviceItems = await _serviceRepository.GetServicesAsync();

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
                _pdfWriter.GenerateProposal(filename, Report);

                SetExitFunction($"Generated proposal successfully: {filename}.", InfoBarType.Success);
            }
            catch (Exception ex)
            {
                SetExitFunction($"Error generating proposal '{filename}': {ex.Message}", InfoBarType.Error);
            }
        }

        private string FormatFreq(BudgetColumn col) =>
            $"{col.Frequency} time(s) / {(col.Unit == FrequencyUnit.PerWeek ? "week" : "month")} ({col.SessionsPerMonth:F1} total sessions)";

        // --- UPDATING FORM LOGIC ---

        partial void OnSelectedServiceChanged(ServiceSummary? value)
        {
            if (SelectedService is ServiceSummary s)
            {
                IsPricePerHour = s.IsPricePerHour;
                BasePrice = s.Price;
            }
            else
            {
                IsPricePerHour = false;
                BasePrice = 0;
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

        partial void OnBasePriceChanged(double value) => Validate();
        partial void OnIsWeekendOrHolidayChanged(bool value) => Validate();
        partial void OnIsOnlineChanged(bool value) => Validate();
        partial void OnDurationChanged(double value) => Validate();
        partial void OnIsPricePerHourChanged(bool value)
        {
            PriceHeader = IsPricePerHour ? "Price/Hour:" : "Price:";
        }
        partial void OnFrequencyChanged(int value) => Validate();
        partial void OnUnitChanged(FrequencyUnit value) => Validate();

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

            IsPrimaryButtonEnabled = !errors.Any();

            if (!IsPrimaryButtonEnabled)
            {
                Report = new ProposalReport();
                SetExitFunction(string.Join(Environment.NewLine, errors), InfoBarType.Error);
            }
            else
            {
                var input = new ProposalInput(){
                    ServiceName = SelectedService!.Name, 
                    BasePrice = BasePrice, 
                    IsOnline = IsOnline, 
                    TravelAllowance = (decimal)Profile.TravelAllowance,
                    IsWeekendOrHoliday = IsWeekendOrHoliday, 
                    WeekendFee = (decimal) Profile.WeekendFee, 
                    Duration = Duration, 
                    IsPricePerHour = IsPricePerHour,
                    Frequency = Frequency,
                    Unit = Unit
                };
                Report = ProposalService.CalculateProposal(input);
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
