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
    public class InvoicesViewModelsTests
    {
        private Mock<IInvoiceRepository> _mockInvoiceRepo = null!;
        private Mock<IPayerRepository> _mockPayerRepo = null!;
        private Mock<IUserProfileService> _mockUserProfileService = null!;
        private Mock<PDF.IWriter> _mockPDFWriter = null!;
        private InvoicesViewModel _viewModel = null!;

        [TestInitialize]
        public void TestInit()
        {
            _mockInvoiceRepo = new Mock<IInvoiceRepository>();
            _mockPayerRepo = new Mock<IPayerRepository>();
            _mockUserProfileService = new Mock<IUserProfileService>();
            _mockPDFWriter = new Mock<PDF.IWriter>();

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
                WeekendFee = 20
            };

            _mockUserProfileService.Setup(r => r.LoadProfileAsync())
                .ReturnsAsync(userProfile);

            _viewModel = new InvoicesViewModel(_mockInvoiceRepo.Object, _mockPayerRepo.Object, _mockUserProfileService.Object,
                _mockPDFWriter.Object);
        }

        void VerifyAction(string? message, InfoBarType severity, bool isOpen, int payersCount, int count, decimal totalSelected, decimal total, bool isBusy = false)
        {
            Assert.HasCount(payersCount, _viewModel.Payers);
            Assert.HasCount(count, _viewModel.Attendances);
            Assert.AreEqual(totalSelected, _viewModel.TotalSelected);
            Assert.AreEqual(total, _viewModel.TotalAll);
            Assert.AreEqual(message, _viewModel.InfoMessage);
            Assert.AreEqual(isBusy, _viewModel.IsBusy);
        }

        [TestMethod]
        public void SelectionStateEmpty()
        {
            _viewModel.IsBusy = true;

            _viewModel.SelectionState = null;

            VerifyAction("Can't update selection while busy.", InfoBarType.Warning, 
                isOpen: true, payersCount: 0, count: 0, totalSelected: 0, total: 0, isBusy:true);

        }

        // Load async

        [TestMethod]
        public async Task LoadAsync_WhenBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.LoadAsync();

            VerifyAction("Can't load payers while busy.", InfoBarType.Warning, isOpen: true,
                payersCount: 0, count: 0, isBusy: true, total: 0, totalSelected: 0);

            _mockPayerRepo.Verify(r => r.GetPayerOptionsAsync(), Times.Never);
        }

        [TestMethod]
        public async Task LoadAsync_PopulatesCollection()
        { 
            var firstLoad = new List<PayerOption>();
            firstLoad.Add(new PayerOption(Guid.NewGuid(), "Old payer 1"));
            firstLoad.Add(new PayerOption(Guid.NewGuid(), "Old payer 2"));
            var secondLoad = new List<PayerOption>();
            secondLoad.Add(new PayerOption(Guid.NewGuid(), "New payer 1"));
            secondLoad.Add(new PayerOption(Guid.NewGuid(), "New payer 2"));

            _mockPayerRepo.SetupSequence(r => r.GetPayerOptionsAsync())
                .ReturnsAsync(firstLoad)
                .ReturnsAsync(secondLoad);

            await _viewModel.LoadAsync(); // test that Payers.Clear() is working
            await _viewModel.LoadAsync(); // If LoadAsync is called twice, you should not have duplicate items in your list

            _mockPayerRepo.Verify(r => r.GetPayerOptionsAsync(), Times.Exactly(2));

            VerifyAction(null, InfoBarType.Success, isOpen: false,
                payersCount: 2, count: 0, isBusy: false, total: 0, totalSelected: 0);
            Assert.AreEqual("New payer 1", _viewModel.Payers[0].FullName);
            Assert.AreEqual("New payer 2", _viewModel.Payers[1].FullName);
        }

        [TestMethod]
        public async Task LoadAsync_EmptyCollection()
        {
            _mockPayerRepo.SetupSequence(r => r.GetPayerOptionsAsync())
                .ReturnsAsync(new List<PayerOption>());

            await _viewModel.LoadAsync(); 

            _mockPayerRepo.Verify(r => r.GetPayerOptionsAsync(), Times.Once);

            VerifyAction(null, InfoBarType.Success, isOpen: false,
                payersCount: 0, count: 0, isBusy: false, total: 0, totalSelected: 0);
        }

        // Load Attendances

        [TestMethod]
        public async Task LoadAttendances_WhileBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.LoadAttendancesAsync();

            VerifyAction("Can't load attendances while busy.", InfoBarType.Warning, isOpen: true,
                payersCount: 0, count: 0, isBusy: true, total: 0, totalSelected: 0);

            _mockInvoiceRepo.Verify(r => r.GetInvoiceAttendancesAsync(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task LoadAttendances_PopulatesCollection()
        {
            var payer = new PayerOption(Guid.NewGuid(), "Payer");
            var attendanceId1 = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var studentId1 = Guid.NewGuid();

            _viewModel.Payers.Add(payer);
            _viewModel.SelectedPayerId = payer.Id;

            var firstLoad = new List<InvoiceAttendanceSummary>();
            firstLoad.Add(new InvoiceAttendanceSummary(attendanceId1, lessonId, new DateOnly(2024, 1, 1), "Old Lesson", studentId1, "Student 1", 100));
            firstLoad.Add(new InvoiceAttendanceSummary(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 2), "Old Lesson", Guid.NewGuid(), "Student 2", 200));
            firstLoad.Add(new InvoiceAttendanceSummary(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 3), "Old Lesson", Guid.NewGuid(), "Student 3", 300));

            var secondLoad = new List<InvoiceAttendanceSummary>();
            secondLoad.Add(new InvoiceAttendanceSummary(attendanceId1, lessonId, new DateOnly(2025, 1, 1), "New Lesson", studentId1, "Student 1", 50));
            secondLoad.Add(new InvoiceAttendanceSummary(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2025, 1, 2), "New Lesson", Guid.NewGuid(), "Student 2", 500));
            secondLoad.Add(new InvoiceAttendanceSummary(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2025, 1, 3), "New Lesson", Guid.NewGuid(), "Student 3", 25));

            _mockInvoiceRepo.SetupSequence(r => r.GetInvoiceAttendancesAsync(payer.Id))
                .ReturnsAsync(firstLoad)
                .ReturnsAsync(secondLoad);

            await _viewModel.LoadAttendancesAsync(); // test that Attendances.Clear() is working
            VerifyAction(null, InfoBarType.Success, isOpen: false,
                payersCount: 1, count: 3, isBusy: false, total: 600, totalSelected: 0);

            await _viewModel.LoadAttendancesAsync(); // If LoadAsync is called twice, you should not have duplicate items in your list
            VerifyAction(null, InfoBarType.Success, isOpen: false,
                payersCount: 1, count: 3, isBusy: false, total: 575, totalSelected: 0);

            _mockInvoiceRepo.Verify(r => r.GetInvoiceAttendancesAsync(payer.Id), Times.Exactly(2));

            {
                var line = _viewModel.Attendances.First();
                Assert.AreEqual(attendanceId1, line.AttendanceId);
                Assert.AreEqual(lessonId, line.LessonId);
                Assert.AreEqual(new DateOnly(2025, 1, 1), line.Date);
                Assert.AreEqual("New Lesson", line.LessonName);
                Assert.AreEqual(studentId1, line.StudentId);
                Assert.AreEqual("Student 1", line.StudentName);
                Assert.AreEqual(50, line.Price);
                Assert.IsFalse(line.IsSelected);
            }

            Assert.IsFalse(_viewModel.SelectionState);

            // Let's play with selection
            _viewModel.Attendances[0].IsSelected = true;
            Assert.IsNull(_viewModel.SelectionState);
            Assert.AreEqual(50, _viewModel.TotalSelected);
            Assert.AreEqual(575, _viewModel.TotalAll);

            // Select all
            _viewModel.SelectionState = true;
            Assert.IsTrue(_viewModel.SelectionState);
            Assert.AreEqual(575, _viewModel.TotalSelected);
            Assert.AreEqual(575, _viewModel.TotalAll);
            Assert.IsFalse(_viewModel.IsBusy);

            // Nothing
            _viewModel.SelectionState = null;
            Assert.IsTrue( _viewModel.SelectionState); // Nothing has changed
            Assert.AreEqual(575, _viewModel.TotalSelected);
            Assert.AreEqual(575, _viewModel.TotalAll);
            Assert.IsFalse(_viewModel.IsBusy);

            // Desect all
            _viewModel.SelectionState = false;
            Assert.IsFalse(_viewModel.SelectionState);
            Assert.AreEqual(0, _viewModel.TotalSelected);
            Assert.AreEqual(575, _viewModel.TotalAll);
            Assert.IsFalse(_viewModel.IsBusy);

        }

        [TestMethod]
        public async Task LoadAttendances_EmptyCollection()
        {
            var payer = new PayerOption(Guid.NewGuid(), "Payer");

            _viewModel.Payers.Add(payer);
            _viewModel.SelectedPayerId = payer.Id;

            _mockInvoiceRepo.SetupSequence(r => r.GetInvoiceAttendancesAsync(payer.Id))
                .ReturnsAsync(new List<InvoiceAttendanceSummary>());

            await _viewModel.LoadAttendancesAsync(); // test that Attendances.Clear() is working
            VerifyAction(null, InfoBarType.Success, isOpen: false,
                payersCount: 1, count: 0, isBusy: false, total: 0, totalSelected: 0);

            _mockInvoiceRepo.Verify(r => r.GetInvoiceAttendancesAsync(payer.Id), Times.Once);

            Assert.IsFalse(_viewModel.SelectionState);

            // Select all
            _viewModel.SelectionState = true;
            Assert.IsFalse(_viewModel.SelectionState);
            Assert.AreEqual(0, _viewModel.TotalSelected);
            Assert.AreEqual(0, _viewModel.TotalAll);
            Assert.IsFalse(_viewModel.IsBusy);

            // Nothing
            _viewModel.SelectionState = null;
            Assert.IsFalse(_viewModel.SelectionState);
            Assert.AreEqual(0, _viewModel.TotalSelected);
            Assert.AreEqual(0, _viewModel.TotalAll);
            Assert.IsFalse(_viewModel.IsBusy);

            // Desect all
            _viewModel.SelectionState = false;
            Assert.IsFalse(_viewModel.SelectionState);
            Assert.AreEqual(0, _viewModel.TotalSelected);
            Assert.AreEqual(0, _viewModel.TotalAll);
            Assert.IsFalse(_viewModel.IsBusy);
        }

        // Mark selected as paid

        private List<Guid> ArrangeForMarkAsPaid(bool someSelected = false)
        {
            List<Guid> ids = [ Guid.NewGuid(), Guid.NewGuid()];
            _viewModel.Attendances.Add(new InvoiceLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", Guid.NewGuid(), "Student 1", 50));
            _viewModel.Attendances.Add(new InvoiceLine(ids[0], Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", Guid.NewGuid(), "Student 1", 30)
            { IsSelected = someSelected });
            _viewModel.Attendances.Add(new InvoiceLine(ids[1], Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", Guid.NewGuid(), "Student 1", 35.5m)
            { IsSelected = someSelected });
            _viewModel.Attendances.Add(new InvoiceLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", Guid.NewGuid(), "Student 1", 192.1m));
            return ids;
        }

        private async Task ActForMarkAsPaid(List<Guid> ids, bool dbError = false)
        {
            if (dbError)
            {
                _mockInvoiceRepo.Setup(r => r.UpdateAttendancesAsync(ids))
                    .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }

            await _viewModel.MarkSelectedAsPaidAsync();
        }

        private void AssertForMarkAsPaid(List<Guid> ids, bool success,
            string? infoMessage, InfoBarType severity, bool isBusy = false, bool dbError = false)
        {
            if (success || dbError)
            {
                _mockInvoiceRepo.Verify(r => r.UpdateAttendancesAsync(ids), Times.Once);
            }
            else
            {
                _mockInvoiceRepo.Verify(r => r.UpdateAttendancesAsync(It.IsAny<IReadOnlyList<Guid>>()), Times.Never);
            }

            if (dbError)
            {
                VerifyAction(infoMessage, severity, isOpen: true, payersCount: 0, count: 4,
                totalSelected: 65.5m, total: 307.6m, isBusy: isBusy);
            }
            else if (success)
            {
                VerifyAction(infoMessage, severity, isOpen: true, payersCount: 0, count: 2,
                totalSelected: 0, total: 242.1m, isBusy: isBusy);
            }
            else
            {
                VerifyAction(infoMessage, severity, isOpen: true, payersCount: 0, count: 4, 
                    totalSelected: 0, total: 307.6m, isBusy: isBusy);
            }

        }

        [TestMethod]
        public async Task MarkAsPaid_WhileBusy()
        {
            _viewModel.IsBusy = true;

            var ids = ArrangeForMarkAsPaid();
            await ActForMarkAsPaid(ids);
            AssertForMarkAsPaid(ids, success: false, infoMessage: "Can't mark attendances as paid while busy.",
                severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task MarkAsPaid_NoSelection()
        {
            var ids = ArrangeForMarkAsPaid();
            await ActForMarkAsPaid(ids);
            AssertForMarkAsPaid(ids, success: false, infoMessage: "Please, select first an attendance to mark them as paid.",
                severity: InfoBarType.Info);
        }

        [TestMethod]
        public async Task MarkAsPaid_DBError()
        {
            var ids = ArrangeForMarkAsPaid(someSelected: true);
            await ActForMarkAsPaid(ids, dbError: true);
            AssertForMarkAsPaid(ids, success: false, dbError: true, infoMessage: "Constraint failed.", 
                severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task MarkAsPaid()
        {
            var ids = ArrangeForMarkAsPaid(someSelected: true);
            await ActForMarkAsPaid(ids);
            AssertForMarkAsPaid(ids, success: true, infoMessage: "2 were marked as paid successfully.", 
                severity: InfoBarType.Success);
        }

        // Generate invoice

        private void ArrangeForGenerateInvoice(bool selectPayer = true, bool selectAttendances = true)
        {
            var payer = new PayerOption(Guid.NewGuid(), "Payer");

            _viewModel.Payers.Add(payer);

            if (selectPayer)
                _viewModel.SelectedPayerId = payer.Id;

            _viewModel.Attendances.Add(new InvoiceLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", Guid.NewGuid(), "Student 1", 50));
            _viewModel.Attendances.Add(new InvoiceLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", Guid.NewGuid(), "Student 1", 30)
            { IsSelected = selectAttendances });
            _viewModel.Attendances.Add(new InvoiceLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", Guid.NewGuid(), "Student 1", 35.5m)
            { IsSelected = selectAttendances });
            _viewModel.Attendances.Add(new InvoiceLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", Guid.NewGuid(), "Student 1", 192.1m));
        }

        private async Task ActForForGenerateInvoice(bool dbError = false)
        {
            var payerId = _viewModel.SelectedPayerId ?? Guid.NewGuid();

            var ids = _viewModel.Attendances.Where(a => a.IsSelected).Select(a => a.AttendanceId).ToArray();

            _mockPayerRepo.Setup(r => r.GetPayerSummaryNoOutstandingAsync(payerId))
                .ReturnsAsync(new PayerSummary(payerId, "Payer", "1", 0, null, null, null, null));
            
            if (dbError)
            {
                _mockInvoiceRepo.Setup(r => r.CreateInvoiceAsync(payerId, ids, null))
                    .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            else 
            {
                _mockInvoiceRepo.Setup(r => r.CreateInvoiceAsync(payerId, ids, null))
                    .ReturnsAsync((1, "Invoice_Name"));
            }

            await _viewModel.GenerateInvoice("\\somepath\\invented", null);
        }

        private void AssertForGenerateInvoice(string? infoMessage, InfoBarType severity,bool success, bool dbError = false,
            bool isBusy = false, bool selectPayer = true, bool selectAttendances = true)
        {
            var ids = _viewModel.Attendances.Where(a => a.IsSelected).Select(a => a.AttendanceId).ToArray();

            // Get payer summary
            if (isBusy || !selectPayer || !selectAttendances)
            {
                _mockPayerRepo.Verify(r => r.GetPayerSummaryNoOutstandingAsync(It.IsAny<Guid>()), Times.Never);
            }
            else
            {
                _mockPayerRepo.Verify(r => r.GetPayerSummaryNoOutstandingAsync(_viewModel.SelectedPayerId!.Value), Times.Once);
            }

            // Create invoice
            if (success || dbError)
            {
                _mockInvoiceRepo.Verify(r => r.CreateInvoiceAsync(_viewModel.SelectedPayerId!.Value, ids, null), 
                    Times.Once);
            }
            else
            {
                _mockInvoiceRepo.Verify(r => r.CreateInvoiceAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<Guid>>(),
                    It.IsAny<string?>()), Times.Never);
            }

            VerifyAction(infoMessage, severity, isOpen: true, payersCount: 1, count: 4,
                totalSelected: selectAttendances ? 65.5m : 0, total: 307.6m, isBusy: isBusy);
        }

        [TestMethod]
        public async Task GenerateInvoice_IsBusy()
        {
            _viewModel.IsBusy = true;
            ArrangeForGenerateInvoice();
            await ActForForGenerateInvoice();
            AssertForGenerateInvoice(infoMessage: "Can't generate invoice while busy.", severity: InfoBarType.Warning,
                isBusy: true, success: false);
        }

        [TestMethod]
        public async Task GenerateInvoice_NoPayerSelected()
        {
            ArrangeForGenerateInvoice(selectPayer: false);
            await ActForForGenerateInvoice();
            AssertForGenerateInvoice(infoMessage: "No payer selected.", severity: InfoBarType.Warning, success: false,
                selectPayer: false);
        }

        [TestMethod]
        public async Task GenerateInvoice_NoAttendanceSelected()
        {
            ArrangeForGenerateInvoice(selectAttendances: false);
            await ActForForGenerateInvoice();
            AssertForGenerateInvoice(infoMessage: "No attendances selected.", severity: InfoBarType.Warning, success: false,
                selectAttendances: false);
        }

        [TestMethod]
        public async Task GenerateInvoice_DBError()
        {
            ArrangeForGenerateInvoice();
            await ActForForGenerateInvoice(dbError: true);
            AssertForGenerateInvoice(infoMessage: "Constraint failed.", severity: InfoBarType.Error, success: false,
                dbError: true);
        }

        [TestMethod]
        public async Task GenerateInvoice()
        {
            ArrangeForGenerateInvoice();
            await ActForForGenerateInvoice();
            AssertForGenerateInvoice(infoMessage: "Invoice saved to: \\somepath\\invented\\Invoice_Name.pdf.", 
                severity: InfoBarType.Success, success: true);
        }

        // Load Attendances

        [TestMethod]
        public async Task LoadByInvoice_WhileBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.LoadByInvoiceAsync("Invoice_Name");

            VerifyAction("Can't load invoice while busy.", InfoBarType.Warning, isOpen: true,
                payersCount: 0, count: 0, isBusy: true, total: 0, totalSelected: 0);

            _mockInvoiceRepo.Verify(r => r.GetInvoiceAttendancesAsync(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task LoadByInvoice_InvalidName(string? invalidName)
        {
            await _viewModel.LoadByInvoiceAsync(invalidName);

            VerifyAction("Invoice name is required.", InfoBarType.Warning, isOpen: true,
                payersCount: 0, count: 0, isBusy: false, total: 0, totalSelected: 0);

            _mockInvoiceRepo.Verify(r => r.GetInvoiceAttendancesAsync(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task LoadByInvoice_PopulatesCollection()
        {
            var payer = new PayerOption(Guid.NewGuid(), "Payer");
            var attendanceId1 = Guid.NewGuid();
            var lessonId = Guid.NewGuid();
            var studentId1 = Guid.NewGuid();

            _viewModel.Payers.Add(payer);
            _viewModel.SelectedPayerId = payer.Id;

            var firstLoad = new List<InvoiceAttendanceSummary>();
            firstLoad.Add(new InvoiceAttendanceSummary(attendanceId1, lessonId, new DateOnly(2024, 1, 1), "Old Lesson", studentId1, "Student 1", 100));
            firstLoad.Add(new InvoiceAttendanceSummary(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 2), "Old Lesson", Guid.NewGuid(), "Student 2", 200));
            firstLoad.Add(new InvoiceAttendanceSummary(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 3), "Old Lesson", Guid.NewGuid(), "Student 3", 300));

            var secondLoad = new List<InvoiceAttendanceSummary>();
            secondLoad.Add(new InvoiceAttendanceSummary(attendanceId1, lessonId, new DateOnly(2025, 1, 1), "New Lesson", studentId1, "Student 1", 50));
            secondLoad.Add(new InvoiceAttendanceSummary(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2025, 1, 2), "New Lesson", Guid.NewGuid(), "Student 2", 500));
            secondLoad.Add(new InvoiceAttendanceSummary(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2025, 1, 3), "New Lesson", Guid.NewGuid(), "Student 3", 25));

            _mockInvoiceRepo.SetupSequence(r => r.GetInvoiceAttendancesAsync("Invoice_Name"))
                .ReturnsAsync(firstLoad)
                .ReturnsAsync(secondLoad);

            await _viewModel.LoadByInvoiceAsync("Invoice_Name"); // test that Attendances.Clear() is working
            VerifyAction(null, InfoBarType.Success, isOpen: false,
                payersCount: 1, count: 3, isBusy: false, total: 600, totalSelected: 0);

            await _viewModel.LoadByInvoiceAsync("Invoice_Name"); // If LoadAsync is called twice, you should not have duplicate items in your list
            VerifyAction(null, InfoBarType.Success, isOpen: false,
                payersCount: 1, count: 3, isBusy: false, total: 575, totalSelected: 0);

            _mockInvoiceRepo.Verify(r => r.GetInvoiceAttendancesAsync("Invoice_Name"), Times.Exactly(2));

            {
                var line = _viewModel.Attendances.First();
                Assert.AreEqual(attendanceId1, line.AttendanceId);
                Assert.AreEqual(lessonId, line.LessonId);
                Assert.AreEqual(new DateOnly(2025, 1, 1), line.Date);
                Assert.AreEqual("New Lesson", line.LessonName);
                Assert.AreEqual(studentId1, line.StudentId);
                Assert.AreEqual("Student 1", line.StudentName);
                Assert.AreEqual(50, line.Price);
                Assert.IsFalse(line.IsSelected);
            }

            Assert.IsFalse(_viewModel.SelectionState);

        }

        [TestMethod]
        public async Task LoadByInvoice_EmptyCollection()
        {
            var payer = new PayerOption(Guid.NewGuid(), "Payer");

            _viewModel.Payers.Add(payer);
            _viewModel.SelectedPayerId = payer.Id;

            _mockInvoiceRepo.SetupSequence(r => r.GetInvoiceAttendancesAsync("Invoice_Name"))
                .ReturnsAsync(new List<InvoiceAttendanceSummary>());

            await _viewModel.LoadByInvoiceAsync("Invoice_Name"); 
            VerifyAction(null, InfoBarType.Success, isOpen: false,
                payersCount: 1, count: 0, isBusy: false, total: 0, totalSelected: 0);

            _mockInvoiceRepo.Verify(r => r.GetInvoiceAttendancesAsync("Invoice_Name"), Times.Once);

            Assert.IsFalse(_viewModel.SelectionState);
        }
    }
}
