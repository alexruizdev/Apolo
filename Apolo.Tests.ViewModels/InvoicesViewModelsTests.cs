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
        private Mock<IBillingRepository> _mockInvoiceRepo = null!;
        private Mock<IPayerRepository> _mockPayerRepo = null!;
        private Mock<IUserProfileService> _mockUserProfileService = null!;
        private Mock<PDF.IWriter> _mockPDFWriter = null!;
        private BillingViewModel _viewModel = null!;

        [TestInitialize]
        public void TestInit()
        {
            _mockInvoiceRepo = new Mock<IBillingRepository>();
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

            _viewModel = new BillingViewModel(_mockInvoiceRepo.Object, _mockPayerRepo.Object, _mockUserProfileService.Object,
                _mockPDFWriter.Object);
        }

        void VerifyAction(string? message, InfoBarType severity, bool isOpen, int payersCount, int count, decimal totalSelected, decimal total, bool isBusy = false)
        {
            Assert.HasCount(payersCount, _viewModel.Payers);
            Assert.HasCount(count, _viewModel.Lessons);
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

        // Load Lessons

        [TestMethod]
        public async Task LoadLessons_WhileBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.LoadLessonsAsync();

            VerifyAction("Can't load lessons while busy.", InfoBarType.Warning, isOpen: true,
                payersCount: 0, count: 0, isBusy: true, total: 0, totalSelected: 0);

            _mockInvoiceRepo.Verify(r => r.GetUnbilledLessonsAsync(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task LoadLessons_PopulatesCollection()
        {
            var payer = new PayerOption(Guid.NewGuid(), "Payer");
            var lessonId = Guid.NewGuid();
            var studentId1 = Guid.NewGuid();

            _viewModel.Payers.Add(payer);
            _viewModel.SelectedPayerId = payer.Id;

            var firstLoad = new List<LessonLine>();
            firstLoad.Add(new LessonLine(lessonId, studentId1, new DateOnly(2024, 1, 1), "Old Lesson", "Student 1", 100, false));
            firstLoad.Add(new LessonLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 2), "Old Lesson", "Student 2", 200, false));
            firstLoad.Add(new LessonLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 3), "Old Lesson", "Student 3", 300, false));

            var secondLoad = new List<LessonLine>();
            secondLoad.Add(new LessonLine(lessonId, studentId1, new DateOnly(2025, 1, 1), "New Lesson", "Student 1", 50, false));
            secondLoad.Add(new LessonLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2025, 1, 2), "New Lesson", "Student 2", 500, false));
            secondLoad.Add(new LessonLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2025, 1, 3), "New Lesson", "Student 3", 25, false));

            _mockInvoiceRepo.SetupSequence(r => r.GetUnbilledLessonsAsync(payer.Id))
                .ReturnsAsync(firstLoad)
                .ReturnsAsync(secondLoad);

            await _viewModel.LoadLessonsAsync(); // test that Lessons.Clear() is working
            VerifyAction("Loaded 3 lessons unbilled and unpaid", InfoBarType.Success, isOpen: true,
                payersCount: 1, count: 3, isBusy: false, total: 600, totalSelected: 0);

            await _viewModel.LoadLessonsAsync(); // If LoadAsync is called twice, you should not have duplicate items in your list
            VerifyAction("Loaded 3 lessons unbilled and unpaid", InfoBarType.Success, isOpen: true,
                payersCount: 1, count: 3, isBusy: false, total: 575, totalSelected: 0);

            _mockInvoiceRepo.Verify(r => r.GetUnbilledLessonsAsync(payer.Id), Times.Exactly(2));

            {
                var line = _viewModel.Lessons.First();
                Assert.AreEqual(lessonId, line.Data.Id);
                Assert.AreEqual(new DateOnly(2025, 1, 1), line.Data.Date);
                Assert.AreEqual("New Lesson", line.Data.Name);
                Assert.AreEqual(studentId1, line.Data.StudentId);
                Assert.AreEqual("Student 1", line.Data.StudentName);
                Assert.AreEqual(50, line.Data.FinalPrice);
                Assert.IsFalse(line.IsSelected);
            }

            Assert.IsFalse(_viewModel.SelectionState);

            // Let's play with selection
            _viewModel.Lessons[0].IsSelected = true;
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
        public async Task LoadLessons_EmptyCollection()
        {
            var payer = new PayerOption(Guid.NewGuid(), "Payer");

            _viewModel.Payers.Add(payer);
            _viewModel.SelectedPayerId = payer.Id;

            _mockInvoiceRepo.SetupSequence(r => r.GetUnbilledLessonsAsync(payer.Id))
                .ReturnsAsync(new List<LessonLine>());

            await _viewModel.LoadLessonsAsync(); // test that Lessons.Clear() is working
            VerifyAction("Loaded 0 lessons unbilled and unpaid", InfoBarType.Success, isOpen: true,
                payersCount: 1, count: 0, isBusy: false, total: 0, totalSelected: 0);

            _mockInvoiceRepo.Verify(r => r.GetUnbilledLessonsAsync(payer.Id), Times.Once);

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
            _viewModel.Lessons.Add(new InvoiceLine(new LessonLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", "Student 1", 50, false)));
            _viewModel.Lessons.Add(new InvoiceLine(new LessonLine(ids[0], Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", "Student 1", 30, false))
            { IsSelected = someSelected });
            _viewModel.Lessons.Add(new InvoiceLine(new LessonLine(ids[1], Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", "Student 1", 35.5m, false))
            { IsSelected = someSelected });
            _viewModel.Lessons.Add(new InvoiceLine(new LessonLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", "Student 1", 192.1m, false)));
            return ids;
        }

        private async Task ActForMarkAsPaid(List<Guid> ids, bool dbError = false)
        {
            if (dbError)
            {
                _mockInvoiceRepo.Setup(r => r.UpdateLessonsAsync(ids, isPaid: true))
                    .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }

            await _viewModel.MarkSelectedPaymentAsync(markAsPaid: true);
        }

        private void AssertForMarkAsPaid(List<Guid> ids, bool success,
            string? infoMessage, InfoBarType severity, bool isBusy = false, bool dbError = false)
        {
            if (success || dbError)
            {
                _mockInvoiceRepo.Verify(r => r.UpdateLessonsAsync(ids, isPaid: true), Times.Once);
            }
            else
            {
                _mockInvoiceRepo.Verify(r => r.UpdateLessonsAsync(It.IsAny<IReadOnlyList<Guid>>(), It.IsAny<bool>()), Times.Never);
            }

            if (dbError || success)
            {
                VerifyAction(infoMessage, severity, isOpen: true, payersCount: 0, count: 4,
                totalSelected: 65.5m, total: 307.6m, isBusy: isBusy);
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
            AssertForMarkAsPaid(ids, success: false, infoMessage: "Can't mark lessons as paid while busy.",
                severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task MarkAsPaid_NoSelection()
        {
            var ids = ArrangeForMarkAsPaid();
            await ActForMarkAsPaid(ids);
            AssertForMarkAsPaid(ids, success: false, infoMessage: "Please, select first a lesson to mark them as paid.",
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

        private List<Guid> ArrangeForGenerateInvoice(bool selectPayer = true, bool selectLessons = true)
        {
            var payer = new PayerOption(Guid.NewGuid(), "Payer");

            _viewModel.Payers.Add(payer);

            if (selectPayer)
                _viewModel.SelectedPayerId = payer.Id;

            _viewModel.Lessons.Add(new InvoiceLine(new LessonLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", "Student 1", 50, false)));
            _viewModel.Lessons.Add(new InvoiceLine(new LessonLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", "Student 1", 30, false))
            { IsSelected = selectLessons });
            _viewModel.Lessons.Add(new InvoiceLine(new LessonLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", "Student 1", 35.5m, false))
            { IsSelected = selectLessons });
            _viewModel.Lessons.Add(new InvoiceLine(new LessonLine(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2024, 1, 1),
                "Old Lesson", "Student 1", 192.1m, false)));

            return _viewModel.Lessons.Where(l => l.IsSelected).Select(l => l.Data.Id).ToList();
        }

        private async Task ActForForGenerateInvoice(List<Guid> ids, bool dbError = false)
        {
            var payerId = _viewModel.SelectedPayerId ?? Guid.NewGuid();

            _mockPayerRepo.Setup(r => r.GetPayerSummaryNoOutstandingAsync(payerId))
                .ReturnsAsync(new PayerSummary(payerId, "Payer", "1", 0, null, null, null, null));
            
            if (dbError)
            {
                _mockInvoiceRepo.Setup(r => r.CreateBillAsync(payerId, ids, DocumentType.Invoice))
                    .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            else 
            {
                _mockInvoiceRepo.Setup(r => r.CreateBillAsync(payerId, ids, DocumentType.Invoice))
                    .ReturnsAsync((new BillingDocument(DateTime.UtcNow){ SequenceNumber = 1, Type = DocumentType.Invoice}));
            }

            await _viewModel.GenerateInvoice("\\somepath\\invented", isInvoice: true);
        }

        private void AssertForGenerateInvoice(List<Guid> ids, string? infoMessage, InfoBarType severity,bool success, bool dbError = false,
            bool isBusy = false, bool selectPayer = true, bool selectLessons = true)
        {
            // Get payer summary
            if (isBusy || !selectPayer || !selectLessons)
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
                _mockInvoiceRepo.Verify(r => r.CreateBillAsync(_viewModel.SelectedPayerId!.Value, ids, DocumentType.Invoice), 
                    Times.Once);
            }
            else
            {
                _mockInvoiceRepo.Verify(r => r.CreateBillAsync(It.IsAny<Guid>(), It.IsAny<List<Guid>>(),
                    It.IsAny<DocumentType>()), Times.Never);
            }

            decimal selected = 0;
            if (selectLessons && !success)
                selected = 65.5m;
            decimal total = success ? 60.5m : 307.6m;

            VerifyAction(infoMessage, severity, isOpen: true, payersCount: 1, count: success ? 2 : 4,
                totalSelected: selected, total: total, isBusy: isBusy);
        }

        [TestMethod]
        public async Task GenerateInvoice_IsBusy()
        {
            _viewModel.IsBusy = true;
            var ids = ArrangeForGenerateInvoice();
            await ActForForGenerateInvoice(ids);
            AssertForGenerateInvoice(ids, infoMessage: "Can't generate invoice while busy.", severity: InfoBarType.Warning,
                isBusy: true, success: false);
        }

        [TestMethod]
        public async Task GenerateInvoice_NoPayerSelected()
        {
            var ids = ArrangeForGenerateInvoice(selectPayer: false);
            await ActForForGenerateInvoice(ids);
            AssertForGenerateInvoice(ids, infoMessage: "No payer selected.", severity: InfoBarType.Warning, success: false,
                selectPayer: false);
        }

        [TestMethod]
        public async Task GenerateInvoice_NoLessonSelected()
        {
            var ids = ArrangeForGenerateInvoice(selectLessons: false);
            await ActForForGenerateInvoice(ids);
            AssertForGenerateInvoice(ids, infoMessage: "No lessons selected.", severity: InfoBarType.Warning, success: false,
                selectLessons: false);
        }

        [TestMethod]
        public async Task GenerateInvoice_DBError()
        {
            var ids = ArrangeForGenerateInvoice();
            await ActForForGenerateInvoice(ids, dbError: true);
            AssertForGenerateInvoice(ids, infoMessage: "Constraint failed.", severity: InfoBarType.Error, success: false,
                dbError: true);
        }

        [TestMethod]
        public async Task GenerateInvoice()
        {
            var ids = ArrangeForGenerateInvoice();
            await ActForForGenerateInvoice(ids);
            AssertForGenerateInvoice(ids, infoMessage: "Invoice saved to: \\somepath\\invented\\Invoice_Name.pdf.", 
                severity: InfoBarType.Success, success: true);
        }
    }
}
