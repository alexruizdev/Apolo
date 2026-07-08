using Apolo.Services;
using Apolo.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models;
using Moq;
using Repository;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    public class SpecificationsViewModelBaseTests
    {
        protected Mock<ISpecificationRepository> _mockSpecificationRepo = null!;
        protected Mock<IStudentRepository> _mockStudentRepo = null!;
        protected Mock<IServiceRepository> _mockServiceRepo = null!;
        protected Mock<ILessonRepository> _mockLessonRepo = null!;
        protected Mock<IUserProfileService> _mockUserProfileService = null!;
        protected SpecificationsViewModel _viewModel = null!;
        private Mock<IStringLocalizer> _localizerMock = null!;


        [TestInitialize]
        public virtual void TestInit()
        {
            _mockSpecificationRepo = new Mock<ISpecificationRepository>();
            _mockStudentRepo = new Mock<IStudentRepository>();
            _mockServiceRepo = new Mock<IServiceRepository>();
            _mockLessonRepo = new Mock<ILessonRepository>();
            _mockUserProfileService = new Mock<IUserProfileService>();
            _localizerMock = new Mock<IStringLocalizer>();

            var userProfile = new UserProfile
            {
                TravelAllowance = 10,
                WeekendFee = 20
            };

            _mockUserProfileService.Setup(r => r.LoadProfileAsync())
                .ReturnsAsync(userProfile);

            _viewModel = new SpecificationsViewModel(
                _mockSpecificationRepo.Object,
                _mockStudentRepo.Object,
                _mockServiceRepo.Object,
                _mockLessonRepo.Object,
                _mockUserProfileService.Object,
                _localizerMock.Object);
        }
    }

    [TestClass]
    public class SpecificationsViewModelTests : SpecificationsViewModelBaseTests
    {
        void VerifyAction(InfoBarType severity, bool isOpen, int specCount, int studentsCount, int servicesCount, bool isBusy = false)
        {
            Assert.HasCount(specCount, _viewModel.Specifications);
            Assert.HasCount(studentsCount, _viewModel.Students);
            Assert.HasCount(servicesCount, _viewModel.Services);
            Assert.AreEqual(isBusy, _viewModel.IsBusy);
            Assert.AreEqual(isOpen, _viewModel.OpenInfoBar);
            Assert.AreEqual(severity, _viewModel.InfoBarType);
        }

        // Get Service

        [TestMethod]
        public void GetService_InvalidId()
        {
            var exception = Assert.Throws<InvalidDataException>(() => _viewModel.GetService(Guid.NewGuid()));
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        [TestMethod]
        public void GetService()
        {
            var service = new ServiceSummary(Guid.NewGuid(), "Old Service", false, 30);
            _viewModel.Services.Add(service);
            var (value, index) = _viewModel.GetService(service.Id);
            Assert.AreEqual(service.Name, value.Name);
            Assert.AreEqual(0, index);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
        }

        // Get specification

        [TestMethod]
        public void GetSpecification_InvalidId()
        {
            var exception = Assert.Throws<InvalidDataException>(() => _viewModel.GetSpecification(Guid.NewGuid()));
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        [TestMethod]
        public void GetSpecification()
        {
            var spec = new SpecificationSummary(Guid.NewGuid(), "Old Default", Guid.NewGuid(), 
                "Student", Guid.NewGuid(), "Service", 60, null, false, false, 0);
            _viewModel.Specifications.Add(spec);
            var (value, index) = _viewModel.GetSpecification(spec.Id);
            Assert.AreEqual(spec.Name, value.Name);
            Assert.AreEqual(0, index);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
        }

        // --- LoadAsync Tests ---

        [TestMethod]
        public async Task LoadAsync_WhenAlreadyBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.LoadAsync();

            VerifyAction(InfoBarType.Warning, isOpen: true, specCount: 0, studentsCount: 0, servicesCount: 0, isBusy: true);
            _mockStudentRepo.Verify(r => r.GetStudentOptionsAsync(), Times.Never);
            _mockServiceRepo.Verify(r => r.GetServicesAsync(), Times.Never);
            _mockSpecificationRepo.Verify(r => r.GetSpecificationsAsync(), Times.Never);
        }

        [TestMethod]
        public async Task LoadAsync_ValidInput_PopulatesCollection()
        {
            var firstStudentLoad = new List<StudentOption>
            {
                new(Guid.NewGuid(), "Old Man"),
                new(Guid.NewGuid(), "Old Kid")
            };
            var secondStudentLoad = new List<StudentOption>
            {
                new(Guid.NewGuid(), "New Man"),
                new(Guid.NewGuid(), "New Kid")
            };
            var firstServiceLoad = new List<ServiceSummary>
            {
                new(Guid.NewGuid(), "Old Service", false, 30),
                new(Guid.NewGuid(), "Old Contract", false, 30)
            };
            var secondServiceLoad = new List<ServiceSummary>
            {
                new(Guid.NewGuid(), "New Service", true, 50),
                new(Guid.NewGuid(), "New Contract", true, 50)
            };
            var firstSpecificationLoad = new List<SpecificationSummary>
            {
                new(Guid.NewGuid(), "Old Default", firstStudentLoad[0].Id, firstStudentLoad[0].FullName, firstServiceLoad[0].Id, firstServiceLoad[0].Name, 60, null, false, false, 0),
                new(Guid.NewGuid(), "Old Spec", firstStudentLoad[1].Id, firstStudentLoad[1].FullName, firstServiceLoad[1].Id, firstServiceLoad[1].Name, 60, null, false, false, 0)
            };
            var secondSpecificationLoad = new List<SpecificationSummary>
            {
                new(Guid.NewGuid(), "New Default", secondStudentLoad[0].Id, secondStudentLoad[0].FullName, secondServiceLoad[0].Id, secondServiceLoad[0].Name, 90, 50, true, true, 0),
                new(Guid.NewGuid(), "New Spec", secondStudentLoad[1].Id, secondStudentLoad[1].FullName, secondServiceLoad[1].Id, secondServiceLoad[1].Name, 90, 50, true, true, 0)
            };

            _mockStudentRepo.SetupSequence(r => r.GetStudentOptionsAsync())
             .ReturnsAsync(firstStudentLoad)
             .ReturnsAsync(secondStudentLoad);

            _mockServiceRepo.SetupSequence(r => r.GetServicesAsync())
             .ReturnsAsync(firstServiceLoad)
             .ReturnsAsync(secondServiceLoad);

            _mockSpecificationRepo.SetupSequence(r => r.GetSpecificationsAsync())
             .ReturnsAsync(firstSpecificationLoad)
             .ReturnsAsync(secondSpecificationLoad);

            await _viewModel.LoadAsync(); // test that Specifications.Clear() is working
            await _viewModel.LoadAsync(); // If LoadAsync is called twice, you should not have duplicate items in your list

            // 1. Verify repository was called with correct data
            _mockStudentRepo.Verify(r => r.GetStudentOptionsAsync(), Times.Exactly(2));
            _mockServiceRepo.Verify(r => r.GetServicesAsync(), Times.Exactly(2));
            _mockSpecificationRepo.Verify(r => r.GetSpecificationsAsync(), Times.Exactly(2));

            // 2. Verify the UI collection was updated correctly
            VerifyAction(InfoBarType.Success, isOpen: true, specCount: 2, studentsCount: 2, servicesCount: 2);
            var addedStudent = _viewModel.Students.First();
            var addedService = _viewModel.Services.First();
            var addedSpecification = _viewModel.Specifications.First();
            Assert.AreEqual("New Man", addedStudent.FullName);
            Assert.AreEqual("New Service", addedService.Name);
            Assert.AreEqual("New Default", addedSpecification.Name);
        }

        [TestMethod]
        public async Task LoadAsync_EmptyRepository_ResultingCollectionIsEmpty()
        {
            _mockStudentRepo.SetupSequence(r => r.GetStudentOptionsAsync())
                .ReturnsAsync([]);
            _mockServiceRepo.SetupSequence(r => r.GetServicesAsync())
                .ReturnsAsync([]);
            _mockSpecificationRepo.SetupSequence(r => r.GetSpecificationsAsync())
                .ReturnsAsync([]);


            await _viewModel.LoadAsync();

            _mockStudentRepo.Verify(r => r.GetStudentOptionsAsync(), Times.Once);
            _mockServiceRepo.Verify(r => r.GetServicesAsync(), Times.Once);
            _mockSpecificationRepo.Verify(r => r.GetSpecificationsAsync(), Times.Once);

            VerifyAction(InfoBarType.Success, isOpen: true, specCount: 0, studentsCount: 0, servicesCount: 0);
        }

        // --- Refresh specifications Tests ---

        [TestMethod]
        public async Task RefreshSpecificationsAsync_WhenAlreadyBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.RefreshSpecifications();

            VerifyAction(InfoBarType.Warning, isOpen: true, specCount: 0, studentsCount: 0, servicesCount: 0, isBusy: true);
            _mockSpecificationRepo.Verify(r => r.GetSpecificationsAsync(), Times.Never);
        }

        [TestMethod]
        public async Task RefreshSpecificationsAsync()
        {
            var firstSpecificationLoad = new List<SpecificationSummary>
            {
                new(Guid.NewGuid(), "Spec 1", Guid.NewGuid(), "Student 1", Guid.NewGuid(), "Service 1", 60, null, false, false, 1),
                new(Guid.NewGuid(), "Spec 2", Guid.NewGuid(), "Student 2", Guid.NewGuid(), "Service 2", 60, null, false, false, 3)
            };


            _mockSpecificationRepo.SetupSequence(r => r.GetSpecificationsAsync())
             .ReturnsAsync(firstSpecificationLoad);

            await _viewModel.RefreshSpecifications();

            VerifyAction(InfoBarType.Success, isOpen: false, specCount: 2, studentsCount: 0, servicesCount: 0);
            _mockSpecificationRepo.Verify(r => r.GetSpecificationsAsync(), Times.Once);
        }

        // --- AddSpecificationAsync Tests ---

        [TestMethod]
        public async Task AddSpecificationAsync_WhenAlreadyBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.AddSpecificationAsync("Specification", 60, null, false, false, Guid.NewGuid(), Guid.NewGuid());

            VerifyAction(InfoBarType.Warning, isOpen: true, specCount: 0, studentsCount: 0, servicesCount: 0, isBusy: true);
            _mockSpecificationRepo.Verify(r => r.AddSpecificationAsync(It.IsAny<Specification>()), Times.Never);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task AddSpecificationAsync_WhenNameIsInvalid(string invalidName)
        {
            var service = new ServiceSummary(Guid.NewGuid(), "New Service", true, 50);
            var student = new StudentOption(Guid.NewGuid(), "Old Man");
            _viewModel.Services.Add(service);
            _viewModel.Students.Add(student);

            await _viewModel.AddSpecificationAsync(invalidName, 60, null, false, false, student.Id, service.Id);

            VerifyAction(InfoBarType.Warning, isOpen: true, specCount: 0, studentsCount: 1, servicesCount: 1);
            _mockSpecificationRepo.Verify(r => r.AddSpecificationAsync(It.IsAny<Specification>()), Times.Never);
        }

        [TestMethod]
        public async Task AddSpecificationAsync_WhenDurationIsInvalid()
        {
            await _viewModel.AddSpecificationAsync("Specification", -5, null, false, false, Guid.NewGuid(), Guid.NewGuid());

            VerifyAction(InfoBarType.Warning, isOpen: true, specCount: 0, studentsCount: 0, servicesCount: 0);
            _mockSpecificationRepo.Verify(r => r.AddSpecificationAsync(It.IsAny<Specification>()), Times.Never);
        }

        [TestMethod]
        public async Task AddSpecificationAsync_ServiceIsInvalid()
        {
            _viewModel.Services.Add(new ServiceSummary(Guid.NewGuid(), "New Service", true, 50));

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
                await _viewModel.AddSpecificationAsync("Specification", 60, null, false, false, Guid.NewGuid(), Guid.NewGuid())
            );

            _mockSpecificationRepo.Verify(r => r.AddSpecificationAsync(It.IsAny<Specification>()), Times.Never);
        }

        [TestMethod]
        public async Task AddSpecificationAsync_StudentIsInvalid()
        {
            _viewModel.Students.Add(new StudentOption(Guid.NewGuid(), "Old Man"));

            await Assert.ThrowsAsync<InvalidDataException>(async () =>
                await _viewModel.AddSpecificationAsync("Specification", 60, null, false, false, Guid.NewGuid(), Guid.NewGuid())
            );

            _mockSpecificationRepo.Verify(r => r.AddSpecificationAsync(It.IsAny<Specification>()), Times.Never);
        }

        [TestMethod]
        public async Task AddSpecificationAsync_WhenSpecificationRepositoryThrows()
        {
            var service = new ServiceSummary(Guid.NewGuid(), "New Service", true, 50);
            var student = new StudentOption(Guid.NewGuid(), "Old Man");
            _viewModel.Services.Add(service);
            _viewModel.Students.Add(student);

            _mockSpecificationRepo.Setup(r => r.AddSpecificationAsync(It.IsAny<Specification>()))
                     .ThrowsAsync(new DbUpdateException("Database connection lost."));

            await _viewModel.AddSpecificationAsync("Specification", 60, null, false, false, student.Id, service.Id);

            VerifyAction(InfoBarType.Error, isOpen: true, specCount: 0, studentsCount: 1, servicesCount: 1);
            _mockSpecificationRepo.Verify(r => r.AddSpecificationAsync(It.IsAny<Specification>()), Times.Once);
        }

        [TestMethod]
        public async Task AddSpecificationAsync_ValidInput()
        {
            var service = new ServiceSummary(Guid.NewGuid(), "New Service", true, 50);
            var student = new StudentOption(Guid.NewGuid(), "Old Man");
            _viewModel.Services.Add(service);
            _viewModel.Students.Add(student);

            await _viewModel.AddSpecificationAsync("Specification", 60, null, false, false, student.Id, service.Id);

            VerifyAction(InfoBarType.Success, isOpen: true, specCount: 1, studentsCount: 1, servicesCount: 1);
            _mockSpecificationRepo.Verify(r => r.AddSpecificationAsync(It.IsAny<Specification>()), Times.Once);

            var addedSpecification = _viewModel.Specifications.First();
            Assert.AreEqual("Specification", addedSpecification.Name);
            Assert.AreEqual(student.Id, addedSpecification.StudentId);
            Assert.AreEqual(student.FullName, addedSpecification.StudentName);
            Assert.AreEqual(service.Id, addedSpecification.ServiceId);
            Assert.AreEqual(service.Name, addedSpecification.ServiceName);
            Assert.AreEqual(60, addedSpecification.DurationMinutes);
            Assert.IsNull(addedSpecification.Price);
            Assert.IsFalse(addedSpecification.IsOnline);
            Assert.IsFalse(addedSpecification.IsWeekendOrHoliday);
        }

        // --- DeleteSpecificationAsync Tests ---

        [TestMethod]
        public async Task DeleteSpecificationAsync_WhenAlreadyBusy_AbortsAndSetsMessage()
        {
            // Arrange
            var service = new ServiceSummary(Guid.NewGuid(), "Service", true, 50);
            var student = new StudentOption(Guid.NewGuid(), "Student");
            var itemToDelete = new SpecificationSummary(Guid.NewGuid(), "Spec", student.Id,
                student.FullName, service.Id, service.Name, 60, null, false, false, 0);
            _viewModel.IsBusy = true;
            _viewModel.Services.Add(service);
            _viewModel.Students.Add(student);
            _viewModel.Specifications.Add(itemToDelete);

            // Act
            await _viewModel.DeleteSpecificationAsync(itemToDelete.Id);

            // Assert
            VerifyAction(InfoBarType.Warning, isOpen: true, studentsCount: 1, servicesCount: 1, specCount: 1, isBusy: true);
            _mockSpecificationRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        }

        [TestMethod]
        public async Task DeleteSpecificationAsync_WhenDatabaseFails_CatchesExceptionAndLeavesListIntact()
        {
            // Arrange
            var service = new ServiceSummary(Guid.NewGuid(), "Service", true, 50);
            var student = new StudentOption(Guid.NewGuid(), "Student");
            var itemToDelete = new SpecificationSummary(Guid.NewGuid(), "Spec", student.Id,
                student.FullName, service.Id, service.Name, 60, null, false, false, 0);

            _viewModel.Services.Add(service);
            _viewModel.Students.Add(student);
            _viewModel.Specifications.Add(itemToDelete);

            // Force the mock database to fail (e.g., a Foreign Key constraint violation)
            _mockSpecificationRepo.Setup(r => r.DeleteAsync(itemToDelete.Id))
                     .ThrowsAsync(new DbUpdateException("Constraint failed."));

            // Act
            await _viewModel.DeleteSpecificationAsync(itemToDelete.Id);

            // Assert
            VerifyAction(InfoBarType.Error, isOpen: true, studentsCount: 1, servicesCount: 1, specCount: 1);
            _mockSpecificationRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Once);
        }

        [TestMethod]
        public async Task DeleteSpecificationAsync_Valid_DeletesFromDatabaseAndRemovesFromUI()
        {
            // Arrange
            var service = new ServiceSummary(Guid.NewGuid(), "Service", true, 50);
            var student = new StudentOption(Guid.NewGuid(), "Student");
            var itemToDelete = new SpecificationSummary(Guid.NewGuid(), "Spec", student.Id,
                student.FullName, service.Id, service.Name, 60, null, false, false, 0);
            var itemToKeep = new SpecificationSummary(Guid.NewGuid(), "Spec to keep", student.Id,
                student.FullName, service.Id, service.Name, 90, 35, true, true, 0);

            _viewModel.Services.Add(service);
            _viewModel.Students.Add(student);
            _viewModel.Specifications.Add(itemToDelete);
            _viewModel.Specifications.Add(itemToKeep);

            // Act
            await _viewModel.DeleteSpecificationAsync(itemToDelete.Id);

            // Assert
            VerifyAction(InfoBarType.Success, isOpen: true, studentsCount: 1, servicesCount: 1, specCount: 1);
            _mockSpecificationRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Once);
            Assert.AreEqual("Spec to keep", _viewModel.Specifications[0].Name); // Only the kept item remains
        }

        // --- UpdateSpecificationAsync Tests ---

        private void ArrangeForUpdateTests(Guid targetId, Guid service2Id)
        {
            // Arrange
            var service1 = new ServiceSummary(Guid.NewGuid(), "Service1", true, 50);
            var service2 = new ServiceSummary(service2Id, "Service2", false, 90);
            var student = new StudentOption(Guid.NewGuid(), "Student");
            var originalItem = new SpecificationSummary(targetId, "Spec", student.Id,
                student.FullName, service1.Id, service1.Name, 60, null, false, false, 0);
            var unrelatedItem = new SpecificationSummary(Guid.NewGuid(), "Keep Specification", student.Id,
                student.FullName, service1.Id, service1.Name, 60, null, false, false, 0);

            _viewModel.Students.Add(student);
            _viewModel.Services.Add(service1);
            _viewModel.Services.Add(service2);
            _viewModel.Specifications.Add(originalItem);
            _viewModel.Specifications.Add(unrelatedItem);
        }

        private async Task ActForUpdateTests(Guid targetId, Guid service2Id, string name = "Specification", int duration = 10)
        {
            await _viewModel.UpdateSpecificationAsync(targetId, name, duration, null, true, true, service2Id);
        }

        private void AssertUpdateTests(Guid targetId, Guid service2Id,
            bool isBusy, InfoBarType severity, bool success, string name = "Specification", int duration = 10)
        {
            // 1. Verify Repo call
            if (success)
            {
                _mockSpecificationRepo.Verify(r => r.UpdateAsync(targetId, service2Id, name, duration,
                    null, true, true), Times.Once);
            }
            else
            {
                _mockSpecificationRepo.Verify(r => r.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<int>(),
                    It.IsAny<decimal?>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
            }

            // 2. Verify UI Update
            VerifyAction(severity, isOpen: true, studentsCount: 1, servicesCount: 2, specCount: 2, isBusy: isBusy);

            // The item at index 0 should be our updated record
            var updatedItem = _viewModel.Specifications[0];
            Assert.AreEqual(success ? "Specification" : "Spec", updatedItem.Name);
            Assert.AreEqual(success ? "Service2" : "Service1", updatedItem.ServiceName);

            // Unrelated item should be untouched
            Assert.AreEqual("Keep Specification", _viewModel.Specifications[1].Name);
        }

        [TestMethod]
        public async Task UpdateSpecificationAsync_WhenAlreadyBusy_AbortsAndSetsMessage()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var service2Id = Guid.NewGuid();
            ArrangeForUpdateTests(targetId, service2Id);

            _viewModel.IsBusy = true;

            // Act
            await ActForUpdateTests(targetId, service2Id);

            // Assert
            AssertUpdateTests(targetId, service2Id, isBusy: true, success: false, severity: InfoBarType.Warning);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public async Task UpdateSpecificationAsync_WhenNameInvalid_AbortsAndSetsMessage(string invalidName)
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var service2Id = Guid.NewGuid();
            ArrangeForUpdateTests(targetId, service2Id);

            // Act
            await ActForUpdateTests(targetId, service2Id, name: invalidName);

            // Assert
            AssertUpdateTests(targetId, service2Id, isBusy: false, success: false, name: invalidName, severity: InfoBarType.Warning);
        }

        [TestMethod]
        public async Task UpdateSpecificationAsync_WhenDurationInvalid_AbortsAndSetsMessage()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var service2Id = Guid.NewGuid();
            ArrangeForUpdateTests(targetId, service2Id);

            // Act
            await ActForUpdateTests(targetId, service2Id, duration: -1);

            // Assert
            AssertUpdateTests(targetId, service2Id, isBusy: false, success: false, duration: -1, severity: InfoBarType.Warning);

        }

        [TestMethod]
        public async Task UpdateSpecificationAsync_HappyPath_UpdatesDatabaseAndReplacesUIItem()
        {
            // Arrange
            var targetId = Guid.NewGuid();
            var service2Id = Guid.NewGuid();
            ArrangeForUpdateTests(targetId, service2Id);

            // Act
            await ActForUpdateTests(targetId, service2Id);

            // Assert
            AssertUpdateTests(targetId, service2Id, isBusy: false, success: true, severity: InfoBarType.Success);
        }

        // --- CreateLessonFromSpecificationAsync Tests ---

        private void ArrangeForCreateLessonTests(Guid targetId, Guid studentId, bool hasPrice = false, bool invalidService = false)
        {
            // Arrange
            var service = new ServiceSummary(Guid.NewGuid(), "Service", true, 50);
            var student = new StudentOption(studentId, "Student");
            var spec = new SpecificationSummary(targetId, "Spec", student.Id,
                student.FullName, invalidService ? Guid.NewGuid() : service.Id, service.Name,
                60, hasPrice ? 60 : null, false, false, 0);

            _viewModel.Students.Add(student);
            _viewModel.Services.Add(service);
            _viewModel.Specifications.Add(spec);
        }

        private async Task ActForCreateLessonTests(Guid targetId, bool invalidTip = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            string notes = "Some notes";

            await _viewModel.CreateLessonFromSpecificationAsync(targetId, date, invalidTip ? -10 : 10, notes);
        }

        private void ThrowExceptionForCreateLessonTests(Guid studentId, bool hasPrice = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            string notes = "Some notes";
            List<Guid> ids = [studentId];
            _mockLessonRepo.Setup(r => r.AddLessonAsync(date, "Service", false, studentId, null, 
                    true, 60, hasPrice ? 60 : 50,
                    false, 10, false, 20, 10,
                    notes))
                     .ThrowsAsync(new DbUpdateException("Constraint failed."));
        }

        private void AssertForCreateLessonTests(Guid targetId, Guid studentId, bool success, InfoBarType severity,
            bool isBusy = false, bool hasPrice = false, bool dbError = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            string notes = "Some notes";
            List<Guid> ids = [studentId];

            // 1. Verify Repo call
            if (success)
            {
                _mockLessonRepo.Verify(r => r.AddLessonAsync(date, "Service", false, studentId, null,
                    true, 60, hasPrice ? 60 : 50,
                    false, 10, false, 20, 10,
                    notes), Times.Once);
                _mockSpecificationRepo.Verify(r => r.IncrementUsageAsync(targetId), Times.Once);
            }
            else if (dbError)
            {
                _mockLessonRepo.Verify(r => r.AddLessonAsync(date, "Service", false, studentId, null,
                    true, 60, hasPrice ? 60 : 50,
                    false, 10, false, 20, 10,
                    notes), Times.Once);
                _mockSpecificationRepo.Verify(r => r.IncrementUsageAsync(It.IsAny<Guid>()), Times.Never);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.AddLessonAsync(It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<decimal>(),
                    It.IsAny<bool>(), It.IsAny<decimal>(), It.IsAny<bool>(), It.IsAny<decimal>(), It.IsAny<decimal>(), 
                    It.IsAny<string?>()), Times.Never);
                _mockSpecificationRepo.Verify(r => r.IncrementUsageAsync(It.IsAny<Guid>()), Times.Never);
            }

            // 2. Verify UI Update
            VerifyAction(severity, isOpen: true, studentsCount: 1, servicesCount: 1, specCount: 1, isBusy: isBusy);
        }

        [TestMethod]
        public async Task CreateLessonFromSpecificationAsync_WhenAlreadyBusy()
        {
            var targetId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForCreateLessonTests(targetId, studentId);

            _viewModel.IsBusy = true;

            await ActForCreateLessonTests(targetId);

            AssertForCreateLessonTests(targetId, studentId, success: false, isBusy: true, severity: InfoBarType.Warning);
        }

        [TestMethod]
        public async Task CreateLessonFromSpecificationAsync_InvalidTip()
        {
            var targetId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForCreateLessonTests(targetId, studentId);

            await ActForCreateLessonTests(targetId, invalidTip: true);

            AssertForCreateLessonTests(targetId, studentId, success: false, isBusy: false, severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task CreateLessonFromSpecificationAsync_InvalidSpecificationId()
        {
            var targetId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var invalidId = Guid.NewGuid(); // This ID does not exist in the list
            ArrangeForCreateLessonTests(targetId, studentId);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                 ActForCreateLessonTests(invalidId));

            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        [TestMethod]
        public async Task CreateLessonFromSpecificationAsync_InvalidServiceId()
        {
            var targetId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForCreateLessonTests(targetId, studentId, invalidService: true);

            var exception = await Assert.ThrowsAsync<InvalidDataException>(() =>
                 ActForCreateLessonTests(targetId));

            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }


        [TestMethod]
        public async Task CreateLessonFromSpecificationAsync_WhenDatabaseFails_CatchesExceptionAndLeavesListIntact()
        {
            var targetId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForCreateLessonTests(targetId, studentId);

            // Force the mock database to fail (e.g., a Foreign Key constraint violation)
            ThrowExceptionForCreateLessonTests(studentId);

            await ActForCreateLessonTests(targetId);

            // Assert
            AssertForCreateLessonTests(targetId, studentId, success: false, dbError: true, severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task CreateLessonFromSpecificationAsync_ValidPath_NoSpecPrice()
        {
            var targetId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForCreateLessonTests(targetId, studentId, hasPrice: false);

            await ActForCreateLessonTests(targetId);

            // Assert
            AssertForCreateLessonTests(targetId, studentId, success: true, hasPrice: false, severity: InfoBarType.Success);
        }

        [TestMethod]
        public async Task CreateLessonFromSpecificationAsync_ValidPath_SpecPrice()
        {
            var targetId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForCreateLessonTests(targetId, studentId, hasPrice: true);

            await ActForCreateLessonTests(targetId);

            // Assert
            AssertForCreateLessonTests(targetId, studentId, success: true, hasPrice: true, severity: InfoBarType.Success);
        }
    }
}
