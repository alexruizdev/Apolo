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
            // Arrange
            var service = TestGenerator.CreateService1();
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student1 = TestGenerator.CreateStudent1(payer.Id);
            var student2 = TestGenerator.CreateStudent2(payer.Id);

            var spec1 = TestGenerator.CreateSpecification1(student1.Id, service.Id);
            var spec2 = TestGenerator.CreateSpecification2(student2.Id, service.Id);

            _context.Services.Add(service);
            _context.Payers.Add(payer);
            _context.Students.Add(student1);
            _context.Students.Add(student2);
            _context.Specifications.Add(spec1);
            _context.Specifications.Add(spec2);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetSpecificationsAsync()).ToList();

            // Assert
            Assert.HasCount(2, results);

            Assert.AreEqual(TestGenerator.SpecificationName2, results[0].SpecificationName);
            Assert.AreEqual(student2.FullName, results[0].StudentName);
            Assert.AreEqual(TestGenerator.ServiceName1, results[0].ServiceName);
            Assert.AreEqual(TestGenerator.ShortDuration, results[0].DurationMinutes);
            Assert.IsNull(results[0].Price);
            Assert.IsTrue(results[0].IsOnline);
            Assert.IsFalse(results[0].IsWeekenOrHoliday);

            Assert.AreEqual(TestGenerator.SpecificationName1, results[1].SpecificationName);
            Assert.AreEqual(student1.FullName, results[1].StudentName);
            Assert.AreEqual(TestGenerator.ServiceName1, results[1].ServiceName);
            Assert.AreEqual(TestGenerator.LongDuration, results[1].DurationMinutes);
            Assert.IsNotNull(results[1].Price);
            Assert.AreEqual((double)TestGenerator.ServicePrice2, results[1].Price!.Value);
            Assert.IsFalse(results[1].IsOnline);
            Assert.IsTrue(results[1].IsWeekenOrHoliday);
        }

        [TestMethod]
        public async Task GetSpecificationsForStudentAsync()
        {
            // Arrange
            var service = TestGenerator.CreateService1();
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student1 = TestGenerator.CreateStudent1(payer.Id);
            var student2 = TestGenerator.CreateStudent2(payer.Id);

            var spec1 = TestGenerator.CreateSpecification1(student1.Id, service.Id);
            var spec2 = TestGenerator.CreateSpecification2(student2.Id, service.Id);

            _context.Services.Add(service);
            _context.Payers.Add(payer);
            _context.Students.Add(student1);
            _context.Students.Add(student2);
            _context.Specifications.Add(spec1);
            _context.Specifications.Add(spec2);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetSpecificationsForStudentAsync([student1.Id])).ToList();

            // Assert
            Assert.HasCount(1, results);

            Assert.AreEqual($"{TestGenerator.SpecificationName1} - {student1.FullName}", results[0].Display);
            Assert.AreEqual(service.Id, results[0].ServiceId);
            Assert.AreEqual((double)TestGenerator.ServicePrice2, results[0].Price!.Value);
            Assert.AreEqual(TestGenerator.LongDuration, results[0].DurationMinutes);
            Assert.IsFalse(results[0].IsOnline);
            Assert.IsTrue(results[0].IsWeekend);
        }

        [TestMethod]
        public async Task UpdateAsync_ModifiesAllFieldsSuccessfully()
        {
            // Arrange
            var service1 = TestGenerator.CreateService1();
            var service2 = TestGenerator.CreateService2();
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student1 = TestGenerator.CreateStudent1(payer.Id);

            var spec = TestGenerator.CreateSpecification1(student1.Id, service1.Id);

            _context.Services.Add(service1);
            _context.Services.Add(service2);
            _context.Payers.Add(payer);
            _context.Students.Add(student1);
            _context.Specifications.Add(spec);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAsync(spec.Id, service2.Id, "New Name", 90, 75.5m, true, false);

            // Assert
            var updated = await _context.Specifications.FindAsync(spec.Id);
            Assert.AreEqual("New Name", updated!.Name);
            Assert.AreEqual(service2.Id, updated.ServiceId);
            Assert.AreEqual(90, updated.DurationMinutes);
            Assert.AreEqual(75.5m, updated.Price);
            Assert.IsTrue(updated.IsOnline);
            Assert.IsFalse(updated.IsWeekenOrHoliday);
        }

        [TestMethod]
        public async Task UpdateAsync_InvalidServiceId_ThrowsDbUpdateException()
        {
            // Arrange
            var service1 = TestGenerator.CreateService1();
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student1 = TestGenerator.CreateStudent1(payer.Id);

            var spec = TestGenerator.CreateSpecification1(student1.Id, service1.Id);

            _context.Services.Add(service1);
            _context.Payers.Add(payer);
            _context.Students.Add(student1);
            _context.Specifications.Add(spec);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(async () =>
            {
                await _repository.UpdateAsync(spec.Id, new Guid(), "New Name", 90, 75.5m, true, false);
            });
        }

        [TestMethod]
        public async Task UpdateAsync_NonExistentId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.UpdateAsync(new Guid(), new Guid(), "New Name", 90, 75.5m, true, false);
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
            // Arrange: Create a Payer first because Student depends on PayerId
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var service = TestGenerator.CreateService1();
            var spec = TestGenerator.CreateSpecification1(student.Id, service.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Services.Add(service);
            _context.Specifications.Add(spec);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(spec.Id);

            // Assert
            Assert.HasCount(0, _context.Specifications);
        }

        [TestMethod]
        public async Task AddSpecificationAsync()
        {
            // Arrange: Create a Payer first because Student depends on PayerId
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var service = TestGenerator.CreateService1();
            var spec = TestGenerator.CreateSpecification1(student.Id, service.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            // Act
            await _repository.AddSpecificationAsync(spec);

            // Assert
            Assert.HasCount(1, _context.Specifications);
        }
    }
}
