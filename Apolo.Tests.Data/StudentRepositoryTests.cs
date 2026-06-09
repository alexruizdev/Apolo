using Models;
using Repository;

namespace Apolo.Tests.Data
{
    [TestClass]
    public class StudentRepositoryTests : RepositoryTests
    {
        private StudentRepository _repository = null!;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _repository = new StudentRepository(_context);
        }

        // --- GetStudentsAsync Tests ---

        [TestMethod]
        public async Task GetStudentsAsync_IncludesPayerFullName()
        {
            // Act
            var results = (await _repository.GetSudentsAsync()).ToList();

            // Assert
            Assert.HasCount(11, results);
            Assert.AreEqual("Alice", results[0].FirstName);
            Assert.AreEqual("Doe", results[0].LastName);
            Assert.AreEqual(_data.Payers[0].Id, results[0].PayerId);
            Assert.AreEqual("John Doe", results[0].PayerName);
        }

        // --- AddAsync Tests ---

        [TestMethod]
        public async Task AddAsync_ShouldActuallyBeAdd_GivenCurrentImplementation()
        {

            var student = new Student { FirstName = "New", LastName = "Student", PayerId = _data.Payers[0].Id };

            await _repository.AddAsync(student);

            Assert.HasCount(12, _context.Students);
        }

        [TestMethod]
        public async Task AddAsync_WithoutValidPayer_ThrowsInvalidOperationException()
        {
            var orphanStudent = new Student { FirstName = "New", LastName = "Student", PayerId = Guid.NewGuid() };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _repository.AddAsync(orphanStudent);
            });

            StringAssert.Contains(ex.Message, "Payer with ID");
        }

        [TestMethod]
        public async Task AddAsync_ExistingStudent_UpdatesExistingRecord()
        {

            var student = _data.Students[0];
            student.PayerId = _data.Payers[1].Id; // Change Payer to test update behavior

            var ex = await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.AddAsync(student);
            });

            StringAssert.Contains(ex.Message, "This student already exists");

            var studentInDb = await _context.Students.FindAsync(student.Id);

            Assert.HasCount(11, _context.Students);
            Assert.IsNotNull(studentInDb);
        }

        // --- DeleteAsync Tests ---

        [TestMethod]
        public async Task DeleteAsync_NonExistentId_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.DeleteAsync(Guid.NewGuid());
            });

            // Verify the message contains the specific text we expect
            StringAssert.Contains(exception.Message, "Student not found");
        }

        [TestMethod]
        public async Task DeleteAsync()
        {
            var id = _data.Students[10].Id;
            // Act
            await _repository.DeleteAsync(id);

            // Assert
            Assert.HasCount(10, _context.Students);
        }

        // --- UpdateAsync Tests ---

        [TestMethod]
        public async Task UpdateAsync_ChangesPayerLink()
        {
            // Act: Move student to Payer Two
            await _repository.UpdateAsync(_data.Students[0].Id, _data.Payers[1].Id, "Kid", "NewLastName");

            // Assert
            var updated = _context.Students.Find(_data.Students[0].Id);
            Assert.AreEqual(_data.Payers[1].Id, updated!.PayerId);
            Assert.AreEqual("NewLastName", updated.LastName);
        }

        [TestMethod]
        public async Task UpdateAsync_NonExistentId_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await _repository.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), "John", "Doe");
            });
        }

        [TestMethod]
        public async Task UpdateAsync_InvalidPayerId_ThrowsDbUpdateException()
        {
            // Act & Assert
            // This usually throws a DbUpdateException because of the Foreign Key constraint
            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(async () =>
            {
                await _repository.UpdateAsync(_data.Students[0].Id, Guid.NewGuid(), "Kid", "NewName");
            });
        }


        // --- GetStudentOptionsAsync Tests ---

        [TestMethod]
        public async Task GetStudentOptionsAsync_ReturnsAllStudents_InAlphabeticalOrder()
        {
            var results = (await _repository.GetStudentOptionsAsync()).ToList();

            Assert.HasCount(11, results);

            Assert.AreEqual("Alice Doe", results[0].FullName);
            Assert.AreEqual("Bob Doe", results[1].FullName);
            Assert.AreEqual("Charlie Smith", results[2].FullName);
        }

    }
}
