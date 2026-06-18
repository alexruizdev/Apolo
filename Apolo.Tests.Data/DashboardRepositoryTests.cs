using Repository;

namespace Apolo.Tests.Data
{
    [TestClass]
    public class DashboardRepositoryTests : RepositoryTests
    {
        private DashboardRepository _repository = null!;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _repository = new DashboardRepository(_context, _archiveContext);
        }


        [TestMethod]
        public async Task TotalUnpaidAmount()
        {
            var total = await _repository.GetTotalUnpaidAmountAsync();
            Assert.AreEqual(595, total);
        }

        [TestMethod]
        public async Task MonthlyEarning()
        {
            var earnings = await _repository.GetMonthlyEarningsAsync(2025, 09);
            Assert.AreEqual(285, earnings);
            earnings = await _repository.GetMonthlyEarningsAsync(2024, 04);
            Assert.AreEqual(35, earnings);
        }

        [TestMethod]
        public async Task LessonCount()
        {
            var count = await _repository.GetMonthlyLessonCountAsync(2025, 09);
            Assert.AreEqual(4, count);
            count = await _repository.GetMonthlyLessonCountAsync(2024, 04);
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task YearlyIncome()
        {
            var income = await _repository.GetYearlyIncomeTrendAsync(2024);
            Assert.HasCount(12, income);
            Assert.AreEqual(35, income[0]);
            Assert.AreEqual(101, income[1]);
            Assert.AreEqual(193, income[2]);
            Assert.AreEqual(35, income[3]);
            Assert.AreEqual(0, income[4]);
            Assert.AreEqual(112.5m, income[5]);
            Assert.AreEqual(310, income[6]);
            Assert.AreEqual(180, income[7]);
            Assert.AreEqual(175, income[8]);
            Assert.AreEqual(114.5m, income[9]);
            Assert.AreEqual(136, income[10]);
            Assert.AreEqual(250, income[11]);
        }

        [TestMethod]
        public async Task TopPayers()
        {
            var top = await _repository.GetTopPayersThisMonthAsync(2025, 09);
            Assert.HasCount(2, top);
            Assert.AreEqual(142.5m, top.First().Value);
            top = await _repository.GetTopPayersThisMonthAsync(2024, 04);
            Assert.HasCount(1, top);
            Assert.AreEqual(35, top.First().Value);
        }

        [TestMethod]
        public async Task PaidVsUnpaid()
        {
            var (paid, unpaid) = await _repository.GetPaidVsUnpaidCountThisMonthAsync(2025, 09);
            Assert.AreEqual(4, paid);
            Assert.AreEqual(0, unpaid);
            (paid, unpaid) = await _repository.GetPaidVsUnpaidCountThisMonthAsync(2024, 04);
            Assert.AreEqual(1, paid);
            Assert.AreEqual(0, unpaid);
        }
    }
}
