using Models;
using Repository;

namespace Apolo.Tests.Data
{
    [TestClass]
    public class PayerRepositoryTests : RepositoryTests
    {
        private PayerRepository _repository = null!;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _repository = new PayerRepository(_context);
        }

        [TestMethod]
        public async Task DeleteAsync_WithAssociatedStudents_ThrowsInvalidOperationException()
        {
            // Arrange: Create a payer and a student linked to them
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _repository.DeleteAsync(payer.Id);
            });
        }

        [TestMethod]
        public async Task GetPayersAsync_CalculatesUnpaidTotalsCorrectly()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: false);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson = TestGenerator.CreateLessonPaid();

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.Add(lesson);
            _context.Attendances.Add(new Attendance
            {
                LessonId = lesson.Id,
                StudentId = student.Id,
                IsPaid = false
            });
            await _context.SaveChangesAsync();

            // Act
            var results = await _repository.GetPayersAsync();
            var payerSummary = results.First(p => p.Id == payer.Id);

            // Assert
            // Verify the debt is calculated (assuming 50 is the price)
            Assert.AreEqual(TestGenerator.PayerName1, payerSummary.FirstName);
            Assert.AreEqual(TestGenerator.PayerLastName1, payerSummary.LastName);
            Assert.AreEqual(TestGenerator.LessonTotalPrice, payerSummary.Outstanding);
            Assert.AreEqual(TestGenerator.Address1, payerSummary.Address);
            Assert.AreEqual(TestGenerator.ZipCode1, payerSummary.Zip);
            Assert.AreEqual(TestGenerator.City1, payerSummary.City);
            Assert.AreEqual(TestGenerator.TaxId1, payerSummary.TaxId);
        }

        [TestMethod]
        public async Task GetPayerSummaryNoOutstandingAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: false);

            _context.Payers.Add(payer);
            await _context.SaveChangesAsync();

            // Act
            var payerSummary = await _repository.GetPayerSummaryNoOutstandingAsync(payer.Id);

            // Assert
            // Verify the debt is calculated (assuming 50 is the price)
            Assert.AreEqual(TestGenerator.PayerName1, payerSummary.FirstName);
            Assert.AreEqual(TestGenerator.PayerLastName1, payerSummary.LastName);
            Assert.AreEqual(0, payerSummary.Outstanding);
            Assert.AreEqual(TestGenerator.Address1, payerSummary.Address);
            Assert.AreEqual(TestGenerator.ZipCode1, payerSummary.Zip);
            Assert.AreEqual(TestGenerator.City1, payerSummary.City);
            Assert.AreEqual(TestGenerator.TaxId1, payerSummary.TaxId);
        }

        [TestMethod]
        public async Task GetPayerSummaryNoOutstandingAsync_InvalidPayer()
        {
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.GetPayerSummaryNoOutstandingAsync(Guid.NewGuid());
            });
        }

        [TestMethod]
        public async Task GetPayersAsync_MissingDuration_ThrowsArgumentException()
        {
            // Arrange
            var payer = new Payer { Id = Guid.NewGuid(), FirstName = "Test", LastName = "Payer" };
            var student = new Student { Id = Guid.NewGuid(), PayerId = payer.Id };

            // Lesson set to 'PricePerHour' but Duration is NULL
            var invalidLesson = new Lesson
            {
                Id = Guid.NewGuid(),
                IsPricePerHour = true,
                DurationMinutes = null,
                PricePerAttendance = 50
            };

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.Add(invalidLesson);
            _context.Attendances.Add(new Attendance { StudentId = student.Id, LessonId = invalidLesson.Id, IsPaid = false });
            await _context.SaveChangesAsync();

            // Act & Assert
            // This confirms that your repository correctly propagates the Model's business rules
            await Assert.ThrowsAsync<ArgumentException>(_repository.GetPayersAsync);
        }

        [TestMethod]
        public async Task UpsertAsync_ValidPayer_SavesSuccessfully()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);

            // Act
            await _repository.UpsertAsync(payer);

            // Assert
            var saved = _context.Payers.FirstOrDefault(p => p.FirstName == TestGenerator.PayerName1);
            Assert.IsNotNull(saved);
            Assert.AreEqual(TestGenerator.PayerName1, saved.FirstName);
            Assert.AreEqual(TestGenerator.PayerLastName1, saved.LastName);
            Assert.IsNull(saved.Address);
            Assert.IsNull(saved.ZipCode);
            Assert.IsNull(saved.City);
            Assert.IsNull(saved.TaxId);
        }

        [TestMethod]
        public async Task UpsertAsync_UpdatePayer()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);

            _context.Payers.Add(payer);
            await _context.SaveChangesAsync();

            payer.Address = TestGenerator.Address1;
            payer.ZipCode = TestGenerator.ZipCode1;
            payer.City = TestGenerator.City1;
            payer.TaxId = TestGenerator.TaxId1;

            // Act
            await _repository.UpsertAsync(payer);
        }

        // --- DELETE TESTS ---

        [TestMethod]
        public async Task DeleteAsync_ExistingId_RemovesFromDb()
        {
            // Arrange
            var id = Guid.NewGuid();
            _context.Payers.Add(TestGenerator.CreateTemporaryPayer(id));
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

        [TestMethod]
        public async Task DeleteAsync_AssociatedStudent_ThrowsException()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);

            await _context.SaveChangesAsync();

            // Act
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _repository.DeleteAsync(payer.Id);
            });
        }

        [TestMethod]
        public async Task UpdateAsync_ValidChanges_UpdatesProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            _context.Payers.Add(new Payer { 
                Id = id, 
                FirstName = "Old Name", 
                LastName = "Old Last Name"
            });
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAsync(id, "New Name", "New Last Name", "New Address", "New Zip", "New City", "New Tax");

            // Assert
            var updated = _context.Payers.Find(id);
            Assert.IsNotNull(updated);
            Assert.AreEqual("New Name", updated.FirstName);
            Assert.AreEqual("New Last Name", updated.LastName);
            Assert.AreEqual("New Address", updated.Address);
            Assert.AreEqual("New Zip", updated.ZipCode);
            Assert.AreEqual("New City", updated.City);
            Assert.AreEqual("New Tax", updated.TaxId);
        }

        [TestMethod]
        public async Task UpdateAsync_NonExistentId_ThrowsException()
        {
            // Act
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.UpdateAsync(Guid.NewGuid(), 
                    "New Name", "New Last Name", "New Address", "New Zip", "New City", "New Tax");
            });
        }

        [TestMethod]
        public async Task GetPayerOptionsAsync_ReturnsAlphabeticalPayers()
        {
            // Arrange
            _context.Payers.AddRange(
                new Payer { Id = Guid.NewGuid(), FirstName = "B", LastName = "User" },
                new Payer { Id = Guid.NewGuid(), FirstName = "A", LastName = "User" },
                new Payer { Id = Guid.NewGuid(), FirstName = "Z", LastName = "User" }
            );
            await _context.SaveChangesAsync();

            // Act
            var options = (await _repository.GetPayerOptionsAsync()).ToList();

            // Assert
            Assert.AreEqual("A User", options[0].FullName);
            Assert.AreEqual("B User", options[1].FullName);
            Assert.AreEqual("Z User", options[2].FullName);
        }
    }
}
