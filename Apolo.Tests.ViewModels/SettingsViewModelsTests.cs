using Apolo.Services;
using Apolo.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models;
using Moq;
using Repository;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    [TestClass]
    public class SettingsViewModelsTests
    {
        private SettingsViewModel _viewModel = null!;

        private Mock<IGeneralRepository> _repositoryMock = null!;
        private Mock<IUserProfileService> _mockUserProfileService = null!;
        private Mock<Excel.IWriter> _writerMock = null!;
        private Mock<Excel.IReader> _readerMock = null!;

        [TestInitialize]
        public void TestInit()
        {
            _repositoryMock = new Mock<IGeneralRepository>();
            _mockUserProfileService = new Mock<IUserProfileService>();
            _writerMock = new Mock<Excel.IWriter>();
            _readerMock = new Mock<Excel.IReader>();

            var userProfile = new UserProfile
            {
                FullName = "Test",
                Address = "Address",
                ZipCode = "Code",
                City = "City",
                Phone = "123456789",
                TaxId = "000000001W",
                Email = "test@email.com",
                BankName = "Bank",
                BankAccount = "123-456-789",
                IvaPercent = 5,
                TravelAllowance = 10,
                WeekendFee = 20
            };

            _mockUserProfileService.Setup(r => r.LoadProfileAsync())
                .ReturnsAsync(userProfile);

            _viewModel = new SettingsViewModel(_repositoryMock.Object, _mockUserProfileService.Object,
                _readerMock.Object, _writerMock.Object);
        }

        void VerifyAction(string? message, InfoBarType severity, bool isOpen, bool isBusy = false, bool contains = false)
        {
            if (contains)
            {
                Assert.IsNotNull(_viewModel.InfoMessage);
                Assert.IsNotNull(message);
                Assert.Contains(message, _viewModel.InfoMessage);
            }
            else
                Assert.AreEqual(message, _viewModel.InfoMessage);
            Assert.AreEqual(isBusy, _viewModel.IsBusy);
            Assert.AreEqual(isOpen, _viewModel.OpenInfoBar);
            Assert.AreEqual(severity, _viewModel.InfoBarType);
        }

        // Save async
        [TestMethod]
        public async Task SaveAsyn_WhenBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.SaveAsync();

            VerifyAction("Can't save settings while busy.", InfoBarType.Warning, isOpen: true, isBusy: true);
        }

        [TestMethod]
        public async Task SaveAsync()
        {
            await _viewModel.SaveAsync();

            VerifyAction("User profile saved successfully.", InfoBarType.Success, isOpen: true);
        }

        // Clear database
        [TestMethod]
        public async Task ClearDatabase_WhenBusy()
        {
            _viewModel.IsBusy = true;
            await _viewModel.ClearDatabaseAsync();
            VerifyAction("Can't clear database while busy.", InfoBarType.Warning, isOpen: true, isBusy: true);
        }

        [TestMethod]
        public async Task ClearDatabase()
        {
            await _viewModel.ClearDatabaseAsync();
            VerifyAction("Database has been clear successfully.", InfoBarType.Success, isOpen: true);
        }

        // Import database from excel
        [TestMethod]
        public async Task ImportFromExcel_WhenBusy()
        {
            _viewModel.IsBusy = true;
            await _viewModel.ImportDatabaseFromExcel("file");
            VerifyAction("Can't import database from Excel while busy.", InfoBarType.Warning, isOpen: true, isBusy: true);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task ImportFromExcel_InvalidFile(string invalidName)
        {
            await _viewModel.ImportDatabaseFromExcel(invalidName);
            VerifyAction("No file selected.", InfoBarType.Warning, isOpen: true);
        }

        [TestMethod]
        public async Task ImportFromExcel_InvalidPath()
        {
            await _viewModel.ImportDatabaseFromExcel("\\invalid_path\\file.xlsm");
            VerifyAction("Directory '\\invalid_path' does not exist.", InfoBarType.Error, isOpen: true);
        }

        [TestMethod]
        public async Task GenerateExportSummary_ShouldCreateFileWithCorrectContent()
        {
            // --- Arrange ---
            // Create a temporary path so we don't clutter the machine
            string tempPath =Path.GetTempPath();
            string file = Path.Combine(tempPath, "Excel.xlsm");
            string fileName = $"Summary_{DateTime.Now:yyyyMMdd_HHmm}.txt";
            string resultPath = Path.Combine(tempPath, fileName);
            Directory.CreateDirectory(tempPath);

            try
            {

                // Arrange
                var data = Helper.GetData();

                _readerMock.Setup(r => r.Services).Returns(data.Services);
                _readerMock.Setup(r => r.Payers).Returns(data.Payers);
                _readerMock.Setup(r => r.Students).Returns(data.Students);
                _readerMock.Setup(r => r.Specifications).Returns(data.Specifications);
                _readerMock.Setup(r => r.Lessons).Returns(data.Lessons);
                _readerMock.Setup(r => r.Invoices).Returns(data.Invoices);

                // --- Act ---
                await _viewModel.ImportDatabaseFromExcel(file);

                _repositoryMock.Verify(r => r.ImportAllDataAsync(data.Services, data.Payers, data.Students, data.Specifications,
                    data.Lessons, data.Invoices), Times.Once);

                string fileContent = await File.ReadAllTextAsync(resultPath);
                VerifyAction($"Summary saved to {resultPath}", InfoBarType.Success, isOpen: true, contains: true);

                // 3. Verify specific data points are inside the string
                StringAssert.Contains(fileContent, "APOLO APP - IMPORT SUMMARY");
                StringAssert.Contains(fileContent, $"- Services Imported: 3");
                StringAssert.Contains(fileContent, $"- Invoices Processed: 3");
                StringAssert.Contains(fileContent, "STATUS: Success");
            }
            finally
            {
                // --- Cleanup ---
                if (File.Exists(resultPath))
                {
                    File.Delete(resultPath);
                }
            }
        }

        // Export database from excel
        [TestMethod]
        public async Task ExportToExcel_WhenBusy()
        {
            _viewModel.IsBusy = true;
            await _viewModel.ExportDatabaseToExcel("folder", "installed_path");
            VerifyAction("Can't export database while busy.", InfoBarType.Warning, isOpen: true, isBusy: true);
        }

        [TestMethod]
        public async Task ExportToExcel_InvalidFolder()
        {
            await _viewModel.ExportDatabaseToExcel("folder", "installed_path");
            VerifyAction("Directory folder does not exist.", InfoBarType.Error, isOpen: true);
        }

        [TestMethod]
        public async Task ExportToExcel()
        {
            // Create a temporary path so we don't clutter the machine
            string tempPath = Path.GetTempPath();
            string file = Path.Combine(tempPath, "Excel.xlsm");
            string fileName = $"Summary_{DateTime.Now:yyyyMMdd_HHmm}.txt";
            string resultPath = Path.Combine(tempPath, fileName);
            Directory.CreateDirectory(tempPath);

            var data = Helper.GetData();

            _repositoryMock.Setup(r => r.GetAllDataAsync()).ReturnsAsync(data);

            await _viewModel.ExportDatabaseToExcel(tempPath, "installed_path");

            _repositoryMock.Verify(r => r.GetAllDataAsync(), Times.Once);

            VerifyAction($"Export completed", InfoBarType.Success, isOpen: true, contains: true);
        }

        [TestMethod]
        public async Task GetPayersActivity()
        {
            await _viewModel.GetPayersActivity();

            Assert.IsFalse(_viewModel.OpenInfoBar);
            Assert.IsFalse(_viewModel.IsBusy);

            _repositoryMock.Verify(r => r.GetPayersWithActivityAsync(), Times.Once);
        }

        [TestMethod]
        public async Task Archive_WhileBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.ArchiveOldData([Guid.NewGuid()]);

            VerifyAction("Can't archive data while busy.", InfoBarType.Warning, isOpen: true, isBusy: true);

            _repositoryMock.Verify(r => r.ArchiveOldDataAsync(It.IsAny<List<Guid>>()), Times.Never);
        }

        [TestMethod]
        public async Task Archive_NoPayersSelected()
        {
            await _viewModel.ArchiveOldData([]);

            VerifyAction("No payers were selected.", InfoBarType.Info, isOpen: true);

            _repositoryMock.Verify(r => r.ArchiveOldDataAsync(It.IsAny<List<Guid>>()), Times.Never);
        }

        [TestMethod]
        public async Task Archive_Exception()
        {
            List<Guid> ids = [Guid.NewGuid()];

            _repositoryMock.Setup(r => r.ArchiveOldDataAsync(ids))
                .ThrowsAsync(new DbUpdateException("Database connection lost."));

            await _viewModel.ArchiveOldData(ids);

            VerifyAction("Database connection lost.", InfoBarType.Error, isOpen: true);

            _repositoryMock.Verify(r => r.ArchiveOldDataAsync(ids), Times.Once);
        }

        [TestMethod]
        public async Task Archive()
        {
            List<Guid> ids = [Guid.NewGuid()];

            await _viewModel.ArchiveOldData(ids);

            VerifyAction("Archived data successfully.", InfoBarType.Success, isOpen: true);

            _repositoryMock.Verify(r => r.ArchiveOldDataAsync(ids), Times.Once);
        }
    }
}
