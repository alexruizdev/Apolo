using Microsoft.EntityFrameworkCore;
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
            base.Setup(); 
            _repository = new ServiceRepository(_context);
        }

        // --- GET TESTS ---

        [TestMethod]
        public async Task GetServicesAsync_ReturnsAlphabeticalOrder()
        {
            // Act
            var results = (await _repository.GetServicesAsync()).ToList();

            // Assert
            Assert.HasCount(6, results);
            Assert.AreEqual("Exam Preparation Package", results[0].Name);
            Assert.AreEqual(300, results[0].Price);
            Assert.IsFalse(results[0].IsPricePerHour);
            Assert.AreEqual("Language Lessons", results[1].Name);
            Assert.AreEqual(35, results[1].Price);
            Assert.IsTrue(results[1].IsPricePerHour);
        }

        // --- ADD TESTS ---

        [TestMethod]
        public async Task AddAsync_ValidService_SavesSuccessfully()
        {
            // Arrange
            var service = new Service { Name = "Test Service", IsPricePerHour = true, Price = 45.50m };

            // Act
            await _repository.AddAsync(service);

            // Assert
            var saved = _context.Services.FirstOrDefault(s => s.Id == service.Id);
            Assert.IsNotNull(saved);
            Assert.AreEqual("Test Service", saved.Name);
            Assert.AreEqual(45.50m, saved.Price);
            Assert.IsTrue(saved.IsPricePerHour);
        }

        [TestMethod]
        public async Task AddAsync_DuplicateName_ThrowsException()
        {
            var service = new Service { Name = "Math Tutoring", IsPricePerHour = true, Price = 45.50m };

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(async () =>
            {
                await _repository.AddAsync(service);
            });
        }

        // --- DELETE TESTS ---

        [TestMethod]
        public async Task DeleteAsync_ExistingId_RemovesFromDb()
        {
            // Arrange
            var id = _data.Services[0].Id;

            // Act
            await _repository.DeleteAsync(id);

            // Assert
            Assert.IsFalse(_context.Services.Any(s => s.Id == id));
        }

        [TestMethod]
        public async Task DeleteAsync_NonExistentId_ThrowsException()
        {
            // Act
            await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            {
                await _repository.DeleteAsync(Guid.NewGuid());
            });
        }

        // --- UPDATE TESTS ---

        [TestMethod]
        public async Task UpdateAsync_ValidChanges_UpdatesProperties()
        {
            // Arrange
            var id = _data.Services[0].Id;

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
            var id = _data.Services[0].Id;
            var name = _data.Services[1].Name;

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.UpdateAsync(id, name, false, 10);
            });
        }
    }
}
