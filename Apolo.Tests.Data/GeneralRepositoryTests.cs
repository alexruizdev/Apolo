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
            _repository = new GeneralRepository(_context, _archiveContext);
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
            _context.BillingDocuments.AddRange(data.Invoices);
            await _context.SaveChangesAsync();

            Assert.HasCount(3, _context.Services);
            Assert.HasCount(3, _context.Payers);
            Assert.HasCount(3, _context.Students);
            Assert.HasCount(3, _context.Specifications);
            Assert.HasCount(4, _context.Lessons);
            Assert.HasCount(2, _context.BillingDocuments);

            await _repository.ClearDatabaseAsync();

            Assert.HasCount(0, _context.Services);
            Assert.HasCount(0, _context.Payers);
            Assert.HasCount(0, _context.Students);
            Assert.HasCount(0, _context.Specifications);
            Assert.HasCount(0, _context.Lessons);
            Assert.HasCount(0, _context.BillingDocuments);
        }

        [TestMethod]
        public async Task ClearArchiveAsync()
        {
            var data = Helper.GetData();
            _archiveContext.Payers.AddRange(data.Payers);
            _archiveContext.Students.AddRange(data.Students);
            _archiveContext.Lessons.AddRange(data.Lessons);
            _archiveContext.BillingDocuments.AddRange(data.Invoices);
            await _archiveContext.SaveChangesAsync();

            Assert.HasCount(3, _archiveContext.Payers);
            Assert.HasCount(3, _archiveContext.Students);
            Assert.HasCount(4, _archiveContext.Lessons);
            Assert.HasCount(2, _archiveContext.BillingDocuments);

            await _repository.ClearArchiveAsync();

            Assert.HasCount(0, _archiveContext.Payers);
            Assert.HasCount(0, _archiveContext.Students);
            Assert.HasCount(0, _archiveContext.Lessons);
            Assert.HasCount(0, _archiveContext.BillingDocuments);
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
            Assert.HasCount(4, _context.Lessons);
            Assert.HasCount(2, _context.BillingDocuments);
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
            _context.BillingDocuments.AddRange(data.Invoices);
            await _context.SaveChangesAsync();

            var newData = await _repository.GetAllDataAsync();
            Assert.HasCount(3, newData.Services);
            Assert.HasCount(3, newData.Payers);
            Assert.HasCount(3, newData.Students);
            Assert.HasCount(3, newData.Specifications);
            Assert.HasCount(4, newData.Lessons);
            Assert.HasCount(2, newData.Invoices);
        }

        [TestMethod]
        public async Task GetPayersActivity()
        {
            var data = Helper.GetData();
            _context.Services.AddRange(data.Services);
            _context.Payers.AddRange(data.Payers);
            _context.Students.AddRange(data.Students);
            _context.Specifications.AddRange(data.Specifications);
            _context.Lessons.AddRange(data.Lessons);
            _context.BillingDocuments.AddRange(data.Invoices);
            await _context.SaveChangesAsync();

            var payers = await _repository.GetPayersWithActivityAsync();

            Assert.HasCount(3, payers);
            Assert.AreEqual(data.Payers[0].Id, payers[1].PayerId);
            Assert.AreEqual("Payer 2", payers[0].PayerName);
            Assert.AreEqual(new DateOnly(2024, 1, 1), payers[0].LastLessonDate);
        }

        [TestMethod]
        public async Task GetPayersFromArchive()
        {
            var data = Helper.GetData();
            _archiveContext.Payers.AddRange(data.Payers);
            await _archiveContext.SaveChangesAsync();

            var payers = await _repository.GetPayersFromArchiveAsync();

            Assert.HasCount(3, payers);
            Assert.AreEqual("Payer 1", payers[0].FullName);
        }

        [TestMethod]
        public async Task ArchiveBackForth()
        {
            var data = Helper.GetData();
            _context.Services.AddRange(data.Services);
            _context.Payers.AddRange(data.Payers);
            _context.Students.AddRange(data.Students);
            _context.Specifications.AddRange(data.Specifications);
            _context.Lessons.AddRange(data.Lessons);
            _context.BillingDocuments.AddRange(data.Invoices);
            await _context.SaveChangesAsync();

            var payerId = data.Payers[0].Id;

            await _repository.ArchiveOldDataAsync([payerId]);

            // Main DB
            Assert.HasCount(3, _context.Services);
            Assert.HasCount(2, _context.Payers);
            Assert.HasCount(2, _context.Students);
            Assert.HasCount(2, _context.Specifications);
            Assert.HasCount(3, _context.Lessons);
            Assert.HasCount(1, _context.BillingDocuments);

            // Archive DB
            Assert.HasCount(1, _archiveContext.Payers);
            Assert.HasCount(1, _archiveContext.Students);
            Assert.HasCount(1, _archiveContext.Lessons);
            Assert.HasCount(1, _archiveContext.BillingDocuments);

            await _repository.RetrieveDataFromArchiveAsync([payerId]);

            // Main DB
            Assert.HasCount(3, _context.Services);
            Assert.HasCount(3, _context.Payers);
            Assert.HasCount(3, _context.Students);
            Assert.HasCount(2, _context.Specifications);
            Assert.HasCount(4, _context.Lessons);
            Assert.HasCount(2, _context.BillingDocuments);

            // Archive DB
            Assert.HasCount(0, _archiveContext.Payers);
            Assert.HasCount(0, _archiveContext.Students);
            Assert.HasCount(0, _archiveContext.Lessons);
            Assert.HasCount(0, _archiveContext.BillingDocuments);

            // I haved added this test because I found a condition that crashes when the database tracker kept the
            // entities and crashes the second time we want to archive. I have added a clear tracker before archiving and
            // retrieving
            await _repository.ArchiveOldDataAsync([payerId]);

            // Main DB
            Assert.HasCount(3, _context.Services);
            Assert.HasCount(2, _context.Payers);
            Assert.HasCount(2, _context.Students);
            Assert.HasCount(2, _context.Specifications);
            Assert.HasCount(3, _context.Lessons);
            Assert.HasCount(1, _context.BillingDocuments);

            // Archive DB
            Assert.HasCount(1, _archiveContext.Payers);
            Assert.HasCount(1, _archiveContext.Students);
            Assert.HasCount(1, _archiveContext.Lessons);
            Assert.HasCount(1, _archiveContext.BillingDocuments);
        }
    }
}
