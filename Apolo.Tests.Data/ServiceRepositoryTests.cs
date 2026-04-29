using Models;
using Repository;

namespace Apolo.Tests.Data
{
    [TestClass]
    public class ServiceRepositoryTests : RepositoryTests
    {
        private ServiceRepository _repository = null!;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup(); // Initializes _context and _connection from base
            _repository = new ServiceRepository(_context);
        }

        // --- GET TESTS ---

        [TestMethod]
        public async Task GetServicesAsync_ReturnsAlphabeticalOrder()
        {
            // Arrange
            _context.Services.AddRange(
                TestGenerator.CreateService1(),
                TestGenerator.CreateService2()
            );
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetServicesAsync()).ToList();

            // Assert
            Assert.HasCount(2, results);
            Assert.AreEqual(TestGenerator.ServiceName1, results[1].Name);
            Assert.AreEqual(TestGenerator.ServiceName2, results[0].Name);
            Assert.AreEqual(TestGenerator.ServicePrice1, (decimal)results[1].Price);
            Assert.AreEqual(TestGenerator.ServicePrice2, (decimal)results[0].Price);
            Assert.IsTrue(results[1].IsPricePerHour);
            Assert.IsFalse(results[0].IsPricePerHour);
        }

        // --- ADD TESTS ---

        [TestMethod]
        public async Task AddAsync_ValidService_SavesSuccessfully()
        {
            // Arrange
            var service = TestGenerator.CreateService1();

            // Act
            await _repository.AddAsync(service);

            // Assert
            var saved = _context.Services.FirstOrDefault(s => s.Name == TestGenerator.ServiceName1);
            Assert.IsNotNull(saved);
            Assert.AreEqual(TestGenerator.ServiceName1, saved.Name);
            Assert.AreEqual(TestGenerator.ServicePrice1, (decimal)saved.Price);
            Assert.IsTrue(saved.IsPricePerHour);
        }

        [TestMethod]
        public async Task AddAsync_DuplicateName_ThrowsException()
        {
            // Arrange
            _context.Services.Add(TestGenerator.CreateService1());
            await _context.SaveChangesAsync();
            var duplicate = TestGenerator.CreateServiceDuplicate1();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.AddAsync(duplicate);
            });
        }

        // --- DELETE TESTS ---

        [TestMethod]
        public async Task DeleteAsync_ExistingId_RemovesFromDb()
        {
            // Arrange
            var id = Guid.NewGuid();
            _context.Services.Add(TestGenerator.CreateTemporaryService(id));
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(id);

            // Assert
            Assert.IsFalse(_context.Services.Any(s => s.Id == id));
        }

        [TestMethod]
        public async Task DeleteAsync_NonExistentId_ThrowsException()
        {
            // Act
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.DeleteAsync(Guid.NewGuid());
            });
        }

        // --- UPDATE TESTS ---

        [TestMethod]
        public async Task UpdateAsync_ValidChanges_UpdatesProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            _context.Services.Add(new Service { Id = id, Name = "Old Name", IsPricePerHour = false, Price = 10 });
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAsync(id, "New Name", true, 99.99m);

            // Assert
            var updated = _context.Services.Find(id);
            Assert.IsNotNull(updated);
            Assert.AreEqual("New Name", updated.Name);
            Assert.IsTrue(updated.IsPricePerHour);
            Assert.AreEqual(99.99m, updated.Price);
        }

        [TestMethod]
        public async Task UpdateAsync_ChangingToExistingName_ThrowsException()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            _context.Services.AddRange(
                new Service { Id = id1, Name = "Service A" },
                new Service { Id = id2, Name = "Service B" }
            );
            await _context.SaveChangesAsync();

            // Act: Try to rename Service B to "Service A"
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.UpdateAsync(id2, "service a", false, 10);
            });
        }
    }
}
