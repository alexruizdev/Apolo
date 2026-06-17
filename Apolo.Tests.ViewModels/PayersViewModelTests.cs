using Apolo.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models;
using Moq;
using Repository;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    [TestClass]
    public class PayersViewModelTests
    {
        private Mock<IPayerRepository> _mockRepo = null!;
        private PayersViewModel _viewModel = null!;

        [TestInitialize]
        public void TestInit()
        {
            _mockRepo = new Mock<IPayerRepository>();
            _viewModel = new PayersViewModel(_mockRepo.Object);
        }

        void VerifyAction(string? message, InfoBarType severity, bool isOpen, int count, bool isBusy = false)
        {
            Assert.HasCount(count, _viewModel.Payers);
            Assert.AreEqual(message, _viewModel.InfoMessage);
            Assert.AreEqual(isBusy, _viewModel.IsBusy);
            Assert.AreEqual(isOpen, _viewModel.OpenInfoBar);
            Assert.AreEqual(severity, _viewModel.InfoBarType);
        }

        // --- ValidatePayerInput Tests ---

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task ValidatePayerInput_WhenNameIsInvalid(string invalidName)
        {
            var address = "address ";
            var zipCode = " zip ";
            var city = " city ";
            var taxId = " taxId ";

            // Act
            var result =_viewModel.ValidatePayerInput(ref invalidName, ref invalidName, ref address, ref zipCode, ref city, ref taxId);

            // Assert
            VerifyAction("Enter at least a first or last name.", InfoBarType.Warning, isOpen: true, count: 0);
            Assert.AreEqual("address", address);
            Assert.AreEqual("zip", zipCode);
            Assert.AreEqual("city", city);
            Assert.AreEqual("taxId", taxId);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ValidatePayerInput()
        {
            var firstName = "Payer ";
            var lastName = "X ";
            var address = "address ";
            var zipCode = " zip ";
            var city = " city ";
            var taxId = " taxId ";

            // Act
            var result = _viewModel.ValidatePayerInput(ref firstName, ref lastName, ref address, ref zipCode, ref city, ref taxId);

            // Assert
            VerifyAction(null, InfoBarType.Success, isOpen: false, count: 0);
            Assert.AreEqual("address", address);
            Assert.AreEqual("zip", zipCode);
            Assert.AreEqual("city", city);
            Assert.AreEqual("taxId", taxId);
            Assert.AreEqual("Payer", firstName);
            Assert.AreEqual("X", lastName);
            Assert.IsTrue(result);  
        }

        // Get Payer

        [TestMethod]
        public void GetPayer_InvalidId()
        {
            var exception = Assert.Throws<InvalidDataException>(() => _viewModel.GetPayer(Guid.NewGuid()));
            Assert.AreEqual("Payer not loaded.", exception.Message);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        [TestMethod]
        public void GetPayer()
        {
            var payer = new PayerSummary(Guid.NewGuid(), "First", "Last", 0, null, null, null, null);
            _viewModel.Payers.Add(payer);
            var result = _viewModel.GetPayer(payer.Id);
            Assert.AreEqual("First Last", payer.Name);
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

            VerifyAction("Can't load payers while busy.", InfoBarType.Warning, isOpen: true, count: 0, isBusy: true);
            _mockRepo.Verify(r => r.GetPayersAsync(), Times.Never);
        }

        [TestMethod]
        public async Task LoadAsync_ValidInput_PopulatesPayersCollection()
        {
            var firstLoad = new List<PayerSummary>
            {
                new(Guid.NewGuid(), "Old", "Man", 0, null, null, null, null),
                new(Guid.NewGuid(), "Old", "Kid", 0, null, null, null, null)
            };
            var secondLoad = new List<PayerSummary>
            {
                new(Guid.NewGuid(), "New", "Man", 0, null, null, null, null),
                new(Guid.NewGuid(), "New", "Kid", 0, null, null, null, null)
            };

            _mockRepo.SetupSequence(r => r.GetPayersAsync())
             .ReturnsAsync(firstLoad)
             .ReturnsAsync(secondLoad);

            // Act
            await _viewModel.LoadAsync(); // test that Payers.Clear() is working
            await _viewModel.LoadAsync(); // If LoadAsync is called twice, you should not have duplicate items in your list

            // Assert
            // 1. Verify repository was called with correct data
            _mockRepo.Verify(r => r.GetPayersAsync(), Times.Exactly(2));

            // 2. Verify the UI collection was updated correctly
            VerifyAction("2 loaded", InfoBarType.Success, isOpen: true, count: 2);
            var addedSummary = _viewModel.Payers.First();
            Assert.AreEqual("New", addedSummary.FirstName);
            Assert.AreEqual("Man", addedSummary.LastName);
        }

        [TestMethod]
        public async Task LoadAsync_EmptyRepository_ResultingCollectionIsEmpty()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetPayersAsync())
                .ReturnsAsync([]);

            // Act
            await _viewModel.LoadAsync();

            // Assert
            _mockRepo.Verify(r => r.GetPayersAsync(), Times.Once);
            VerifyAction("0 loaded", InfoBarType.Success, isOpen: true, count: 0);
        }

        // --- AddPayerAsync Tests ---

        [TestMethod]
        public async Task AddPayerAsync_WhenAlreadyBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.AddPayerAsync("New", "Payer", "", "", "", "");

            VerifyAction("Can't add payer while busy.", InfoBarType.Warning, isOpen: true, count: 0, isBusy: true);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Payer>()), Times.Never);
        }

        [TestMethod]
        public async Task AddPayerAsync_InvalidInput()
        {
            // Act
            await _viewModel.AddPayerAsync("", "", "", "", "", "");

            // Assert
            VerifyAction("Enter at least a first or last name.", InfoBarType.Warning, isOpen: true, count: 0);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Payer>()), Times.Never);
        }

        [TestMethod]
        public async Task AddPayerAsync_WhenRepositoryThrows()
        {
            // Arrange
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<Payer>()))
                     .ThrowsAsync(new DbUpdateException("Database connection lost."));

            // Act
            await _viewModel.AddPayerAsync("New", "Payer", "", "", "", "");

            // Assert
            VerifyAction("Database connection lost.", InfoBarType.Error, isOpen: true, count: 0);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Payer>()), Times.Once);
        }

        [TestMethod]
        public async Task AddPayerAsync_ValidInput_SavesToRepoAndUpdatesCollection()
        {
            // Act
            await _viewModel.AddPayerAsync("New", "Payer", "Address", "ZipCode", "City", "TaxId");

            // Assert
            // 1. Verify repository was called with correct data
            _mockRepo.Verify(r => r.AddAsync(It.Is<Payer>(p =>
                p.FirstName == "New" &&
                p.LastName == "Payer" &&
                p.Address == "Address" &&
                p.ZipCode == "ZipCode" &&
                p.City == "City" &&
                p.TaxId == "TaxId")), Times.Once);

            // 2. Verify the UI collection was updated correctly
            VerifyAction("Payer 'New Payer' added successfully.", InfoBarType.Success, isOpen: true, count: 1);
            var addedSummary = _viewModel.Payers.First();
            Assert.AreEqual("New", addedSummary.FirstName);
            Assert.AreEqual("Payer", addedSummary.LastName);
            Assert.AreEqual("Address", addedSummary.Address);
            Assert.AreEqual("ZipCode", addedSummary.Zip);
            Assert.AreEqual("City", addedSummary.City);
            Assert.AreEqual("TaxId", addedSummary.TaxId);
        }

        // --- DeletePayerAsync Tests ---

        [TestMethod]
        public async Task DeletePayerAsync_WhenAlreadyBusy_AbortsAndSetsMessage()
        {
            // Arrange
            _viewModel.IsBusy = true;
            var itemToDelete = new PayerSummary(Guid.NewGuid(), "Old", "Man", 0, null, null, null, null);

            // Act
            await _viewModel.DeletePayerAsync(itemToDelete.Id);

            // Assert
            VerifyAction("Can't delete payer while busy.", InfoBarType.Warning, isOpen: true, count: 0, isBusy: true);
            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task DeletePayerAsync_WhenDatabaseFails_CatchesExceptionAndLeavesListIntact()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var itemToDelete = new PayerSummary(targetId, "Old", "Man", 0, null, null, null, null);

            // Add the item to the UI list so we can verify it DOESN'T get removed
            _viewModel.Payers.Add(itemToDelete);

            // Force the mock database to fail (e.g., a Foreign Key constraint violation)
            _mockRepo.Setup(r => r.DeleteAsync(targetId))
                     .ThrowsAsync(new DbUpdateException("Constraint failed"));

            // Act
            await _viewModel.DeletePayerAsync(targetId);
            // Assert
            VerifyAction("Constraint failed", InfoBarType.Error, isOpen: true, count: 1);
            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Once);
        }

        [TestMethod]
        public async Task DeletePayerAsync_Valid_DeletesFromDatabaseAndRemovesFromUI()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var itemToDelete = new PayerSummary(targetId, "Old", "Man", 0, null, null, null, null);
            var itemToKeep = new PayerSummary(Guid.NewGuid(), "New", "Man", 0, null, null, null, null);

            // Add both items to the UI list
            _viewModel.Payers.Add(itemToDelete);
            _viewModel.Payers.Add(itemToKeep);

            // Act
            await _viewModel.DeletePayerAsync(targetId);

            // Assert
            // 1. Verify the repository was told to delete the correct ID
            _mockRepo.Verify(r => r.DeleteAsync(targetId), Times.Once);

            // 2. Verify the UI list was updated correctly
            VerifyAction("Payer 'Old Man' deleted successfully.", InfoBarType.Success, isOpen: true, count: 1);
            _mockRepo.Verify(r => r.DeleteAsync(targetId), Times.Once);
            Assert.AreEqual("New Man", _viewModel.Payers[0].Name); // Only the kept item remains
        }

        // --- UpdatePayerAsync Tests ---

        [TestMethod]
        public async Task UpdatePayerAsync_WhenAlreadyBusy_AbortsAndSetsMessage()
        {
            // Arrange
            _viewModel.IsBusy = true;

            // Act
            await _viewModel.UpdatePayerAsync(Guid.NewGuid(), "Payer", "Name", "Address", "ZipCode", "City", "TaxId");

            // Assert
            VerifyAction("Can't update payer while busy.", InfoBarType.Warning, isOpen: true, count: 0, isBusy: true);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdatePayerAsync_InvalidInput()
        {
            // Arrange
            _viewModel.IsBusy = false;

            // Act
            await _viewModel.UpdatePayerAsync(Guid.NewGuid(), "", "", "Address", "ZipCode", "City", "TaxId");

            // Assert
            VerifyAction("Enter at least a first or last name.", InfoBarType.Warning, isOpen: true, count: 0);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdatePayerAsync_WhenDatabaseFails_CatchesException()
        {
            // Arrange
            // Arrange
            var targetId = Guid.NewGuid();
            var originalItem = new PayerSummary(targetId, "Old", "Name", 55.0m, "", "", "", "");
            var unrelatedItem = new PayerSummary(Guid.NewGuid(), "Payer", "Name", 0, "", "", "", "");

            _viewModel.Payers.Add(originalItem);
            _viewModel.Payers.Add(unrelatedItem);

            _mockRepo.Setup(r => r.UpdateAsync(targetId, "Payer", "Name", "Address", "ZipCode", "City", "TaxId"))
                     .ThrowsAsync(new DbUpdateException("Update failed due to lock."));

            // Act
            await _viewModel.UpdatePayerAsync(targetId, "Payer", "Name", "Address", "ZipCode", "City", "TaxId");

            // Assert
            VerifyAction("Update failed due to lock.", InfoBarType.Error, isOpen: true, count: 2);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdatePayerAsync_HappyPath_UpdatesDatabaseAndReplacesUIItem()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var originalItem = new PayerSummary(targetId, "Old", "Name", 55.0m, "", "", "", "");
            var unrelatedItem = new PayerSummary(Guid.NewGuid(), "Payer", "Name", 0, "", "", "", "");

            _viewModel.Payers.Add(originalItem);
            _viewModel.Payers.Add(unrelatedItem);

            // Act - Change Name and Price
            await _viewModel.UpdatePayerAsync(targetId, "New", "Name", "Address", "ZipCode", "City", "TaxId");

            // Assert
            // 1. Verify Repo call
            _mockRepo.Verify(r => r.UpdateAsync(targetId, "New", "Name", "Address", "ZipCode", "City", "TaxId"), Times.Once);

            // 2. Verify UI Update
            VerifyAction($"Payer 'Old Name' updated successfully.", InfoBarType.Success, isOpen: true, count: 2);

            // The item at index 0 should be our updated record
            var updatedItem = _viewModel.Payers[0];
            Assert.AreEqual("New Name", updatedItem.Name);
            Assert.AreEqual(55.0m, updatedItem.Outstanding);
            Assert.AreEqual("Address", updatedItem.Address);
            Assert.AreEqual("ZipCode", updatedItem.Zip);
            Assert.AreEqual("City", updatedItem.City);
            Assert.AreEqual("TaxId", updatedItem.TaxId);

            // Unrelated item should be untouched
            Assert.AreEqual("Payer Name", _viewModel.Payers[1].Name);
        }
    }
}
