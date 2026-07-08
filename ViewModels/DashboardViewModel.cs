using Apolo.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using Repository;
using SkiaSharp;
using System.Globalization;

namespace ViewModels
{
    public record MonthOption(int Value, string Name);
    public partial class DashboardViewModel : BaseViewModel
    {
        readonly IDashboardRepository _dashboardRepository;

        // Filters
        [ObservableProperty] private int _selectedYear;
        [ObservableProperty] private MonthOption _selectedMonth;

        public List<int> AvailableYears { get; }
        public List<MonthOption> AvailableMonths { get; }

        // KPIs
        [ObservableProperty] private decimal _currentMonthEarnings;
        [ObservableProperty] private decimal _previousMonthEarnings;
        [ObservableProperty] private decimal _totalUnpaidAmount;
        [ObservableProperty] private int _lessonsThisMonth;
        [ObservableProperty] private string _earningsTrend = string.Empty;

        // Charts
        [ObservableProperty] private ISeries[] _incomeTrendSeries = null!;
        [ObservableProperty] private List<Axis> _xAxes =
        [
            new Axis
            {
                Labels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"]
            }
        ];
        [ObservableProperty] private ISeries[] _topPayersSeries = null!;
        [ObservableProperty] private Axis[] _topPayersYAxes = null!;
        [ObservableProperty] private Axis[] _topPayersXAxes = null!;
        [ObservableProperty] private ISeries[] _paidVsUnpaidSeries = null!;

        // Messages 
        protected static string Message_Load_Error => "Messages/Load_Dashboard_Error";
        protected static string Message_Income => "Messages/Dashboard_Income";
        protected static string Message_Total_Amount => "Messages/Dashboard_Total_Amount";
        protected static string Message_Paid => "Messages/Paid";
        protected static string Message_Unpaid => "Messages/Unpaid";
        protected static string Message_New => "Messages/New";

        public DashboardViewModel(IDashboardRepository dashboardRepository, IStringLocalizer stringLocalizer)
            : base(stringLocalizer)
        {
            _dashboardRepository = dashboardRepository;

            // Initialize Filters
            var now = DateTime.Now;
            AvailableYears = [.. Enumerable.Range(now.Year - 5, 6).OrderByDescending(y => y)]; // Last 5 years + current
            AvailableMonths = [.. Enumerable.Range(1, 12).Select(m => new MonthOption(m, CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(m)))];

            SelectedYear = now.Year;
            SelectedMonth = AvailableMonths.First(m => m.Value == now.Month);
        }

        [RelayCommand]
        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                SetExitBusy(Message_Load_Error);
                return;
            }

            SetEnterFunction();

            int year = SelectedYear;
            int month = SelectedMonth.Value;

            var currentPeriod = new DateTime(year, month, 1);
            var prevPeriod = currentPeriod.AddMonths(-1);

            // Load KPIs
            TotalUnpaidAmount = await _dashboardRepository.GetTotalUnpaidAmountAsync();
            LessonsThisMonth = await _dashboardRepository.GetMonthlyLessonCountAsync(year, month);
            CurrentMonthEarnings = await _dashboardRepository.GetMonthlyEarningsAsync(year, month);
            PreviousMonthEarnings = await _dashboardRepository.GetMonthlyEarningsAsync(prevPeriod.Year, prevPeriod.Month);

            CalculateEarningsTrend();

            // Load Charts
            var currentYearIncomes = await _dashboardRepository.GetYearlyIncomeTrendAsync(year);
            var prevYearIncomes = await _dashboardRepository.GetYearlyIncomeTrendAsync(year - 1);

            // 1. Yearly Income Trend (Current vs Previous Year)
            IncomeTrendSeries =
            [
                new LineSeries<decimal>
                {
                    Values = currentYearIncomes,
                    Name = $"{_loc.Get(Message_Income, year)}",
                    Fill = new SolidColorPaint(SKColors.LightBlue.WithAlpha(90)),
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 3 },
                    GeometrySize = 10
                },
                new LineSeries<decimal>
                    {
                        Values = prevYearIncomes,
                        Name = $"{_loc.Get(Message_Income, year - 1)}",
                        Fill = null, // No fill for the comparison line to keep it clean
                        Stroke = new SolidColorPaint(SKColors.Gray)
                        {
                            StrokeThickness = 2,
                            PathEffect = new DashEffect([5, 5]) // Dashed line
                        },
                        GeometrySize = 0 // Hide points on the comparison line
                    }
            ];

            // 2. Top Payers (Horizontal Bar Chart)
            var topPayers = await _dashboardRepository.GetTopPayersThisMonthAsync(year, month);
            TopPayersSeries =
            [
                new ColumnSeries<decimal>
                {
                    Values = [.. topPayers.Values],
                    Name = _loc.Get(Message_Total_Amount),
                    MaxBarWidth = 40, // Keeps the bars from getting too thick
                    DataLabelsPaint = new SolidColorPaint(new SKColor(30, 30, 30)),
                    DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Right,
                    DataLabelsFormatter = point => point.Model.ToString("C")
                }
            ];

            TopPayersYAxes =
            [
                new Axis
                {
                    Labels = [.. topPayers.Keys],
                    IsInverted = true, // CRITICAL: Puts the #1 highest payer at the TOP of the chart
                    LabelsPaint = new SolidColorPaint(new SKColor(100, 100, 100)),
                    TextSize = 14,
                    SeparatorsPaint = null // Removes the grid lines behind the names for a cleaner look
                }
            ];

            TopPayersXAxes =
            [
                new Axis
                {
                    Labeler = value => value.ToString("C"), // Formats the bottom axis as currency
                    MinLimit = 0 // Ensures the bars always start exactly from 0
                }
            ];

            // 3. Paid vs Unpaid
            var (paid, unpaid) = await _dashboardRepository.GetPaidVsUnpaidCountThisMonthAsync(year, month);
            PaidVsUnpaidSeries =
            [
                new PieSeries<int> { Values = [paid], Name = _loc.Get(Message_Paid) },
                new PieSeries<int> { Values = [unpaid], Name = _loc.Get(Message_Unpaid) }
            ];
            SetExitFunction();
        }

        private void CalculateEarningsTrend()
        {
            if (PreviousMonthEarnings == 0)
            {
                EarningsTrend = CurrentMonthEarnings > 0 ? _loc.Get(Message_New) : "0%";
                return;
            }

            var difference = CurrentMonthEarnings - PreviousMonthEarnings;
            var percentage = difference / PreviousMonthEarnings * 100;
            EarningsTrend = $"{(percentage >= 0 ? "+" : "")}{percentage:F1}%";
        }
    }
}
