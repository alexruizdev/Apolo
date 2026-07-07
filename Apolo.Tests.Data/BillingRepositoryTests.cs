using Microsoft.EntityFrameworkCore;
using Models;
using Repository;

namespace Apolo.Tests.Data
{
    [TestClass]
    public class BillingRepositoryTests : RepositoryTests
    {
        private BillingRepository _repository = null!;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _repository = new BillingRepository(_context);
        }

        [TestMethod]
        public async Task GetInvoiceLessonsAsyncByPayerId()
        {

            var results = (await _repository.GetUnbilledLessonsAsync(_data.Payers[2].Id)).ToList();

            Assert.HasCount(1, results);

            // Lesson 3
            int idx = results.FindIndex(r => r.Id == _data.Lessons[19].Id);
            Assert.AreEqual(_data.Lessons[19].Id, results[idx].Id);
            Assert.AreEqual(_data.Lessons[19].Date, results[idx].Date);
            Assert.AreEqual(_data.Lessons[19].Name, results[idx].Name);
            Assert.AreEqual(_data.Students[4].Id, results[idx].StudentId);
            Assert.AreEqual(_data.Students[4].FullName, results[idx].StudentName);
        }

        [TestMethod]
        public async Task CreateInvoiceAsyncEmptyLesson()
        {
            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _repository.CreateBillAsync(_data.Payers[0].Id, [], DocumentType.Invoice, new DateTime(2026, 01, 31));
            });

        }

        [TestMethod]
        public async Task CreateInvoiceAsync()
        {
            var lessonIds = _data.Lessons
                .Where(l => l.BillingDocumentId == null && l.StudentId == _data.Students[9].Id)
                .Select(l => l.Id).ToList();

            var entity = await _repository.CreateBillAsync(_data.Payers[7].Id, lessonIds, DocumentType.Invoice, 
                new DateTime(2026, 01, 31));

            var result = await _context.BillingDocuments.ToListAsync(TestContext.CancellationToken);

            Assert.HasCount(25, result);
            Assert.HasCount(4, result.Last().Lines);
            Assert.AreEqual("01-2026-0004", entity.DocumentNumber);
        }

        [TestMethod]
        public async Task EditInvoiceAsync()
        {
            var bill = _data.Bills[0];

            var entity = await _repository.EditAsync(bill.Id, DocumentType.Ticket, 20, new DateTime(2024, 08, 30));

            var result = await _context.BillingDocuments.ToListAsync(TestContext.CancellationToken);

            Assert.HasCount(15, result.Where(b => b.Type == DocumentType.Invoice));
            Assert.HasCount(9, result.Where(b => b.Type == DocumentType.Ticket));
            Assert.HasCount(2, result.First(b => b.Id == entity.Id).Lines);
        }

        [TestMethod]
        public async Task CreateTicketAsync()
        {
            var lessonIds = _data.Lessons
                .Where(l => l.BillingDocumentId == null && l.StudentId == _data.Students[9].Id)
                .Select(l => l.Id).ToList();

            var entity = await _repository.CreateBillAsync(_data.Payers[2].Id, lessonIds, DocumentType.Ticket, 
                new DateTime(2026, 01, 31));

            var result = await _context.BillingDocuments.ToListAsync(TestContext.CancellationToken);

            Assert.HasCount(25, result);
            Assert.HasCount(4, result.Last().Lines);
            Assert.AreEqual("TCK-01-2026-0003", entity.DocumentNumber);
        }

        [TestMethod]
        public async Task DeleteInvoiceAsync()
        {

            Assert.HasCount(24, _context.BillingDocuments);

            await _repository.DeleteAsync(_data.Bills[0].Id);

            Assert.HasCount(23, _context.BillingDocuments);
        }

        [TestMethod]
        public async Task DeleteInvoiceAsyncInvalidInvoice()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _repository.DeleteAsync(Guid.NewGuid());
            });
        }

        public TestContext TestContext { get; set; }
    }
}
