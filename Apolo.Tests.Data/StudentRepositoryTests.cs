using Microsoft.EntityFrameworkCore;
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

        // --- AddAsync Tests ---

        [TestMethod]
        public async Task AddAsync_ShouldActuallyBeAdd_GivenCurrentImplementation()
        {
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            _context.Payers.Add(payer);
            await _context.SaveChangesAsync();

            var student = TestGenerator.CreateStudent1(payer.Id);

            await _repository.AddAsync(student);

            Assert.HasCount(1, _context.Students);
        }

        [TestMethod]
        public async Task AddAsync_WithoutValidPayer_ThrowsInvalidOperationException()
        {
            var orphanStudent = new Student
            {
                Id = Guid.NewGuid(),
                PayerId = Guid.NewGuid(), // ID that doesn't exist in DB
                FirstName = "Orphan",
                LastName = "Student"
            };

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _repository.AddAsync(orphanStudent);
            });

            StringAssert.Contains(ex.Message, "Payer with ID");
        }

        [TestMethod]
        public async Task AddAsync_ExistingStudent_UpdatesExistingRecord()
        {
            var p1 = new Payer { Id = Guid.NewGuid(), FirstName = "Original", LastName = "Payer" };
            var p2 = new Payer { Id = Guid.NewGuid(), FirstName = "Original", LastName = "Payer" };
            var student = new Student
            {
                Id = Guid.NewGuid(),
                PayerId = p1.Id,
                FirstName = "OldName",
                LastName = "OldLastName"
            };

            _context.Payers.AddRange(p1, p2);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            student.PayerId = p2.Id;

            var ex = await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.AddAsync(student);
            });

            StringAssert.Contains(ex.Message, "This student already exists");

            var studentInDb = await _context.Students.FindAsync(student.Id);

            Assert.HasCount(1, _context.Students, "Database should still only have one student record.");
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

        // --- UpdateAsync Tests ---

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


        // --- GetStudentOptionsAsync Tests ---

        [TestMethod]
        public async Task GetStudentOptionsAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Act
            var results = await _repository.GetStudentOptionsAsync();

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count());
        }

        [TestMethod]
        public async Task GetStudentOptionsAsync_ReturnsAllStudents_InAlphabeticalOrder()
        {
            // Arrange: Create a Payer (required for students) and three students out of order
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            _context.Payers.Add(payer);

            _context.Students.AddRange(
                new Student { Id = Guid.NewGuid(), PayerId = payer.Id, FirstName = "Zoe", LastName = "Adams" },
                new Student { Id = Guid.NewGuid(), PayerId = payer.Id, FirstName = "Aaron", LastName = "Zebra" },
                new Student { Id = Guid.NewGuid(), PayerId = payer.Id, FirstName = "Ben", LastName = "Adams" }
            );
            await _context.SaveChangesAsync();

            var results = (await _repository.GetStudentOptionsAsync()).ToList();

            Assert.HasCount(3, results);

            Assert.AreEqual("Aaron Zebra", results[0].FullName);
            Assert.AreEqual("Ben Adams", results[1].FullName);
            Assert.AreEqual("Zoe Adams", results[2].FullName);
        }

    }
}
