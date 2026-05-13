using Repository;
using Models;

namespace Apolo.Tests.Data
{
    [TestClass]
    public class GeneralRepositoryTests : RepositoryTests
    {
        private GeneralRepository _repository = null!;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _repository = new GeneralRepository(_context);
        }

        // Clear database 
        [TestMethod]
        public async Task ClearDatabasAsync()
        {
            var data = Helper.GetData();
            _context.Services.AddRange(data.Services);
            _context.Payers.AddRange(data.Payers);
            _context.Students.AddRange(data.Students);
            _context.Specifications.AddRange(data.Specifications);
            _context.Lessons.AddRange(data.Lessons);
            _context.Invoices.AddRange(data.Invoices);
            await _context.SaveChangesAsync();

            Assert.HasCount(3, _context.Services);
            Assert.HasCount(3, _context.Payers);
            Assert.HasCount(3, _context.Students);
            Assert.HasCount(3, _context.Specifications);
            Assert.HasCount(3, _context.Lessons);
            Assert.HasCount(3, _context.Invoices);
            Assert.HasCount(3, _context.Attendances);
            Assert.HasCount(3, _context.InvoiceAttendances);

            await _repository.ClearDatabaseAsync();

            Assert.HasCount(0, _context.Services);
            Assert.HasCount(0, _context.Payers);
            Assert.HasCount(0, _context.Students);
            Assert.HasCount(0, _context.Specifications);
            Assert.HasCount(0, _context.Lessons);
            Assert.HasCount(0, _context.Invoices);
            Assert.HasCount(0, _context.Attendances);
            Assert.HasCount(0, _context.InvoiceAttendances);
        }

        [TestMethod]
        public async Task Import()
        {
            var data = Helper.GetData();
            await _repository.ImportAllDataAsync(data.Services, data.Payers, data.Students, data.Specifications, 
                data.Lessons, data.Invoices);
            Assert.HasCount(3, _context.Services);
            Assert.HasCount(3, _context.Payers);
            Assert.HasCount(3, _context.Students);
            Assert.HasCount(3, _context.Specifications);
            Assert.HasCount(3, _context.Lessons);
            Assert.HasCount(3, _context.Invoices);
            Assert.HasCount(3, _context.Attendances);
            Assert.HasCount(3, _context.InvoiceAttendances);
        }

        [TestMethod]
        public async Task Export()
        {
            var data = Helper.GetData();
            _context.Services.AddRange(data.Services);
            _context.Payers.AddRange(data.Payers);
            _context.Students.AddRange(data.Students);
            _context.Specifications.AddRange(data.Specifications);
            _context.Lessons.AddRange(data.Lessons);
            _context.Invoices.AddRange(data.Invoices);
            await _context.SaveChangesAsync();

            var newData = await _repository.GetAllDataAsync();
            Assert.HasCount(3, newData.Services);
            Assert.HasCount(3, newData.Payers);
            Assert.HasCount(3, newData.Students);
            Assert.HasCount(3, newData.Specifications);
            Assert.HasCount(3, newData.Lessons);
            Assert.HasCount(1, newData.Lessons.ElementAt(0).Attendances);
            Assert.HasCount(1, newData.Lessons.ElementAt(1).Attendances);
            Assert.HasCount(1, newData.Lessons.ElementAt(2).Attendances);
            Assert.HasCount(3, newData.Invoices);
            Assert.HasCount(1, newData.Invoices.ElementAt(0).Lines);
            Assert.HasCount(1, newData.Invoices.ElementAt(1).Lines);
            Assert.HasCount(1, newData.Invoices.ElementAt(2).Lines);
        }
    }
}
