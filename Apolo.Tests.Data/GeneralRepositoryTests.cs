using Models;
using Repository;

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
            Assert.HasCount(6, _context.Services);
            Assert.HasCount(10, _context.Payers);
            Assert.HasCount(11, _context.Students);
            Assert.HasCount(10, _context.Specifications);
            Assert.HasCount(24, _context.BillingDocuments);
            Assert.HasCount(44, _context.Lessons);

            await _repository.ClearDatabaseAsync();

            Assert.HasCount(0, _context.Services);
            Assert.HasCount(0, _context.Payers);
            Assert.HasCount(0, _context.Students);
            Assert.HasCount(0, _context.Specifications);
            Assert.HasCount(0, _context.Lessons);
            Assert.HasCount(0, _context.BillingDocuments);

            await _repository.ImportAllDataAsync(_data.Services, _data.Payers, _data.Students, _data.Specifications,
                _data.Lessons, _data.Bills);
            Assert.HasCount(6, _context.Services);
            Assert.HasCount(10, _context.Payers);
            Assert.HasCount(11, _context.Students);
            Assert.HasCount(10, _context.Specifications);
            Assert.HasCount(40, _context.Lessons);
            Assert.HasCount(24, _context.BillingDocuments);
        }

        [TestMethod]
        public async Task ClearArchiveAsync()
        {
            _archiveContext.Payers.AddRange(_data.Payers);
            _archiveContext.Students.AddRange(_data.Students);
            _archiveContext.Lessons.AddRange(_data.Lessons);
            _archiveContext.BillingDocuments.AddRange(_data.Bills);
            await _archiveContext.SaveChangesAsync(TestContext.CancellationToken);

            Assert.HasCount(10, _archiveContext.Payers);
            Assert.HasCount(11, _archiveContext.Students);
            Assert.HasCount(40, _archiveContext.Lessons);
            Assert.HasCount(24, _archiveContext.BillingDocuments);

            await _repository.ClearArchiveAsync();

            Assert.HasCount(0, _archiveContext.Payers);
            Assert.HasCount(0, _archiveContext.Students);
            Assert.HasCount(0, _archiveContext.Lessons);
            Assert.HasCount(0, _archiveContext.BillingDocuments);

            await _repository.ImportArchiveAsync(_data.Payers, _data.Students, _data.Lessons, _data.Bills);
            Assert.HasCount(10, _archiveContext.Payers);
            Assert.HasCount(11, _archiveContext.Students);
            Assert.HasCount(40, _archiveContext.Lessons);
            Assert.HasCount(24, _archiveContext.BillingDocuments);
        }


        [TestMethod]
        public async Task Export()
        {
            _ = await _repository.GetAllDataAsync();
            Assert.HasCount(6, _context.Services);
            Assert.HasCount(10, _context.Payers);
            Assert.HasCount(11, _context.Students);
            Assert.HasCount(10, _context.Specifications);
            Assert.HasCount(40, _context.Lessons);
            Assert.HasCount(24, _context.BillingDocuments);
        }

        [TestMethod]
        public async Task ExportArchive()
        {
            _archiveContext.Payers.AddRange(_data.Payers);
            _archiveContext.Students.AddRange(_data.Students);
            _archiveContext.Lessons.AddRange(_data.Lessons);
            _archiveContext.BillingDocuments.AddRange(_data.Bills);
            await _archiveContext.SaveChangesAsync(TestContext.CancellationToken);

            _ = await _repository.ExportArchiveAsync();
            Assert.HasCount(10, _archiveContext.Payers);
            Assert.HasCount(11, _archiveContext.Students);
            Assert.HasCount(40, _archiveContext.Lessons);
            Assert.HasCount(24, _archiveContext.BillingDocuments);
        }

        [TestMethod]
        public async Task GetPayersActivity()
        {
            var payers = await _repository.GetPayersWithActivityAsync();

            Assert.HasCount(10, payers);
            Assert.AreEqual(_data.Payers[0].Id, payers[2].PayerId);
            Assert.AreEqual("John Doe", payers[2].PayerName);
            Assert.AreEqual(new DateOnly(2025, 8, 18), payers[2].LastLessonDate);
        }

        [TestMethod]
        public async Task GetPayersFromArchive()
        {
            _archiveContext.Payers.AddRange(_data.Payers);
            await _archiveContext.SaveChangesAsync(TestContext.CancellationToken);

            var payers = await _repository.GetPayersFromArchiveAsync();

            Assert.HasCount(10, payers);
            Assert.AreEqual("Carlos Gomez", payers[0].FullName);
        }

        [TestMethod]
        public async Task ArchiveBackForth()
        {
            var payerId = _data.Payers[0].Id;

            await _repository.ArchiveOldDataAsync([payerId]);

            // Main DB
            Assert.HasCount(6, _context.Services);
            Assert.HasCount(9, _context.Payers);
            Assert.HasCount(9, _context.Students);
            Assert.HasCount(8, _context.Specifications);
            Assert.HasCount(32, _context.Lessons);
            Assert.HasCount(21, _context.BillingDocuments);

            // Archive DB
            Assert.HasCount(1, _archiveContext.Payers);
            Assert.HasCount(2, _archiveContext.Students);
            Assert.HasCount(8, _archiveContext.Lessons);
            Assert.HasCount(3, _archiveContext.BillingDocuments);

            await _repository.RetrieveDataFromArchiveAsync([payerId]);

            // Main DB
            Assert.HasCount(6, _context.Services);
            Assert.HasCount(10, _context.Payers);
            Assert.HasCount(11, _context.Students);
            Assert.HasCount(8, _context.Specifications);
            Assert.HasCount(40, _context.Lessons);
            Assert.HasCount(24, _context.BillingDocuments);

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
            Assert.HasCount(6, _context.Services);
            Assert.HasCount(9, _context.Payers);
            Assert.HasCount(9, _context.Students);
            Assert.HasCount(8, _context.Specifications);
            Assert.HasCount(32, _context.Lessons);
            Assert.HasCount(21, _context.BillingDocuments);

            // Archive DB
            Assert.HasCount(1, _archiveContext.Payers);
            Assert.HasCount(2, _archiveContext.Students);
            Assert.HasCount(8, _archiveContext.Lessons);
            Assert.HasCount(3, _archiveContext.BillingDocuments);
        }

        public TestContext TestContext { get; set; }
    }
}
