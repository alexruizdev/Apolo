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
            Assert.HasCount(24, _context.BillingDocuments);
            Assert.HasCount(44, _context.Lessons);
        }

        [TestMethod]
        public async Task ClearArchiveAsync()
        {
            Assert.HasCount(5, _archiveContext.Payers);
            Assert.HasCount(8, _archiveContext.Students);
            Assert.HasCount(23, _archiveContext.Lessons);
            Assert.HasCount(10, _archiveContext.BillingDocuments);

            await _repository.ClearArchiveAsync();

            Assert.HasCount(0, _archiveContext.Payers);
            Assert.HasCount(0, _archiveContext.Students);
            Assert.HasCount(0, _archiveContext.Lessons);
            Assert.HasCount(0, _archiveContext.BillingDocuments);

            await _repository.ImportArchiveAsync(_data.ArchivePayers, _data.ArchiveStudents, _data.ArchiveLessons, _data.ArchiveBills);
            Assert.HasCount(5, _archiveContext.Payers);
            Assert.HasCount(8, _archiveContext.Students);
            Assert.HasCount(23, _archiveContext.Lessons);
            Assert.HasCount(10, _archiveContext.BillingDocuments);
        }


        [TestMethod]
        public async Task Export()
        {
            var data = await _repository.GetAllDataAsync();
            Assert.HasCount(6, data.Services);
            Assert.HasCount(10, data.Payers);
            Assert.HasCount(11, data.Students);
            Assert.HasCount(10, data.Specifications);
            Assert.HasCount(24, data.Invoices);
            Assert.HasCount(44, data.Lessons);
        }

        [TestMethod]
        public async Task ExportArchive()
        {
            var data = await _repository.ExportArchiveAsync();
            Assert.HasCount(5, data.Payers);
            Assert.HasCount(8, data.Students);
            Assert.HasCount(23, data.Lessons);
            Assert.HasCount(10, data.Invoices);
            Assert.HasCount(0, data.Services);
            Assert.HasCount(0, data.Specifications);
        }

        [TestMethod]
        public async Task GetPayersActivity()
        {
            var payers = await _repository.GetPayersWithActivityAsync();

            Assert.HasCount(10, payers);

            Assert.AreEqual("John Doe", payers[2].PayerName);
            Assert.IsNotNull(payers[2].LastLessonDate);
            Assert.AreEqual("John Doe - Last activity: 18/08/2025", payers[2].Display);

            Assert.IsNull(payers[0].LastLessonDate);
            Assert.Contains("No recorded activity", payers[0].Display);
        }

        [TestMethod]
        public async Task GetPayersFromArchive()
        {
            var payers = await _repository.GetPayersFromArchiveAsync();

            Assert.HasCount(5, payers);
            Assert.AreEqual("Aiden Clark", payers[0].FullName);
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
            Assert.HasCount(21, _context.BillingDocuments);
            Assert.HasCount(36, _context.Lessons);

            // Archive DB
            Assert.HasCount(6, _archiveContext.Payers);
            Assert.HasCount(10, _archiveContext.Students);
            Assert.HasCount(31, _archiveContext.Lessons);
            Assert.HasCount(13, _archiveContext.BillingDocuments);

            await _repository.RetrieveDataFromArchiveAsync([payerId]);

            // Main DB
            Assert.HasCount(6, _context.Services);
            Assert.HasCount(10, _context.Payers);
            Assert.HasCount(11, _context.Students);
            Assert.HasCount(8, _context.Specifications);
            Assert.HasCount(24, _context.BillingDocuments);
            Assert.HasCount(44, _context.Lessons);

            // Archive DB
            Assert.HasCount(5, _archiveContext.Payers);
            Assert.HasCount(8, _archiveContext.Students);
            Assert.HasCount(23, _archiveContext.Lessons);
            Assert.HasCount(10, _archiveContext.BillingDocuments);

            // I haved added this test because I found a condition that crashes when the database tracker kept the
            // entities and crashes the second time we want to archive. I have added a clear tracker before archiving and
            // retrieving
            await _repository.ArchiveOldDataAsync([payerId]);

            // Main DB
            Assert.HasCount(6, _context.Services);
            Assert.HasCount(9, _context.Payers);
            Assert.HasCount(9, _context.Students);
            Assert.HasCount(8, _context.Specifications);
            Assert.HasCount(21, _context.BillingDocuments);
            Assert.HasCount(36, _context.Lessons);

            // Archive DB
            Assert.HasCount(6, _archiveContext.Payers);
            Assert.HasCount(10, _archiveContext.Students);
            Assert.HasCount(31, _archiveContext.Lessons);
            Assert.HasCount(13, _archiveContext.BillingDocuments);
        }

        public TestContext TestContext { get; set; }
    }
}
