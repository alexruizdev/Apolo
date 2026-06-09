using Models;
using Repository;

namespace Apolo.Tests.Data
{
    [TestClass]
    public class SpecificationRepositoryTests : RepositoryTests
    {
        private SpecificationRepository _repository = null!;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _repository = new SpecificationRepository(_context);
        }

        [TestMethod]
        public async Task GetSpecificationsAsync_IncludesJoinedNames()
        {
            // Act
            var results = (await _repository.GetSpecificationsAsync()).ToList();

            // Assert
            Assert.HasCount(10, results);

            Assert.AreEqual("Spanish Practice - Sofia", results[0].SpecificationName);
            Assert.AreEqual("Sofia Lopez", results[0].StudentName);
            Assert.AreEqual("Math Tutoring", results[0].ServiceName);
            Assert.AreEqual(45, results[0].DurationMinutes);
            Assert.AreEqual(35, results[0].Price);
            Assert.IsTrue(results[0].IsOnline);
            Assert.IsFalse(results[0].IsWeekendOrHoliday);
            Assert.AreEqual(10, results[0].UsageCount);
        }

        [TestMethod]
        public async Task GetSpecificationsForStudentAsync()
        {
            // Act
            var results = (await _repository.GetSpecificationsForStudentAsync([_data.Students[9].Id])).ToList();

            // Assert
            Assert.HasCount(1, results);

            Assert.AreEqual($"French Lessons - Chloe - Chloe Dubois", results[0].Display);
            Assert.AreEqual(_data.Services[0].Id, results[0].ServiceId);
            Assert.AreEqual(55, results[0].Price!.Value);
            Assert.AreEqual(60, results[0].DurationMinutes);
            Assert.IsTrue(results[0].IsOnline);
            Assert.IsFalse(results[0].IsWeekend);
        }

        [TestMethod]
        public async Task UpdateAsync_ModifiesAllFieldsSuccessfully()
        {

            // Act
            await _repository.UpdateAsync(_data.Specifications[0].Id, _data.Services[1].Id, "New Name", 90, 75.5m, true, false);

            // Assert
            var updated = await _context.Specifications.FindAsync(_data.Specifications[0].Id);
            Assert.AreEqual("New Name", updated!.Name);
            Assert.AreEqual(_data.Services[1].Id, updated.ServiceId);
            Assert.AreEqual(90, updated.DurationMinutes);
            Assert.AreEqual(75.5m, updated.Price);
            Assert.IsTrue(updated.IsOnline);
            Assert.IsFalse(updated.IsWeekendOrHoliday);
            Assert.AreEqual(5, updated.UsageCount);
        }

        [TestMethod]
        public async Task UpdateAsync_InvalidServiceId_ThrowsDbUpdateException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(async () =>
            {
                await _repository.UpdateAsync(_data.Specifications[0].Id, Guid.NewGuid(), "New Name", 90, 75.5m, true, false);
            });
        }

        [TestMethod]
        public async Task UpdateAsync_NonExistentId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), "New Name", 90, 75.5m, true, false);
            });

            // Verify the message contains the specific text we expect
            StringAssert.Contains(exception.Message, "Specification not found");
        }

        [TestMethod]
        public async Task DeleteAsync_NonExistentId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.DeleteAsync(Guid.NewGuid());
            });

            // Verify the message contains the specific text we expect
            StringAssert.Contains(exception.Message, "Specification not found");
        }

        [TestMethod]
        public async Task DeleteAsync()
        {
            // Act
            await _repository.DeleteAsync(_data.Specifications[0].Id);

            // Assert
            Assert.HasCount(9, _context.Specifications);
        }

        [TestMethod]
        public async Task AddSpecificationAsync()
        {
            var studentId = _data.Students[1].Id;
            var serviceId = _data.Services[1].Id;
            var spec = new Specification { Name = "Test", StudentId = studentId, ServiceId = serviceId, DurationMinutes = 60, Price = null, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 5 };
            // Act
            await _repository.AddSpecificationAsync(spec);

            // Assert
            Assert.HasCount(11, _context.Specifications);
        }

        [TestMethod]
        public async Task IncreaseUsage_InvalidId()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.IncrementUsageAsync(Guid.NewGuid());
            });
        }

        [TestMethod]
        public async Task IncreaseUsage()
        {
            // Act
            await _repository.IncrementUsageAsync(_data.Specifications[0].Id);

            // Assert
            var databaseSpec = await _context.Specifications.FindAsync(_data.Specifications[0].Id);
            Assert.IsNotNull(databaseSpec);
            Assert.AreEqual(6, databaseSpec.UsageCount);
        }
    }
}
