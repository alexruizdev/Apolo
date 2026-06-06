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
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson1 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 1", paid: true, months: 6);
            var lesson2 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 2", paid: true, months: 6);
            var lesson3 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 3", paid: false, months: 6);
            var lesson4 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 4", paid: false, months: 6);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.AddRange(lesson1, lesson2, lesson3, lesson4);
            await _context.SaveChangesAsync();

            var results = (await _repository.GetUnbilledLessonsAsync(payer.Id)).ToList();

            Assert.HasCount(2, results);
            Assert.IsTrue(results[0].Date <= results[1].Date);

            // Lesson 3
            int idx = results.FindIndex(r => r.Id == lesson3.Id);
            Assert.AreEqual(lesson3.Id, results[idx].Id);
            Assert.AreEqual(lesson3.Date, results[idx].Date);
            Assert.AreEqual(lesson3.Name, results[idx].Name);
            Assert.AreEqual(student.Id, results[idx].StudentId);
            Assert.AreEqual(student.FullName, results[idx].StudentName);
        }

        [TestMethod]
        public async Task CreateInvoiceAsyncEmptyLesson()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _repository.CreateBillAsync(payer.Id, [], DocumentType.Invoice);
            });

        }

        [TestMethod]
        public async Task CreateInvoiceAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lessons = new List<Lesson>();
            lessons.Add(TestGenerator.CreateRandomLesson(student.Id, "Lesson 1", paid: true, months: 6));
            lessons.Add(TestGenerator.CreateRandomLesson(student.Id, "Lesson 2", paid: true, months: 6));

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.AddRange(lessons);
            await _context.SaveChangesAsync();

            var lessonIds = lessons.Select(l => l.Id).ToList();

            var entity = await _repository.CreateBillAsync(payer.Id, lessonIds, DocumentType.Invoice);

            var result = await _context.BillingDocuments.ToListAsync();

            Assert.HasCount(1, result);
            Assert.HasCount(2, result[0].Lines);
            Assert.AreEqual($"{DateTime.Now:yyyy-MM}-E-0001", entity.DocumentNumber);
        }

        [TestMethod]
        public async Task CreateTicketAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lessons = new List<Lesson>();
            lessons.Add(TestGenerator.CreateRandomLesson(student.Id, "Lesson 1", paid: true, months: 6));
            lessons.Add(TestGenerator.CreateRandomLesson(student.Id, "Lesson 2", paid: true, months: 6));

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.AddRange(lessons);
            await _context.SaveChangesAsync();

            var lessonIds = lessons.Select(l => l.Id).ToList();

            var entity = await _repository.CreateBillAsync(payer.Id, lessonIds, DocumentType.Ticket);

            var result = await _context.BillingDocuments.ToListAsync();

            Assert.HasCount(1, result);
            Assert.HasCount(2, result[0].Lines);
            Assert.AreEqual($"TCK-{DateTime.Now:yyyy-MM}-0001", entity.DocumentNumber);
        }

        [TestMethod]
        public async Task DeleteInvoiceAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson1 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 1", paid: true, months: 6);
            var invoice = TestGenerator.CreateInvoice([lesson1], payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.AddRange(lesson1);
            _context.BillingDocuments.Add(invoice);
            await _context.SaveChangesAsync();

            Assert.HasCount(1, _context.BillingDocuments);

            await _repository.DeleteAsync(invoice.Id);

            Assert.HasCount(0, _context.BillingDocuments);
        }

        [TestMethod]
        public async Task DeleteInvoiceAsyncInvalidInvoice()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.DeleteAsync(Guid.NewGuid());
            });
        }
    }
}
