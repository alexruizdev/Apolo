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

        [TestMethod]
        public async Task GetStudentsAsync_IncludesPayerFullName()
        {
            // Arrange: Create a Payer first because Student depends on PayerId
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetSudentsAsync()).ToList();

            // Assert
            Assert.HasCount(1, results);
            Assert.AreEqual(TestGenerator.StudentName1, results[0].FirstName);
            Assert.AreEqual(TestGenerator.StudentLastName1, results[0].LastName);
            Assert.AreEqual(payer.Id, results[0].PayerId);
            Assert.AreEqual(payer.FullName, results[0].PayerName);
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

        [TestMethod]
        public async Task UpdateAsync_ChangesPayerLink()
        {
            // Arrange
            var p1 = new Payer { Id = Guid.NewGuid(), FirstName = "Payer", LastName = "One" };
            var p2 = new Payer { Id = Guid.NewGuid(), FirstName = "Payer", LastName = "Two" };
            var student = new Student { Id = Guid.NewGuid(), PayerId = p1.Id, FirstName = "Kid" };

            _context.Payers.AddRange(p1, p2);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act: Move student to Payer Two
            await _repository.UpdateAsync(student.Id, p2.Id, "Kid", "NewLastName");

            // Assert
            var updated = _context.Students.Find(student.Id);
            Assert.AreEqual(p2.Id, updated!.PayerId);
            Assert.AreEqual("NewLastName", updated.LastName);
        }

        [TestMethod]
        public async Task UpsertAsync_ShouldActuallyBeAdd_GivenCurrentImplementation()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            _context.Payers.Add(payer);
            await _context.SaveChangesAsync();

            var student = TestGenerator.CreateStudent1(payer.Id);

            // Act
            await _repository.UpsertAsync(student);

            // Assert
            Assert.HasCount(1, _context.Students);
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
            StringAssert.Contains(exception.Message, "Student not found");
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
        public async Task DeleteAsync()
        {
            // Arrange: Create a Payer first because Student depends on PayerId
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(student.Id);

            // Assert
            Assert.HasCount(0, _context.Students);
        }

        [TestMethod]
        public async Task UpdateAsync_InvalidPayerId_ThrowsDbUpdateException()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act & Assert
            // This usually throws a DbUpdateException because of the Foreign Key constraint
            await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(async () =>
            {
                await _repository.UpdateAsync(student.Id, Guid.NewGuid(), "Kid", "NewName");
            });
        }

        [TestMethod]
        public async Task UpsertAsync_WithoutValidPayer_ThrowsInvalidOperationException()
        {
            // Arrange
            var orphanStudent = new Student
            {
                Id = Guid.NewGuid(),
                PayerId = Guid.NewGuid(), // ID that doesn't exist in DB
                FirstName = "Orphan",
                LastName = "Student"
            };

            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _repository.UpsertAsync(orphanStudent);
            });

            StringAssert.Contains(ex.Message, "Payer with ID");
        }

        [TestMethod]
        public async Task UpsertAsync_ExistingStudent_UpdatesExistingRecord()
        {
            // Arrange: 1. Create a Payer and a Student
            var payer = new Payer { Id = Guid.NewGuid(), FirstName = "Original", LastName = "Payer" };
            var studentId = Guid.NewGuid();
            var originalStudent = new Student
            {
                Id = studentId,
                PayerId = payer.Id,
                FirstName = "OldName",
                LastName = "OldLastName"
            };

            _context.Payers.Add(payer);
            _context.Students.Add(originalStudent);
            await _context.SaveChangesAsync();

            // 2. Prepare a student object with the SAME ID but NEW data
            var updatedData = new Student
            {
                Id = studentId,
                PayerId = payer.Id,
                FirstName = "NewName",
                LastName = "NewLastName"
            };

            // Act
            await _repository.UpsertAsync(updatedData);

            // Assert: Check that the database still has only 1 student and the names are updated
            var studentInDb = await _context.Students.FindAsync(studentId);

            Assert.HasCount(1, _context.Students, "Database should still only have one student record.");
            Assert.IsNotNull(studentInDb);
            Assert.AreEqual("NewName", studentInDb.FirstName);
            Assert.AreEqual("NewLastName", studentInDb.LastName);
        }

        [TestMethod]
        public async Task UpsertAsync_ChangePayer_UpdatesForeignKey()
        {
            // Arrange
            var p1 = new Payer { Id = Guid.NewGuid(), FirstName = "Payer 1" };
            var p2 = new Payer { Id = Guid.NewGuid(), FirstName = "Payer 2" };
            var studentId = Guid.NewGuid();
            var student = new Student { Id = studentId, PayerId = p1.Id, FirstName = "Kid" };

            _context.Payers.AddRange(p1, p2);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act: Create an object with the same Student ID but the second Payer ID
            var modifiedStudent = new Student { Id = studentId, PayerId = p2.Id, FirstName = "Kid" };
            await _repository.UpsertAsync(modifiedStudent);

            // Assert
            var result = await _context.Students.FindAsync(studentId);
            Assert.AreEqual(p2.Id, result!.PayerId, "Student should have been reassigned to Payer 2.");
        }
    }
}
