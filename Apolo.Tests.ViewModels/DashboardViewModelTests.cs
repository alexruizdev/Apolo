using Apolo.Services;
using Moq;
using Repository;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    [TestClass]
    public class DashboardViewModelTests
    {
        private DashboardViewModel _viewModel = null!;

        private Mock<IDashboardRepository> _repositoryMock = null!;
        private Mock<IStringLocalizer> _localizerMock = null!;

        [TestInitialize]
        public void TestInit()
        {
            _repositoryMock = new Mock<IDashboardRepository>();
            _localizerMock = new Mock<IStringLocalizer>();

            _viewModel = new DashboardViewModel(_repositoryMock.Object, _localizerMock.Object);
        }

        [TestMethod]
        public async Task Load()
        {
            List<decimal> current = [950, 1300, 925, 850, 375, 0, 659.5m, 730, 3560, 1020, 1560, 2490];
            List<decimal> previous = [720, 1460, 900, 835, 300, 55, 780.5m, 900, 3200, 870, 1940, 2046];

            Dictionary<string, decimal> topPayers = new(){ 
                { "Payer 1", 350}, 
                { "Payer 2", 164 }, 
                { "Payer 3", 60 }, 
                { "Payer 4", 55 },
                { "Payer 5", 20}
            };

            // Initialize Filters
            _viewModel.SelectedYear = 2025;
            _viewModel.SelectedMonth = _viewModel.AvailableMonths.First(m => m.Value == 9);

            // Is busy
            _viewModel.IsBusy = true;
            await _viewModel.LoadAsync();
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Warning, _viewModel.InfoBarType);

            // Normal execution
            _viewModel.IsBusy = false;

            // Mock repository
            _repositoryMock.Setup(r => r.GetTotalUnpaidAmountAsync()).ReturnsAsync(595);
            _repositoryMock.Setup(r => r.GetMonthlyLessonCountAsync(2025, 9)).ReturnsAsync(10);
            _repositoryMock.Setup(r => r.GetMonthlyEarningsAsync(2025, 9)).ReturnsAsync(1650);
            _repositoryMock.Setup(r => r.GetMonthlyEarningsAsync(2025, 8)).ReturnsAsync(1935.5m);
            _repositoryMock.Setup(r => r.GetYearlyIncomeTrendAsync(2025)).ReturnsAsync(current);
            _repositoryMock.Setup(r => r.GetYearlyIncomeTrendAsync(2024)).ReturnsAsync(previous);
            _repositoryMock.Setup(r => r.GetTopPayersThisMonthAsync(2025, 9)).ReturnsAsync(topPayers);
            _repositoryMock.Setup(r => r.GetPaidVsUnpaidCountThisMonthAsync(2025, 9)).ReturnsAsync((10, 3));

            await _viewModel.LoadAsync();

            // Check - Main Info
            _repositoryMock.Verify(r => r.GetTotalUnpaidAmountAsync(), Times.Once);
            Assert.AreEqual(595, _viewModel.TotalUnpaidAmount);

            _repositoryMock.Verify(r => r.GetMonthlyLessonCountAsync(2025, 9), Times.Once);
            Assert.AreEqual(10, _viewModel.LessonsThisMonth);

            _repositoryMock.Verify(r => r.GetMonthlyEarningsAsync(2025, 9), Times.Once);
            Assert.AreEqual(1650, _viewModel.CurrentMonthEarnings);

            _repositoryMock.Verify(r => r.GetMonthlyEarningsAsync(2025, 8), Times.Once);
            Assert.AreEqual(1935.5m, _viewModel.PreviousMonthEarnings);
            Assert.AreEqual("-14.8%", _viewModel.EarningsTrend);

            // Check - Yearly income
            _repositoryMock.Verify(r => r.GetYearlyIncomeTrendAsync(2025), Times.Once);
            _repositoryMock.Verify(r => r.GetYearlyIncomeTrendAsync(2024), Times.Once);
            Assert.HasCount(2, _viewModel.IncomeTrendSeries);
            Assert.IsNotNull(_viewModel.IncomeTrendSeries[0].Values);
            Assert.IsNotNull(_viewModel.IncomeTrendSeries[1].Values);

            // Check - Top payers
            _repositoryMock.Verify(r => r.GetTopPayersThisMonthAsync(2025, 9), Times.Once);

            // Check - Paid vs Unpaid
            _repositoryMock.Verify(r => r.GetPaidVsUnpaidCountThisMonthAsync(2025, 9), Times.Once);

        }
    }
}
