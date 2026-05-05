using Apolo.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models;
using Moq;
using Repository;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    [TestClass]
    public class StudentsViewModelTests
    {
        private Mock<IStudentRepository> _mockStudentRepo = null!;
        private Mock<IPayerRepository> _mockPayerRepo = null!;
        private StudentsViewModel _viewModel = null!;

        [TestInitialize]
        public void TestInit()
        {
            _mockStudentRepo = new Mock<IStudentRepository>();
            _mockPayerRepo = new Mock<IPayerRepository>();
            _viewModel = new StudentsViewModel(_mockStudentRepo.Object, _mockPayerRepo.Object);
        }

        void VerifyAction(string? message, InfoBarType severity, bool isOpen, int studentsCount, int payersCount, bool isBusy = false)
        {
            Assert.HasCount(studentsCount, _viewModel.Students);
            Assert.HasCount(payersCount, _viewModel.Payers);
            Assert.AreEqual(message, _viewModel.InfoMessage);
            Assert.AreEqual(isBusy, _viewModel.IsBusy);
            Assert.AreEqual(isOpen, _viewModel.OpenInfoBar);
            Assert.AreEqual(severity, _viewModel.InfoBarType);
        }

        // --- ValidateStudentInput Tests ---

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task ValidateStudentInput_WhenNameIsInvalid(string invalidName)
        {
            // Act
            var result = _viewModel.ValidateStudentInput(ref invalidName, ref invalidName);

            // Assert
            VerifyAction("Enter at least a first or last name.", 
                InfoBarType.Warning, isOpen: true, studentsCount: 0, payersCount: 0);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidateStudentInput()
        {
            var firstName = "Student ";
            var lastName = "X ";
            // Act
            var result = _viewModel.ValidateStudentInput(ref firstName, ref lastName);

            // Assert
            VerifyAction(null, InfoBarType.Success, isOpen: false, studentsCount: 0, payersCount: 0);
            Assert.IsTrue(result);
            Assert.AreEqual("Student", firstName);
            Assert.AreEqual("X", lastName);
        }

        // Get Student

        [TestMethod]
        public void GetStudent_InvalidId()
        {
            var exception = Assert.Throws<InvalidDataException>(() => _viewModel.GetStudent(Guid.NewGuid()));
            Assert.AreEqual("Student not loaded.", exception.Message);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        [TestMethod]
        public void GetStudent()
        {
            var student = new StudentSummary(Guid.NewGuid(), "First", "Last", Guid.NewGuid(), "Payer");
            _viewModel.Students.Add(student);
            var result = _viewModel.GetStudent(student.Id);
            Assert.AreEqual("First Last", student.FullName);
            Assert.AreEqual(0, result.index);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
        }

        // --- LoadAsync Tests ---

        [TestMethod]
        public async Task LoadAsync_WhenAlreadyBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.LoadAsync();

            VerifyAction("Can't load students while busy.", InfoBarType.Warning, isOpen: true,
                studentsCount: 0, payersCount: 0, isBusy: true);
            _mockPayerRepo.Verify(r => r.GetPayerOptionsAsync(), Times.Never);
            _mockStudentRepo.Verify(r => r.GetSudentsAsync(), Times.Never);
        }

        [TestMethod]
        public async Task LoadAsync_ValidInput_PopulatesStudentAndPayerCollection()
        {
            var firstPayerLoad = new List<PayerOption>();
            firstPayerLoad.Add(new PayerOption(Guid.NewGuid(), "Old Man"));
            firstPayerLoad.Add(new PayerOption(Guid.NewGuid(), "Old Kid"));
            var secondPayerLoad = new List<PayerOption>();
            secondPayerLoad.Add(new PayerOption(Guid.NewGuid(), "New Man"));
            secondPayerLoad.Add(new PayerOption(Guid.NewGuid(), "New Kid"));
            var firstStudentLoad = new List<StudentSummary>();
            firstStudentLoad.Add(new StudentSummary(Guid.NewGuid(), "Old", "Human", firstPayerLoad[0].Id, firstPayerLoad[0].FullName));
            firstStudentLoad.Add(new StudentSummary(Guid.NewGuid(), "Old", "Child", firstPayerLoad[1].Id, firstPayerLoad[1].FullName));
            var secondStudentLoad = new List<StudentSummary>();
            secondStudentLoad.Add(new StudentSummary(Guid.NewGuid(), "New", "Human", secondPayerLoad[0].Id, secondPayerLoad[0].FullName));
            secondStudentLoad.Add(new StudentSummary(Guid.NewGuid(), "New", "Child", secondPayerLoad[1].Id, secondPayerLoad[1].FullName));

            _mockPayerRepo.SetupSequence(r => r.GetPayerOptionsAsync())
             .ReturnsAsync(firstPayerLoad)
             .ReturnsAsync(secondPayerLoad);

            _mockStudentRepo.SetupSequence(r => r.GetSudentsAsync())
             .ReturnsAsync(firstStudentLoad)
             .ReturnsAsync(secondStudentLoad);

            await _viewModel.LoadAsync(); // test that Students.Clear() is working
            await _viewModel.LoadAsync(); // If LoadAsync is called twice, you should not have duplicate items in your list

            // 1. Verify repository was called with correct data
            _mockPayerRepo.Verify(r => r.GetPayerOptionsAsync(), Times.Exactly(2));
            _mockStudentRepo.Verify(r => r.GetSudentsAsync(), Times.Exactly(2));

            // 2. Verify the UI collection was updated correctly
            VerifyAction(null, InfoBarType.Success, isOpen: false, payersCount: 2, studentsCount: 2);
            var addedStudent = _viewModel.Students.First();
            var addedPayer = _viewModel.Payers.First();
            Assert.AreEqual("New Human", addedStudent.FullName);
            Assert.AreEqual("New Man", addedPayer.FullName);
        }

        [TestMethod]
        public async Task LoadAsync_EmptyRepository_ResultingCollectionIsEmpty()
        {
            _mockPayerRepo.SetupSequence(r => r.GetPayerOptionsAsync())
                .ReturnsAsync(new List<PayerOption>());
            _mockStudentRepo.SetupSequence(r => r.GetSudentsAsync())
                .ReturnsAsync(new List<StudentSummary>());


            await _viewModel.LoadAsync();

            _mockPayerRepo.Verify(r => r.GetPayerOptionsAsync(), Times.Once);
            _mockStudentRepo.Verify(r => r.GetSudentsAsync(), Times.Once);
            VerifyAction(null, InfoBarType.Success, isOpen: false, studentsCount: 0, payersCount: 0);
        }

        // --- AddStudentAsync Tests ---

        [TestMethod]
        public async Task AddStudentAsync_WhenAlreadyBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.AddStudentAsync("New", "Student", null);

            VerifyAction("Can't add student while busy.", InfoBarType.Warning, isOpen: true,
                payersCount: 0, studentsCount: 0, isBusy: true);
            _mockStudentRepo.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Never);
            _mockPayerRepo.Verify(r => r.AddAsync(It.IsAny<Payer>()), Times.Never);
        }

        [TestMethod]
        public async Task AddStudentAsync_InvalidInput()
        {
            await _viewModel.AddStudentAsync("", "", null);

            VerifyAction("Enter at least a first or last name.", InfoBarType.Warning, isOpen: true,
                payersCount: 0, studentsCount: 0);
            _mockStudentRepo.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Never);
            _mockPayerRepo.Verify(r => r.AddAsync(It.IsAny<Payer>()), Times.Never);
        }

        [TestMethod]
        public async Task AddStudentAsync_WhenPayerRepositoryThrows()
        {
            _mockPayerRepo.Setup(r => r.AddAsync(It.IsAny<Payer>()))
                     .ThrowsAsync(new DbUpdateException("Database connection lost."));

            await _viewModel.AddStudentAsync("New", "Student", null);

            VerifyAction("Database connection lost.", InfoBarType.Error, isOpen: true, payersCount: 0, studentsCount: 0);
            _mockPayerRepo.Verify(r => r.AddAsync(It.IsAny<Payer>()), Times.Once);
            _mockStudentRepo.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Never);
        }

        [TestMethod]
        public async Task AddStudentAsync_WhenStudentRepositoryThrows()
        {
            var existingPayerId = Guid.NewGuid();
            _viewModel.Payers.Add(new PayerOption(existingPayerId, "Existing Payer"));

            _mockStudentRepo.Setup(r => r.AddAsync(It.IsAny<Student>()))
                     .ThrowsAsync(new DbUpdateException("Database connection lost."));

            await _viewModel.AddStudentAsync("New", "Student", existingPayerId);

            VerifyAction("Database connection lost.", InfoBarType.Error, isOpen: true, payersCount: 1, studentsCount: 0);
            _mockPayerRepo.Verify(r => r.AddAsync(It.IsAny<Payer>()), Times.Never);
            _mockStudentRepo.Verify(r => r.AddAsync(It.IsAny<Student>()), Times.Once);
        }

        [TestMethod]
        public async Task AddStudentAsync_ValidInput_SavesEmptyPayerAndUpdatesCollection()
        {
            // Act
            await _viewModel.AddStudentAsync("New", "Student", null);

            // Assert
            // 1. Verify repository was called with correct data
            _mockPayerRepo.Verify(r => r.AddAsync(It.Is<Payer>(p =>
                p.FirstName == "New" &&
                p.LastName == "Student")), Times.Once);
            _mockStudentRepo.Verify(r => r.AddAsync(It.Is<Student>(s =>
                s.FirstName == "New" &&
                s.LastName == "Student")), Times.Once);

            // 2. Verify the UI collection was updated correctly
            VerifyAction("Student 'New Student' added successfully. Created payer with same name.", InfoBarType.Success,
                isOpen: true, payersCount: 1, studentsCount: 1);
            var addedSummary = _viewModel.Students.First();
            Assert.AreEqual("New", addedSummary.FirstName);
            Assert.AreEqual("Student", addedSummary.LastName);
            var addedOption = _viewModel.Payers.First();
            Assert.AreEqual("New Student", addedOption.FullName);
        }

        [TestMethod]
        public async Task AddStudentAsync_ValidInput_SavesExistingPayerAndUpdatesCollection()
        {
            var existingPayerId = Guid.NewGuid();
            _viewModel.Payers.Add(new PayerOption(existingPayerId, "Existing Payer"));
            // Act
            await _viewModel.AddStudentAsync("New", "Student", existingPayerId);

            // Assert
            // 1. Verify repository was called with correct data
            _mockPayerRepo.Verify(r => r.AddAsync(It.IsAny<Payer>()), Times.Never);
            _mockStudentRepo.Verify(r => r.AddAsync(It.Is<Student>(s =>
                s.FirstName == "New" &&
                s.LastName == "Student")), Times.Once);

            // 2. Verify the UI collection was updated correctly
            VerifyAction("Student 'New Student' added successfully.", InfoBarType.Success,
                isOpen: true, payersCount: 1, studentsCount: 1);
            var addedSummary = _viewModel.Students.First();
            Assert.AreEqual("New", addedSummary.FirstName);
            Assert.AreEqual("Student", addedSummary.LastName);
            var addedOption = _viewModel.Payers.First();
            Assert.AreEqual("Existing Payer", addedOption.FullName);
        }

        // --- DeleteStudentAsync Tests ---

        [TestMethod]
        public async Task DeleteStudentAsync_WhenAlreadyBusy_AbortsAndSetsMessage()
        {
            // Arrange
            _viewModel.IsBusy = true;
            var itemToDelete = new StudentSummary(Guid.NewGuid(), "Student", "Name", Guid.NewGuid(), "Payer Name");

            // Act
            await _viewModel.DeleteStudentAsync(itemToDelete.Id);

            // Assert
            VerifyAction("Can't delete student while busy.", InfoBarType.Warning, isOpen: true,
                studentsCount: 0, payersCount:0, isBusy: true);
            _mockStudentRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task DeleteStudentAsync_WhenDatabaseFails_CatchesExceptionAndLeavesListIntact()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var itemToDelete = new StudentSummary(targetId, "Student", "Name", Guid.NewGuid(), "Payer Name");

            // Add the item to the UI list so we can verify it DOESN'T get removed
            _viewModel.Students.Add(itemToDelete);

            // Force the mock database to fail (e.g., a Foreign Key constraint violation)
            _mockStudentRepo.Setup(r => r.DeleteAsync(targetId))
                     .ThrowsAsync(new DbUpdateException("Constraint failed"));

            // Act
            await _viewModel.DeleteStudentAsync(targetId);
            // Assert
            VerifyAction("Constraint failed", InfoBarType.Error, isOpen: true, payersCount: 0, studentsCount: 1);
            _mockStudentRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Once);
        }

        [TestMethod]
        public async Task DeleteStudentAsync_Valid_DeletesFromDatabaseAndRemovesFromUI()
        {
            // Arrange
            var payer = new PayerOption(Guid.NewGuid(), "Payer Name");
            var targetId = Guid.NewGuid();
            var itemToDelete = new StudentSummary(targetId, "Student", "Name", payer.Id, payer.FullName);
            var itemToKeep = new StudentSummary(Guid.NewGuid(), "Keep", "Student", payer.Id, payer.FullName);

            // Add both items to the UI list
            _viewModel.Students.Add(itemToDelete);
            _viewModel.Students.Add(itemToKeep);
            _viewModel.Payers.Add(payer);

            // Act
            await _viewModel.DeleteStudentAsync(targetId);

            // Assert
            // 1. Verify the repository was told to delete the correct ID
            _mockStudentRepo.Verify(r => r.DeleteAsync(targetId), Times.Once);

            // 2. Verify the UI list was updated correctly
            VerifyAction("Student 'Student Name' deleted successfully.", InfoBarType.Success, isOpen: true,
                studentsCount: 1, payersCount: 1);
            _mockStudentRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Once);
            Assert.AreEqual("Keep Student", _viewModel.Students[0].FullName); // Only the kept item remains
        }

        // --- UpdateStudentAsync Tests ---

        [TestMethod]
        public async Task UpdateStudentAsync_WhenAlreadyBusy_AbortsAndSetsMessage()
        {
            // Arrange
            _viewModel.IsBusy = true;

            // Act
            await _viewModel.UpdateStudentAsync(Guid.NewGuid(), "Student", "Name", Guid.NewGuid());

            // Assert
            VerifyAction("Can't update student while busy.", InfoBarType.Warning, isOpen: true,
                payersCount: 0, studentsCount: 0, isBusy: true);
            _mockStudentRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), 
                It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateStudentAsync_InvalidInput()
        {
            // Arrange
            _viewModel.IsBusy = false;

            // Act
            await _viewModel.UpdateStudentAsync(Guid.NewGuid(), "", "", Guid.NewGuid());

            // Assert
            VerifyAction("Enter at least a first or last name.", InfoBarType.Warning, isOpen: true,
                payersCount: 0, studentsCount: 0);
            _mockStudentRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateStudentAsync_WhenDatabaseFails_CatchesException()
        {
            // Arrange
            // Arrange
            var p1 = new PayerOption(Guid.NewGuid(), "Payer Name");
            var p2 = new PayerOption(Guid.NewGuid(), "Other Payer");
            var targetId = Guid.NewGuid();
            var originalItem = new StudentSummary(targetId, "Update", "Student", p1.Id, p1.FullName);
            var unrelatedItem = new StudentSummary(Guid.NewGuid(), "Keep", "Student", p1.Id, p1.FullName);

            _viewModel.Students.Add(originalItem);
            _viewModel.Students.Add(unrelatedItem);
            _viewModel.Payers.Add(p1);
            _viewModel.Payers.Add(p2);

            _mockStudentRepo.Setup(r => r.UpdateAsync(targetId, p2.Id, "Student", "Name"))
                     .ThrowsAsync(new DbUpdateException("Update failed due to lock."));

            // Act
            await _viewModel.UpdateStudentAsync(targetId, "Student", "Name", p2.Id);
            // Assert
            VerifyAction("Update failed due to lock.", InfoBarType.Error, isOpen: true, payersCount: 2, studentsCount: 2);
            _mockStudentRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateStudentAsync_HappyPath_UpdatesDatabaseAndReplacesUIItem()
        {
            // Arrange
            var p1 = new PayerOption(Guid.NewGuid(), "Payer Name");
            var p2 = new PayerOption(Guid.NewGuid(), "Other Payer");
            var targetId = Guid.NewGuid();
            var originalItem = new StudentSummary(targetId, "Update", "Student", p1.Id, p1.FullName);
            var unrelatedItem = new StudentSummary(Guid.NewGuid(), "Keep", "Student", p1.Id, p1.FullName);

            _viewModel.Students.Add(originalItem);
            _viewModel.Students.Add(unrelatedItem);
            _viewModel.Payers.Add(p1);
            _viewModel.Payers.Add(p2);

            // Act - Change Name and Price
            await _viewModel.UpdateStudentAsync(targetId, "New", "Name", p2.Id);

            // Assert
            // 1. Verify Repo call
            _mockStudentRepo.Verify(r => r.UpdateAsync(targetId, p2.Id, "New", "Name"), Times.Once);

            // 2. Verify UI Update
            VerifyAction("Student 'Update Student' updated successfully.", InfoBarType.Success, isOpen: true,
                studentsCount: 2, payersCount: 2);

            // The item at index 0 should be our updated record
            var updatedItem = _viewModel.Students[0];
            Assert.AreEqual("New Name", updatedItem.FullName);
            Assert.AreEqual(p2.FullName, updatedItem.PayerName);

            // Unrelated item should be untouched
            Assert.AreEqual("Keep Student", _viewModel.Students[1].FullName);
        }
    }
}
