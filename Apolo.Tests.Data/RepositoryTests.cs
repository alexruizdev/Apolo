using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Repository;
using Models;

namespace Apolo.Tests.Data
{
    [TestClass]
    public abstract class RepositoryTests
    {
        private SqliteConnection _connection = null!;
        private SqliteConnection _archiveConnection = null!;
        protected ApoloContext _context = null!;
        protected ApoloArchiveContext _archiveContext = null!;
        protected (List<Service> Services, List<Payer> Payers, List<Student> Students,
            List<Specification> Specifications, List<Lesson> Lessons, List<BillingDocument> Invoices) _data = Helper.GetDummyData();

        [TestInitialize]
        public virtual void Setup()
        {
            // 1. Create a connection to a RAM-based database
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // 2. Initialize your DB context/engine with this connection
            var options = new DbContextOptionsBuilder<ApoloContext>()
                .UseSqlite(_connection)
                .Options;

            _context = new ApoloContext(options);
            _context.Database.EnsureCreated(); // Creates tables based on your models

            // --- Archive Database Setup ---
            _archiveConnection = new SqliteConnection("DataSource=:memory:");
            _archiveConnection.Open();
            var archiveOptions = new DbContextOptionsBuilder<ApoloArchiveContext>()
                .UseSqlite(_archiveConnection)
                .Options;

            // Initialize using your marker class
            _archiveContext = new ApoloArchiveContext(archiveOptions);
            _archiveContext.Database.EnsureCreated();

            // Include dummy data to ensure tests are not relying on an empty database
            _context.Services.AddRange(_data.Services);
            _context.Payers.AddRange(_data.Payers);
            _context.Students.AddRange(_data.Students);
            _context.Specifications.AddRange(_data.Specifications);
            _context.BillingDocuments.AddRange(_data.Invoices);
            _context.Lessons.AddRange(_data.Lessons);
            _context.SaveChangesAsync().Wait();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // This closes the RAM connection and wipes the DB for the next test
            _context?.Dispose();
            _connection?.Close();
            _connection?.Dispose();

            _archiveContext?.Dispose();
            _archiveConnection?.Close();
            _archiveConnection?.Dispose();
        }
    }
}
