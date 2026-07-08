using Apolo.Services;
using Models;
using Moq;
using PDF;
using Repository;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    [TestClass]
    public class ProposalViewModelTests
    {
        private ProposalViewModel _viewModel = null!;

        private Mock<IServiceRepository> _mockServiceRepo = null!;
        private Mock<IUserProfileService> _mockUserProfileService = null!;
        private Mock<IReportWriter> _mockPDFWriter = null!;
        private Mock<IStringLocalizer> _localizerMock = null!;


        [TestInitialize]
        public void TestInit()
        {
            _mockServiceRepo = new Mock<IServiceRepository>();
            _mockPDFWriter = new Mock<IReportWriter>();
            _mockUserProfileService = new Mock<IUserProfileService>();
            _localizerMock = new Mock<IStringLocalizer>();

            var userProfile = new UserProfile
            {
                FullName = "Test User",
                Address = "Test Address",
                ZipCode = "12345",
                City = "Test City",
                Phone = "123-456-7890",
                TaxId = "TAX123456",
                Email = "test@example.com",
                BankName = "Test Bank",
                BankAccount = "123456234567",
                IvaPercent = 25,
                TravelAllowance = 10,
                WeekendFee = 20,
                BillingFolder = "\\somepath\\invented"
            };

            _mockUserProfileService.Setup(r => r.LoadProfileAsync())
                .ReturnsAsync(userProfile);

            _viewModel = new ProposalViewModel(_mockServiceRepo.Object, _mockUserProfileService.Object,
                _mockPDFWriter.Object, _localizerMock.Object);
        }

        [TestMethod]
        public async Task Load()
        {
            var data = new DummyData();

            _viewModel.IsBusy = true;
            await _viewModel.LoadAsync();
            Assert.AreEqual("Can't load services while busy.", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Warning, _viewModel.InfoBarType);

            _viewModel.IsBusy = false;

            // Mock repository
            _mockServiceRepo.Setup(r => r.GetServicesAsync()).ReturnsAsync(data.ServiceSummaries);

            await _viewModel.LoadAsync();

            _mockServiceRepo.Verify(r => r.GetServicesAsync(), Times.Once);
            Assert.HasCount(6, _viewModel.Services);

            Assert.IsNotNull(_viewModel.InfoMessage);
            Assert.Contains("You must select a service.", _viewModel.InfoMessage);
            Assert.Contains("Price must be a positive integer.", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Error, _viewModel.InfoBarType);
            Assert.IsNull(_viewModel.SelectedService);
            Assert.IsFalse(_viewModel.IsPricePerHour);
            Assert.AreEqual(0, _viewModel.BasePrice);
            Assert.AreEqual("Price:", _viewModel.PriceHeader);

            // Input
            Assert.AreEqual(string.Empty, _viewModel._input.ServiceName);
            Assert.AreEqual(0, _viewModel._input.BasePrice);
            Assert.IsFalse(_viewModel._input.IsOnline);
            Assert.AreEqual(10, _viewModel._input.TravelAllowance);
            Assert.IsFalse(_viewModel._input.IsWeekendOrHoliday);
            Assert.AreEqual(20, _viewModel._input.WeekendFee);
            Assert.IsFalse(_viewModel._input.IsPricePerHour);
            Assert.AreEqual(0, _viewModel._input.Duration);
            Assert.AreEqual(1, _viewModel._input.Frequency);
            Assert.AreEqual(FrequencyUnit.PerWeek, _viewModel._input.Unit);

            // Select service
            _viewModel.SelectedService =_viewModel.Services.First();
            Assert.IsNotNull(_viewModel.InfoMessage);
            Assert.Contains("Duration must be a positive integer.", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Error, _viewModel.InfoBarType);
            Assert.IsNotNull(_viewModel.SelectedService);
            Assert.IsNotNull(_viewModel.Report);
            Assert.IsEmpty(_viewModel.Report.ServiceName);
            Assert.AreEqual(0, _viewModel.Report.PricePerSession);
            Assert.AreEqual("Price/Hour:", _viewModel.PriceHeader);
            Assert.AreEqual(40, _viewModel.BasePrice);
            Assert.IsFalse(_viewModel.IsOnline);
            Assert.IsFalse(_viewModel.IsWeekendOrHoliday);
            Assert.AreEqual(1, _viewModel.Frequency);
            Assert.AreEqual(FrequencyUnit.PerWeek, _viewModel.Unit);

            // Select duration
            _viewModel.Duration = 90;
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsFalse(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Success, _viewModel.InfoBarType);
            Assert.AreEqual("1 time(s) / week (4.3 total sessions)", _viewModel.BudgetMinusFrequencyString);

            _viewModel.BasePrice = 50;
            _viewModel.IsOnline = true;
            _viewModel.IsWeekendOrHoliday = true;
            _viewModel.Frequency = 2;
            _viewModel.Unit = FrequencyUnit.PerMonth;

            // Input
            Assert.AreEqual("Math Tutoring", _viewModel._input.ServiceName);
            Assert.AreEqual(50, _viewModel._input.BasePrice);
            Assert.IsTrue(_viewModel._input.IsOnline);
            Assert.AreEqual(10, _viewModel._input.TravelAllowance);
            Assert.IsTrue(_viewModel._input.IsWeekendOrHoliday);
            Assert.AreEqual(20, _viewModel._input.WeekendFee);
            Assert.IsTrue(_viewModel._input.IsPricePerHour);
            Assert.AreEqual(90, _viewModel._input.Duration);
            Assert.AreEqual(2, _viewModel._input.Frequency);
            Assert.AreEqual(FrequencyUnit.PerMonth, _viewModel._input.Unit);


        }

        [TestMethod]
        public async Task GeneratePDFReport()
        {
            _viewModel.IsBusy = true;
            await _viewModel.GeneratePDF();
            Assert.IsNotNull(_viewModel.InfoMessage);
            Assert.AreEqual("Can't generate proposal while busy.", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Warning, _viewModel.InfoBarType);

            _viewModel.IsBusy = false;

            // Create a temporary path so we don't clutter the machine
            string tempPath = Path.Combine(Path.GetTempPath(), "invalid");
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath);
            
            _viewModel.Profile.BillingFolder = tempPath;

            await _viewModel.GeneratePDF();

            Assert.IsNotNull(_viewModel.InfoMessage);
            Assert.AreEqual("Can't export proposal without setting 'Billing Folder'", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Error, _viewModel.InfoBarType);


            tempPath = Path.Combine(Path.GetTempPath(), "valid");
            Directory.CreateDirectory(tempPath);
            _viewModel.Profile.BillingFolder = tempPath;

            await _viewModel.GeneratePDF();

            var date = DateTime.Now.ToString("dd-MM-yyyy");
            var filename = Path.Combine(tempPath, "Budget",  $"Proposal_{date}.pdf");
            Assert.IsNotNull(_viewModel.InfoMessage);
            Assert.AreEqual($"Generated proposal successfully: {filename}.", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Success, _viewModel.InfoBarType);

            _mockPDFWriter.Verify(r => r.GenerateProposal(filename, _viewModel.Report), Times.Once);

            _mockPDFWriter.Setup(r => r.GenerateProposal(filename, _viewModel.Report))
                    .Throws(new Exception("Something bad happened."));

            await _viewModel.GeneratePDF();

            _mockPDFWriter.Verify(r => r.GenerateProposal(filename, _viewModel.Report), Times.Exactly(2));
            Assert.IsNotNull(_viewModel.InfoMessage);
            Assert.AreEqual($"Error generating proposal '{filename}': Something bad happened.", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Error, _viewModel.InfoBarType);
        }
    }
}
