using Apolo.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models;
using Moq;
using Repository;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    [TestClass]
    public class ServicesViewModelTests
    {
        private Mock<IServiceRepository> _mockRepo = null!;
        private ServicesViewModel _viewModel = null!;

        [TestInitialize]
        public void TestInit()
        {
            _mockRepo = new Mock<IServiceRepository>();
            _viewModel = new ServicesViewModel(_mockRepo.Object);
        }

        void VerifyAction(string? message, InfoBarType severity, bool isOpen, int count, bool isBusy = false)
        {
            Assert.HasCount(count, _viewModel.Services);
            Assert.AreEqual(message, _viewModel.InfoMessage);           
            Assert.AreEqual(isBusy, _viewModel.IsBusy);
            Assert.AreEqual(isOpen, _viewModel.OpenInfoBar);
            Assert.AreEqual(severity, _viewModel.InfoBarType);
        }

        // Validate service
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void ValidateService_InvalidServiceName(string invalidName)
        {
            var result = _viewModel.ValidateServiceInput(ref invalidName, 60);
            Assert.IsFalse(result);
            VerifyAction("Name is required.", InfoBarType.Warning, isOpen: true, count: 0);
        }

        [TestMethod]
        public void ValidateService_InvalidPrice()
        {
            var serviceName = "Service";
            var result = _viewModel.ValidateServiceInput(ref serviceName, -60);
            Assert.IsFalse(result);
            VerifyAction("Enter a valid non-negative price (e.g., 42.50).", InfoBarType.Warning, isOpen: true, count: 0);
        }

        [TestMethod]
        public void ValidateService()
        {
            var serviceName = "Service";
            var result = _viewModel.ValidateServiceInput(ref serviceName, 60);
            Assert.IsTrue(result);
            Assert.AreEqual("Service", serviceName); // Name should be unchanged
            VerifyAction(null, InfoBarType.Success, isOpen: false, count: 0);
        }

        // Get Service

        [TestMethod]
        public void GetService_InvalidId()
        {
            var exception = Assert.Throws<InvalidDataException>(() => _viewModel.GetService(Guid.NewGuid()));
            Assert.AreEqual("Service not loaded.", exception.Message);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        [TestMethod]
        public void GetService()
        {
            var service = new ServiceSummary(Guid.NewGuid(), "Old Service", false, 30);
            _viewModel.Services.Add(service);
            var result = _viewModel.GetService(service.Id);
            Assert.AreEqual(service.Name, result.service.Name);
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

            VerifyAction("Can't load services while busy.", InfoBarType.Warning, isOpen: true, count: 0, isBusy: true);
            _mockRepo.Verify(r => r.GetServicesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task LoadAsync_ValidInput_PopulatesServicesCollection()
        {
            var firstLoad = new List<ServiceSummary>();
            firstLoad.Add(new ServiceSummary(Guid.NewGuid(), "Old Math", true, 50.0));
            firstLoad.Add(new ServiceSummary(Guid.NewGuid(), "Old Science", false, 40.0));
            var secondLoad = new List<ServiceSummary>();
            secondLoad.Add(new ServiceSummary(Guid.NewGuid(), "Math", true, 50.0));
            secondLoad.Add(new ServiceSummary(Guid.NewGuid(), "Science", false, 40.0));

            _mockRepo.SetupSequence(r => r.GetServicesAsync())
             .ReturnsAsync(firstLoad)
             .ReturnsAsync(secondLoad);

            // Act
            await _viewModel.LoadAsync(); // test that Services.Clear() is working
            await _viewModel.LoadAsync(); // If LoadAsync is called twice, you should not have duplicate items in your list

            // Assert
            // 1. Verify repository was called with correct data
            _mockRepo.Verify(r => r.GetServicesAsync(), Times.Exactly(2));

            // 2. Verify the UI collection was updated correctly
            VerifyAction(null, InfoBarType.Success, isOpen: false, count: 2);
            var addedSummary = _viewModel.Services.First();
            Assert.AreEqual("Math", addedSummary.Name);
            Assert.AreEqual(50.0, addedSummary.Price);
            Assert.IsTrue(addedSummary.IsPricePerHour);
        }

        [TestMethod]
        public async Task LoadAsync_EmptyRepository_ResultingCollectionIsEmpty()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetServicesAsync())
                .ReturnsAsync(new List<ServiceSummary>()); 

            // Act
            await _viewModel.LoadAsync();

            // Assert
            _mockRepo.Verify(r => r.GetServicesAsync(), Times.Once);
            VerifyAction(null, InfoBarType.Success, isOpen: false, count: 0);
        }

        // --- AddServiceAsync Tests ---

        [TestMethod]
        public async Task AddServiceAsync_WhenAlreadyBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.AddServiceAsync("Valid Name", false, 10m);

            VerifyAction("Can't add service while busy.", InfoBarType.Warning, isOpen: true, count: 0, isBusy: true);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Service>()), Times.Never);
        }

        [TestMethod]
        public async Task AddServiceAsync_InvalidInput()
        {
            // Act
            await _viewModel.AddServiceAsync("Math Tutoring", false, -5m);

            // Assert
            VerifyAction("Enter a valid non-negative price (e.g., 42.50).", InfoBarType.Warning, isOpen: true, count: 0);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Service>()), Times.Never);
        }

        [TestMethod]
        public async Task AddServiceAsync_WhenRepositoryThrows()
        {
            // Arrange
            _mockRepo.Setup(r => r.AddAsync(It.IsAny<Service>()))
                     .ThrowsAsync(new DbUpdateException("Database connection lost."));

            // Act
            await _viewModel.AddServiceAsync("Guitar Lesson", true, 20m);

            // Assert
            VerifyAction("Database connection lost.", InfoBarType.Error, isOpen: true, count: 0);
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Service>()), Times.Once);
        }

        [TestMethod]
        public async Task AddServiceAsync_ValidInput_SavesToRepoAndUpdatesCollection()
        {
            // Act
            await _viewModel.AddServiceAsync("Guitar Lesson", true, 45.00m);

            // Assert
            // 1. Verify repository was called with correct data
            _mockRepo.Verify(r => r.AddAsync(It.Is<Service>(s =>
                s.Name == "Guitar Lesson" &&
                s.IsPricePerHour == true &&
                s.Price == 45.00m)), Times.Once);

            // 2. Verify the UI collection was updated correctly
            VerifyAction("Service 'Guitar Lesson' added successfully.", InfoBarType.Success, isOpen: true, count: 1);
            var addedSummary = _viewModel.Services.First();
            Assert.AreEqual("Guitar Lesson", addedSummary.Name); 
            Assert.AreEqual(45.0, addedSummary.Price); 
            Assert.IsTrue(addedSummary.IsPricePerHour); 
        }

        // --- DeleteServiceAsync Tests ---

        [TestMethod]
        public async Task DeleteServiceAsync_WhenAlreadyBusy_AbortsAndSetsMessage()
        {
            // Arrange
            _viewModel.IsBusy = true;
            var itemToDelete = new ServiceSummary(Guid.NewGuid(), "Test", true, 10.0);

            // Act
            await _viewModel.DeleteServiceAsync(itemToDelete.Id);

            // Assert
            VerifyAction("Can't delete service while busy.", InfoBarType.Warning, isOpen: true, count: 0, isBusy: true);
            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task DeleteServiceAsync_WhenDatabaseFails_CatchesExceptionAndLeavesListIntact()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var itemToDelete = new ServiceSummary(targetId, "Math", true, 50.0);

            // Add the item to the UI list so we can verify it DOESN'T get removed
            _viewModel.Services.Add(itemToDelete);

            // Force the mock database to fail (e.g., a Foreign Key constraint violation)
            _mockRepo.Setup(r => r.DeleteAsync(targetId))
                     .ThrowsAsync(new DbUpdateException("Constraint failed"));

            // Act
            await _viewModel.DeleteServiceAsync(targetId);
            // Assert
            VerifyAction("Constraint failed", InfoBarType.Error, isOpen: true, count: 1);
            _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Once);
        }

        [TestMethod]
        public async Task DeleteServiceAsync_Valid_DeletesFromDatabaseAndRemovesFromUI()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var itemToDelete = new ServiceSummary(targetId, "Science", true, 40.0);
            var itemToKeep = new ServiceSummary(Guid.NewGuid(), "History", false, 30.0);

            // Add both items to the UI list
            _viewModel.Services.Add(itemToDelete);
            _viewModel.Services.Add(itemToKeep);

            // Act
            await _viewModel.DeleteServiceAsync(targetId);

            // Assert
            // 1. Verify the repository was told to delete the correct ID
            _mockRepo.Verify(r => r.DeleteAsync(targetId), Times.Once);

            // 2. Verify the UI list was updated correctly
            VerifyAction($"Service '{itemToDelete.Name}' deleted successfully.", InfoBarType.Success, isOpen: true, count: 1);
            _mockRepo.Verify(r => r.DeleteAsync(targetId), Times.Once);
            Assert.AreEqual("History", _viewModel.Services[0].Name); // Only the kept item remains
            Assert.IsFalse(_viewModel.Services[0].IsPricePerHour); 
            Assert.AreEqual(30.0, _viewModel.Services[0].Price); 
        }

        // --- UpdateServiceAsync Tests ---

        [TestMethod]
        public async Task UpdateServiceAsync_WhenAlreadyBusy_AbortsAndSetsMessage()
        {
            // Arrange
            _viewModel.IsBusy = true;

            // Act
            await _viewModel.UpdateServiceAsync(Guid.NewGuid(), "Test", true, 10.0m);

            // Assert
            VerifyAction("Can't update service while busy.", InfoBarType.Warning, isOpen: true, count: 0, isBusy: true);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<decimal>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateServiceAsync_InvalidInput()
        {
            // Act
            await _viewModel.UpdateServiceAsync(Guid.NewGuid(), "Math", true, -5.0m);

            // Assert
            VerifyAction("Enter a valid non-negative price (e.g., 42.50).", InfoBarType.Warning, isOpen: true, count: 0);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<decimal>()), Times.Never);
        }

        [TestMethod]
        public async Task UpdateServiceAsync_WhenDatabaseFails_CatchesException()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var originalItem = new ServiceSummary(targetId, "Math", true, 40.0);
            var unrelatedItem = new ServiceSummary(Guid.NewGuid(), "Science", false, 30.0);

            _viewModel.Services.Add(originalItem);
            _viewModel.Services.Add(unrelatedItem);

            _mockRepo.Setup(r => r.UpdateAsync(targetId, "Math", true, 50m))
                     .ThrowsAsync(new DbUpdateException("Update failed due to lock."));

            // Act
            await _viewModel.UpdateServiceAsync(targetId, "Math", true, 50m);

            // Assert
            VerifyAction("Update failed due to lock.", InfoBarType.Error, isOpen: true, count: 2);
            _mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<decimal>()), Times.Once);
        }

        [TestMethod]
        public async Task UpdateServiceAsync_HappyPath_UpdatesDatabaseAndReplacesUIItem()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var originalItem = new ServiceSummary(targetId, "Math", true, 40.0);
            var unrelatedItem = new ServiceSummary(Guid.NewGuid(), "Science", false, 30.0);

            _viewModel.Services.Add(originalItem);
            _viewModel.Services.Add(unrelatedItem);

            // Act - Change Name and Price
            await _viewModel.UpdateServiceAsync(targetId, "Advanced Math", true, 55.0m);

            // Assert
            // 1. Verify Repo call
            _mockRepo.Verify(r => r.UpdateAsync(targetId, "Advanced Math", true, 55.0m), Times.Once);

            // 2. Verify UI Update
            VerifyAction($"Service 'Advanced Math' updated successfully.", InfoBarType.Success, isOpen: true, count: 2);

            // The item at index 0 should be our updated record
            var updatedItem = _viewModel.Services[0];
            Assert.AreEqual("Advanced Math", updatedItem.Name);
            Assert.AreEqual(55.0, updatedItem.Price);
            Assert.IsTrue(updatedItem.IsPricePerHour);

            // Unrelated item should be untouched
            Assert.AreEqual("Science", _viewModel.Services[1].Name);
        }
    }
}
