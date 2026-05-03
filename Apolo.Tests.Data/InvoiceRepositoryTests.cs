using Microsoft.EntityFrameworkCore;
using Models;
using Repository;
using System.Collections.Frozen;

namespace Apolo.Tests.Data
{
    [TestClass]
    public class InvoiceRepositoryTests : RepositoryTests
    {
        private InvoiceRepository _repository = null!;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _repository = new InvoiceRepository(_context);
        }

        [TestMethod]
        public async Task GetInvoiceAttendancesAsyncByPayerId()
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

            var results = (await _repository.GetInvoiceAttendancesAsync(payer.Id)).ToList();

            Assert.HasCount(2, results);
            Assert.IsTrue(results[0].Date <= results[1].Date);

            // Lesson 3
            int idx = results.FindIndex(r => r.LessonId == lesson3.Id);
            Assert.AreEqual(lesson3.Id, results[idx].LessonId);
            Assert.AreEqual(lesson3.Date, results[idx].Date);
            Assert.AreEqual(lesson3.Name, results[idx].LessonName);
            Assert.AreEqual(student.Id, results[idx].StudentId);
            Assert.AreEqual(student.FullName, results[idx].StudentName);
            Assert.AreEqual(lesson3.GetFinalPricePerStudent(), results[idx].Price);
        }

        [TestMethod]
        public async Task UpdateAttendancesAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lessons = new List<Lesson> { TestGenerator.CreateRandomLesson(student.Id, "Lesson 1", paid: true, months: 6),
                            TestGenerator.CreateRandomLesson(student.Id, "Lesson 2", paid: true, months: 6),
                            TestGenerator.CreateRandomLesson(student.Id, "Lesson 3", paid: false, months: 6),
                            TestGenerator.CreateRandomLesson(student.Id, "Lesson 4", paid: false, months: 6) };
            var invoice = TestGenerator.CreateInvoice(lessons, payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.AddRange(lessons);
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            var attendanceIds = lessons.SelectMany(l => l.Attendaces.Select(a => a.Id)).ToList();

            await _repository.UpdateAttendancesAsync(attendanceIds);


            Assert.AreEqual(4, _context.Attendances.Count(a => a.IsPaid));

        }

        [TestMethod]
        public async Task UpdateAttendancesAsyncEmpty()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var invoice = TestGenerator.CreateInvoice([], payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            await _repository.UpdateAttendancesAsync([]);


            Assert.AreEqual(0, _context.Attendances.Count(a => a.IsPaid));

        }

        [TestMethod]
        public async Task GetInvoiceAttendancesAsyncByInvoiceName()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson1 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 1", paid: true, months: 6);
            var lesson2 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 2", paid: true, months: 6);
            var lesson3 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 3", paid: false, months: 6);
            var lesson4 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 4", paid: false, months: 6);
            var invoice = TestGenerator.CreateInvoice([lesson1, lesson2, lesson3, lesson4], payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.AddRange(lesson1, lesson2, lesson3, lesson4);
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            var results = (await _repository.GetInvoiceAttendancesAsync($"{TestGenerator.InvoiceName1} ")).ToList();

            Assert.HasCount(4, results);
            Assert.IsTrue(results[0].Date <= results[1].Date);

        }

        [TestMethod]
        public async Task AddAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson1 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 1", paid: true, months: 6);
            var lesson2 = TestGenerator.CreateRandomLesson(student.Id, "Lesson 2", paid: true, months: 6);
            var invoice = TestGenerator.CreateInvoice([lesson1, lesson2], payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.AddRange(lesson1, lesson2);
            await _context.SaveChangesAsync();

            await _repository.AddAsync(invoice);

            var dbInvoice = await _context.Invoices.Include(i => i.Lines).FirstAsync(i => i.Id == invoice.Id);

            Assert.HasCount(2, dbInvoice.Lines);
            Assert.AreEqual(TestGenerator.InvoiceName1, dbInvoice.Name);
            Assert.AreEqual(invoice.CreatedUTC, dbInvoice.CreatedUTC);
            Assert.AreEqual(payer.Id, dbInvoice.PayerId);
            Assert.AreEqual(invoice.Id, dbInvoice.Lines.ElementAt(0).InvoiceId);
            Assert.AreEqual(lesson1.Attendaces.First().Id, dbInvoice.Lines.ElementAt(0).AttendanceId);
        }

        [TestMethod]
        public async Task CreateInvoiceAsyncEmptyAttendance()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await _repository.CreateInvoiceAsync(payer.Id, [], null);
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

            var attendanceIds = lessons.SelectMany(l => l.Attendaces.Select(a => a.Id)).ToList();

            var (invoiceId, invoiceName) = await _repository.CreateInvoiceAsync(payer.Id, attendanceIds, null);

            var result = await _context.Invoices.ToListAsync();

            Assert.HasCount(1, result);
            Assert.HasCount(2, result[0].Lines);
            Assert.AreEqual(1, invoiceId);
            Assert.AreEqual($"{DateTime.Now:yyyy-MM}-E-1", invoiceName);
        }

        [TestMethod]
        public async Task CreateInvoiceAsyncRequestedName()
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

            var attendanceIds = lessons.SelectMany(l => l.Attendaces.Select(a => a.Id)).ToList();

            var (invoiceId, invoiceName) = await _repository.CreateInvoiceAsync(payer.Id, attendanceIds, "TestName");

            var result = await _context.Invoices.ToListAsync();

            Assert.HasCount(1, result);
            Assert.HasCount(2, result[0].Lines);
            Assert.AreEqual(1, invoiceId);
            Assert.AreEqual("TestName", invoiceName);
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
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            Assert.HasCount(1, _context.Invoices);

            await _repository.DeleteInvoiceAsync(invoice.Id);

            Assert.HasCount(0, _context.Invoices);
        }

        [TestMethod]
        public async Task DeleteInvoiceAsyncInvalidInvoice()
        {
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.DeleteInvoiceAsync(99);
            });
        }


        [TestMethod]
        public async Task GetInvoicesAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson1 = TestGenerator.CreateLesson(student.Id, false);
            var invoice = TestGenerator.CreateInvoice([lesson1], payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.AddRange(lesson1);
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            var results = await _repository.GetInvoicesAsync();

            Assert.HasCount(1, results);
        }
    }
}
