using Apolo.Services;
using Apolo.ViewModels;
using Microsoft.EntityFrameworkCore;
using Models;
using Moq;
using Repository;
using ViewModels;

namespace Apolo.Tests.ViewModels
{
    [TestClass]
    public class LessonViewModelBaseTests
    {
        protected Mock<ILessonRepository> _mockLessonRepo = null!;
        protected Mock<IStudentRepository> _mockStudentRepo = null!;
        protected Mock<IServiceRepository> _mockServiceRepo = null!;
        protected Mock<ISpecificationRepository> _mockSpecificationRepo = null!;
        protected Mock<IUserProfileService> _mockUserProfileService = null!;
        protected LessonsViewModel _viewModel = null!;

        [TestInitialize]
        public virtual void TestInit()
        {
            _mockLessonRepo = new Mock<ILessonRepository>();
            _mockStudentRepo = new Mock<IStudentRepository>();
            _mockServiceRepo = new Mock<IServiceRepository>();
            _mockSpecificationRepo = new Mock<ISpecificationRepository>();
            _mockUserProfileService = new Mock<IUserProfileService>();

            var userProfile = new UserProfile
            {
                TravelAllowance = 10,
                WeekendFee = 20
            };

            _mockUserProfileService.Setup(r => r.LoadProfileAsync())
                .ReturnsAsync(userProfile);

            _viewModel = new LessonsViewModel(
                _mockLessonRepo.Object,
                _mockStudentRepo.Object,
                _mockServiceRepo.Object,
                _mockSpecificationRepo.Object,
                _mockUserProfileService.Object);
        }
    }

    [TestClass]
    public class LessonsViewModelTests : LessonViewModelBaseTests
    {
        void VerifyAction(string? message, InfoBarType severity, bool isOpen, int lessonCount, int studentsCount, int servicesCount, bool isBusy = false)
        {
            Assert.HasCount(lessonCount, _viewModel.Lessons);
            Assert.HasCount(studentsCount, _viewModel.Students);
            Assert.HasCount(servicesCount, _viewModel.Services);
            Assert.AreEqual(message, _viewModel.InfoMessage);
            Assert.AreEqual(isBusy, _viewModel.IsBusy);
            Assert.AreEqual(isOpen, _viewModel.OpenInfoBar);
            Assert.AreEqual(severity, _viewModel.InfoBarType);
        }

        // Load Async 

        private void ArrangeForLoadTests()
        {
            _viewModel.FilterStudentName = "alice";
            _viewModel.FilterPayerName = "doe";
            _viewModel.SelectedPaymentStatusIndex = 1;
            _viewModel.FilterStartDate = new DateTimeOffset(new DateTime(2024, 1, 1));
            _viewModel.FilterEndDate = new DateTimeOffset(new DateTime(2024, 12, 1));
        }

        private async Task ActForLoadAsyncTests(bool clear = false)
        {
            var services = Helper.GetDummyServiceSummaries();
            var students = Helper.GetDummyStudentOptions();
            var lessons = Helper.GetDummyLessonSummaries();

            string student = clear ? string.Empty : "alice";
            string payer = clear ? string.Empty : "doe";
            bool? status = clear ? null : true;
            DateOnly? start = clear ? null : new DateOnly(2024, 1, 1);
            DateOnly? end = clear ? null : new DateOnly(2024, 12, 1);

            _mockStudentRepo.Setup(r => r.GetStudentOptionsAsync())
             .ReturnsAsync(students);

            _mockServiceRepo.Setup(r => r.GetServicesAsync())
             .ReturnsAsync(services);

            _mockLessonRepo.Setup(r => r.GetLessonsAsync(student, payer, status, start, end))
             .ReturnsAsync(lessons);

            if (clear)
                await _viewModel.ClearFiltersAsync();
            else
                await _viewModel.LoadAsync();
        }

        private void AssertForLoadAsyncTests(bool success, string? infoMessage, InfoBarType severity, 
            bool isBusy = false, bool clear = false, bool dbError = false)
        {
            int lessonCount = success ? 40 : 0;
            int studentsCount = success ? 11 : 0;
            int servicesCount = success ? 6 : 0;

            string student = clear ? string.Empty : "alice";
            string payer = clear ? string.Empty : "doe";
            bool? status = clear ? null : true;
            DateOnly? start = clear ? null : new DateOnly(2024, 1, 1);
            DateOnly? end = clear ? null : new DateOnly(2024, 12, 1);

            if (success)
            {
                _mockStudentRepo.Verify(r => r.GetStudentOptionsAsync(), Times.Once);
                _mockServiceRepo.Verify(r => r.GetServicesAsync(), Times.Once);
                _mockLessonRepo.Verify(r => r.GetLessonsAsync(
                    student, payer, status, start, end), Times.Once);
            }
            else
            {
                _mockStudentRepo.Verify(r => r.GetStudentOptionsAsync(), Times.Never);
                _mockServiceRepo.Verify(r => r.GetServicesAsync(), Times.Never);
                _mockLessonRepo.Verify(r => r.GetLessonsAsync(It.IsAny<string>(), It.IsAny<string>(), 
                    It.IsAny<bool?>(), It.IsAny<DateOnly?>(), It.IsAny<DateOnly?>()), Times.Never);
            }

            VerifyAction(infoMessage, severity, isOpen: true, lessonCount: lessonCount, 
                studentsCount: studentsCount, servicesCount: servicesCount, isBusy: isBusy);

        }

        [TestMethod]
        public async Task LoadAsync_WhenAlreadyBusy()
        {
            _viewModel.IsBusy = true;

            ArrangeForLoadTests();
            await ActForLoadAsyncTests();
            AssertForLoadAsyncTests(success: false, infoMessage: "Can't load lessons while busy.", 
                severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task LoadAsync_ValidInput_PopulatesCollection()
        {
            ArrangeForLoadTests();
            await ActForLoadAsyncTests();
            AssertForLoadAsyncTests(success: true, infoMessage: "40 loaded", 
                severity: InfoBarType.Success, isBusy: false);
        }


        [TestMethod]
        public async Task LoadAsync_UpdateFilter()
        {
            ArrangeForLoadTests();
            await ActForLoadAsyncTests(clear: true);
            AssertForLoadAsyncTests(success: true, infoMessage: "40 loaded",
                severity: InfoBarType.Success, isBusy: false, clear: true);
        }

        // Get student ID

        [TestMethod]
        public void ValidateStudents_InvalidStudentIds()
        {
            var exception = Assert.Throws<InvalidDataException>(() => _viewModel.GetStudent(Guid.NewGuid()));
            Assert.AreEqual("Student not loaded.", exception.Message);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        [TestMethod]
        public void ValidateStudents()
        {
            var studentId = Guid.NewGuid();
            _viewModel.Students.Add(new StudentOption(studentId, "Old Student"));
            _viewModel.Students.Add(new StudentOption(Guid.NewGuid(), "New Student"));
            var result = _viewModel.GetStudent(studentId);
            Assert.AreEqual(0, result.index);
            Assert.AreEqual("Old Student", result.item.FullName);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
        }

        // Validate lesson
        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void ValidateLesson_InvalidLessonName(string invalidName)
        {
            int? duration = 60;
            decimal tip = 10.5m;
            var result = _viewModel.ValidateLessonInput(ref invalidName, ref duration, isPricePerHour: true, basePrice: 30, tip);
            Assert.IsFalse(result);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.AreEqual("Lesson name is required.", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Warning, _viewModel.InfoBarType);
        }

        [TestMethod]
        public void ValidateLesson_InvalidTip()
        {
            var name = "Lesson";
            decimal tip = -10.5m;
            int? duration = null;
            var result = _viewModel.ValidateLessonInput(ref name, ref duration, isPricePerHour: true, basePrice: 30, tip);
            Assert.IsFalse(result);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.AreEqual("Enter a valid non-negative tip (e.g., 15.5).", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Error, _viewModel.InfoBarType);
        }

        [TestMethod]
        public void ValidateLesson_InvalidLessonDurationRequired()
        {
            var name = "Lesson";
            decimal tip = 10.5m;
            int? duration = null;
            var result = _viewModel.ValidateLessonInput(ref name, ref duration, isPricePerHour: true, basePrice: 30, tip);
            Assert.IsFalse(result);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.AreEqual("Duration is required when the lesson is priced per hour.", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Warning, _viewModel.InfoBarType);
        }

        [TestMethod]
        public void ValidateLesson_InvalidDuration()
        {
            var name = "Lesson";
            decimal tip = 10.5m;
            int? duration = -30;
            var result = _viewModel.ValidateLessonInput(ref name, ref duration, isPricePerHour: true, basePrice: 30, tip);
            Assert.IsFalse(result);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.AreEqual("Enter a valid non-negative duration (e.g., 60).", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Warning, _viewModel.InfoBarType);
        }

        [TestMethod]
        public void ValidateLesson_InvalidPrice()
        {
            int? duration = 30;
            decimal tip = 10.5m;
            var name = "Lesson";
            var result = _viewModel.ValidateLessonInput(ref name, ref duration, isPricePerHour: true, basePrice: -30, tip);
            Assert.IsFalse(result);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.AreEqual("Enter a valid non-negative price per student (e.g., 42.5).", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Warning, _viewModel.InfoBarType);
        }

        [TestMethod]
        public void ValidateLesson_DeleteDuration()
        {
            int? duration = 30;
            decimal tip = 10.5m;
            var name = "Lesson";
            var result = _viewModel.ValidateLessonInput(ref name, ref duration, isPricePerHour: false, basePrice: 30, tip);
            Assert.IsTrue(result);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsNull(duration);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        // Get Lesson

        [TestMethod]
        public void GetLesson_InvalidId_ThrowsException()
        {
            var exception = Assert.Throws<InvalidDataException>(() => _viewModel.GetLesson(Guid.NewGuid()));
            Assert.AreEqual("Lesson not loaded.", exception.Message);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        [TestMethod]
        public void GetLesson()
        {
            var student = new StudentOption(Guid.NewGuid(), "Old Man");
            var service = new ServiceSummary(Guid.NewGuid(), "Old Service", false, 30);
            var lesson = new LessonSummary(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), "Old Lesson 1", 30, false, student.Id, student.FullName, null, string.Empty, false, null, 30, false, 5, false, 5, 0, null);
            _viewModel.Lessons.Add(lesson);
            var result = _viewModel.GetLesson(lesson.Id);
            Assert.AreEqual(lesson.Name, result.lesson.Name);
            Assert.AreEqual(0, result.index);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
        }

        // Add lesson
        private void ArrangeForAddLessonTests(Guid studentId)
        {
            var student = new StudentOption(studentId, "Old Man");
            var service = new ServiceSummary(Guid.NewGuid(), "Old Service", false, 30);
            _viewModel.Students.Add(student);
            _viewModel.Services.Add(service);
        }

        private async Task ActForAddLessonTests(Guid studentId, ServiceSummary service, Guid lessonId,
            bool invalidLesson = false, bool dbError = false, bool invalidStudent = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            var name = invalidLesson ? "" : "Lesson";
            decimal tip = 10;
            string notes = "Some notes";
            Lesson result = new Lesson(date, name, isPaid: false, studentId, null, service.IsPricePerHour, null, 30, true, 10, false, 20, 10, notes);
            if (invalidStudent)
                studentId = Guid.NewGuid();

            //Mock
            if (dbError)
            {
                _mockLessonRepo.Setup(r => r.AddLessonAsync(
                    date, name, false, studentId, null, service.IsPricePerHour, null, 30, true, 10, false, 20, 10, notes))
                .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            else
            {
                _mockLessonRepo.Setup(r => r.AddLessonAsync(
                    date, name, false, studentId, null, service.IsPricePerHour, null, 30, true, 10, false, 20, 10, notes))
                 .ReturnsAsync(result);
            }
            // Act
            if (invalidStudent)
            {
                await Assert.ThrowsAsync<InvalidDataException>(async () =>
                    await _viewModel.AddLessonAsync(date, name, service, 60, 30, true, false, tip, notes, studentId));
            }
            else 
                await _viewModel.AddLessonAsync(date, name, service, 60, 30, true, false, tip, notes, studentId);
        }

        private void AssertForAddLessonTests(Guid studentId, Guid lessonId, bool success, 
            string? infoMessage, InfoBarType severity,  bool isBusy = false, bool dbError = false, bool invalidStudent = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            string notes = "Some notes";
            Guid? lessonID = _viewModel.Lessons.Count > 0 ? _viewModel.Lessons[0].Id : null;

            if (success || dbError)
            {
                _mockLessonRepo.Verify(r => r.AddLessonAsync(date, "Lesson", false, studentId, null,
                    false, null, 30, true, 10, false, 20, 10, notes), Times.Once);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.AddLessonAsync(It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<bool>(),
                    It.IsAny<Guid>(), It.IsAny<Guid?>(),
                    It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<decimal>(),
                    It.IsAny<bool>(), It.IsAny<decimal>(), It.IsAny<bool>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
                    It.IsAny<string?>()), Times.Never);
            }
            VerifyAction(infoMessage, severity, isOpen: !invalidStudent, 
                studentsCount: 1, servicesCount: 1, lessonCount: success ? 1 : 0, isBusy: isBusy);

            if (success)
            {
                var addedLesson = _viewModel.Lessons.First();
                Assert.AreEqual(lessonID, addedLesson.Id);
                Assert.AreEqual("Lesson", addedLesson.Name);
                Assert.AreEqual(date, addedLesson.Date);
                Assert.IsFalse(addedLesson.IsPricePerHour);
                Assert.IsNull(addedLesson.DurationMinutes);
                Assert.AreEqual(30, addedLesson.BasePrice);
                Assert.IsTrue(addedLesson.IsOnline);
                Assert.AreEqual(10, addedLesson.TravelAllowance);
                Assert.IsFalse(addedLesson.IsWeekendOrHoliday);
                Assert.AreEqual(20, addedLesson.WeekendFee);
                Assert.AreEqual(notes, addedLesson.Notes);
            }
        }

        [TestMethod]
        public async Task AddLesson_WhenAlreadyBusy()
        {
            var lessonId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForAddLessonTests(studentId);
            _viewModel.IsBusy = true;
            await ActForAddLessonTests(studentId, _viewModel.Services[0], lessonId);
            AssertForAddLessonTests(_viewModel.Students[0].Id, lessonId, success: false, 
                infoMessage: "Can't add lesson while busy.", severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task AddLesson_InvalidStudent()
        {
            var lessonId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForAddLessonTests(studentId);
            await ActForAddLessonTests(studentId, _viewModel.Services[0], lessonId, invalidStudent: true);
            AssertForAddLessonTests(studentId, lessonId, success: false, infoMessage: null,
                severity: InfoBarType.Success, invalidStudent: true);
        }

        [TestMethod]
        public async Task AddLesson_InvalidName()
        {
            var lessonId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForAddLessonTests(studentId);
            await ActForAddLessonTests(studentId, _viewModel.Services[0], lessonId, invalidLesson: true);
            AssertForAddLessonTests(studentId, lessonId, success: false, infoMessage: "Lesson name is required.",
                severity: InfoBarType.Warning);
        }
        [TestMethod]
        public async Task AddLesson_DBException()
        {
            var lessonId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForAddLessonTests(studentId);
            await ActForAddLessonTests(studentId, _viewModel.Services[0], lessonId, dbError: true);
            AssertForAddLessonTests(studentId, lessonId, success: false, dbError: true,
                infoMessage: "Constraint failed.", severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task AddLesson()
        {
            var lessonId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForAddLessonTests(studentId);
            await ActForAddLessonTests(studentId, _viewModel.Services[0], lessonId);
            AssertForAddLessonTests(studentId, lessonId, success: true, 
                infoMessage: "Lesson 'Lesson' added successfully.", severity: InfoBarType.Success);
        }

        // Update lesson

        private void ArrangeForUpdateLessonTests(Guid lessonId, Guid studentId)
        {
            var student = new StudentOption(studentId, "Student");
            var service = new ServiceSummary(Guid.NewGuid(), "Service", false, 30);
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            var originalLesson = new LessonSummary
            (lessonId, date, "Old lesson", 25, false, studentId, student.FullName, null, string.Empty, false, 60, 25, false, 5, false, 10, 0, null);
            var otherLesson = new LessonSummary
            (Guid.NewGuid(), date, "Other lesson", 25, false, studentId, student.FullName, null, string.Empty, false, 60, 25, false, 5, false, 10, 0, null);
            _viewModel.Lessons.Add(otherLesson);
            _viewModel.Lessons.Add(originalLesson);
            _viewModel.Students.Add(student);
            _viewModel.Services.Add(service);
        }

        private async Task ActForUpdateLessonTests(Guid lessonId, Guid studentId,
            bool invalidLesson = false, bool dbError = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1999, 8, 17));
            var name = invalidLesson ? "" : "Lesson";
            bool isPricePerHour = false;
            int? duration = null;
            decimal basePrice = 30;
            bool isOnline = true;
            decimal travelAllowance = 10;
            bool isWeekendOrHoliday = true;
            decimal weekendFee = 20;
            decimal tip = 0;
            string notes = "Some notes";
            Lesson result = new Lesson(date, name, false, studentId, null, isPricePerHour, duration, basePrice, isOnline, 
                travelAllowance, isWeekendOrHoliday, weekendFee, tip, notes);

            //Mock
            if (dbError)
            {
                _mockLessonRepo.Setup(r => r.UpdateLesson(lessonId, date, name, isPricePerHour, duration, basePrice,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, tip, notes))
                .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            else
            {
                _mockLessonRepo.Setup(r => r.UpdateLesson(lessonId, date, name, isPricePerHour, duration, basePrice,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, tip, notes))
                 .ReturnsAsync(result);
            }
            // Act
            await _viewModel.UpdateLessonAsync(lessonId, date, name, isPricePerHour, duration, basePrice, isOnline,
                travelAllowance, isWeekendOrHoliday, weekendFee, tip, notes);
        }

        private void AssertForUpdateLessonTests(Guid lessonId, Guid studentId, bool success, 
            string? infoMessage, InfoBarType severity, bool isBusy = false, bool dbError = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1999, 8, 17));
            var name = "Lesson";
            bool isPricePerHour = false;
            int? duration = null;
            decimal basePrice = 30;
            bool isOnline = true;
            decimal travelAllowance = 10;
            bool isWeekendOrHoliday = true;
            decimal weekendFee = 20;
            decimal tip = 0;
            string notes = "Some notes";

            if (success || dbError)
            {
                _mockLessonRepo.Verify(r => r.UpdateLesson(lessonId, date, name, isPricePerHour, duration, basePrice,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, tip, notes), Times.Once);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.UpdateLesson(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<decimal>(),
                    It.IsAny<bool>(), It.IsAny<decimal>(), It.IsAny<bool>(), It.IsAny<decimal>(), It.IsAny<decimal>(),
                    It.IsAny<string?>()), Times.Never);
            }
            VerifyAction(infoMessage, severity, isOpen: true, studentsCount: 1, servicesCount: 1, lessonCount: 2, isBusy: isBusy);

            if (success)
            {
                var addedLesson = _viewModel.Lessons.First(l => l.Id == lessonId);
                Assert.AreEqual("Lesson", addedLesson.Name);
                Assert.AreEqual(date, addedLesson.Date);
                Assert.IsFalse(addedLesson.IsPricePerHour);
                Assert.IsNull(addedLesson.DurationMinutes);
                Assert.AreEqual(30, addedLesson.BasePrice);
                Assert.IsTrue(addedLesson.IsOnline);
                Assert.AreEqual(10, addedLesson.TravelAllowance);
                Assert.IsTrue(addedLesson.IsWeekendOrHoliday);
                Assert.AreEqual(20, addedLesson.WeekendFee);
                Assert.AreEqual(notes, addedLesson.Notes);
                Assert.AreEqual(50, addedLesson.FinalPrice);
            }
        }

        [TestMethod]
        public async Task UpdateLessonAsync_WhenAlreadyBusy()
        {
            var lessonId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            ArrangeForUpdateLessonTests(lessonId, studentId);
            _viewModel.IsBusy = true;
            await ActForUpdateLessonTests(lessonId, studentId);
            AssertForUpdateLessonTests(lessonId, studentId, success: false, 
                infoMessage: "Can't update lesson while busy.", severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task UpdateLessonAsync_InvalidLesson()
        {
            var lessonId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            ArrangeForUpdateLessonTests(lessonId, studentId);
            await ActForUpdateLessonTests(lessonId, studentId, invalidLesson: true);
            AssertForUpdateLessonTests(lessonId, studentId, success: false, 
                infoMessage: "Lesson name is required.", severity: InfoBarType.Warning);
        }

        [TestMethod]
        public async Task UpdateLessonAsync_DBError()
        {
            var lessonId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            ArrangeForUpdateLessonTests(lessonId, studentId);
            await ActForUpdateLessonTests(lessonId, studentId, dbError: true);
            AssertForUpdateLessonTests(lessonId, studentId, success: false, dbError: true, 
                infoMessage: "Constraint failed.", severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task UpdateLessonAsync()
        {
            var lessonId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            ArrangeForUpdateLessonTests(lessonId, studentId);
            await ActForUpdateLessonTests(lessonId, studentId);
            AssertForUpdateLessonTests(lessonId, studentId, success: true, 
                infoMessage: "Lesson 'Lesson' updated successfully.", severity: InfoBarType.Success);
        }

        // Get specification options
        [TestMethod]
        public async Task GetSpecificationOptionsAsync()
        {
            var serviceId = Guid.NewGuid();
            List<Guid> ids = [Guid.NewGuid()];
            var spec1 = new SpecificationOption(Guid.NewGuid(), "Spec1", serviceId, 30, 60, false, false);
            var spec2 = new SpecificationOption(Guid.NewGuid(), "Spec2", serviceId, 30, 60, false, false);
            var spec3 = new SpecificationOption(Guid.NewGuid(), "Spec3", serviceId, 30, 60, false, false);

            _mockSpecificationRepo.Setup(r => r.GetSpecificationsForStudentAsync(ids))
                .ReturnsAsync(new List<SpecificationOption> { spec1, spec2, spec3 });

            var result = await _viewModel.GetSpecificationOptionsAsync(ids);

            Assert.HasCount(3, result);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        // Delete 

        private void ArrangeForDeleteLessonTests(Guid lessonIdWithBill, Guid lessonIdWithoutBill)
        {
            var student = new StudentOption(Guid.NewGuid(), "Student");
            var service = new ServiceSummary(Guid.NewGuid(), "Service", false, 30);
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            var lessonWithBill = new LessonSummary
            (lessonIdWithBill, date, "Lesson with bill", 25, false, student.Id, student.FullName, Guid.NewGuid(), "Invoice_Name", false, 60, 25, false, 5, false, 10, 0, null);
            var lessonWithoutBill = new LessonSummary
            (lessonIdWithoutBill, date, "Lesson without bill", 25, false, student.Id, student.FullName, null, string.Empty, false, 60, 25, false, 5, false, 10, 0, null);
            _viewModel.Lessons.Add(lessonWithBill);
            _viewModel.Lessons.Add(lessonWithoutBill);
            _viewModel.Students.Add(student);
            _viewModel.Services.Add(service);
        }

        private async Task ActForDeleteLessonTests(Guid lessonId, bool dbError = false)
        {
            //Mock
            if (dbError)
            {
                _mockLessonRepo.Setup(r => r.DeleteAsync(lessonId))
                    .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            // Act
            await _viewModel.DeleteLessonAsync(lessonId);
        }

        private void AssertForDeleteLessonTests(Guid lessonId, bool success,
            string? infoMessage, InfoBarType severity, bool isBusy = false, bool dbError = false)
        {
            if (success || dbError)
            {
                _mockLessonRepo.Verify(r => r.DeleteAsync(lessonId), Times.Once);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
            }
            VerifyAction(infoMessage, severity, isOpen: true, studentsCount: 1, servicesCount: 1, lessonCount: success ? 1 : 2, 
                isBusy: isBusy);

            if (success)
            {
                var addedLesson = _viewModel.Lessons[0];
                Assert.AreEqual("Lesson with bill", addedLesson.Name);
            }
        }

        [TestMethod]
        public async Task DeleteLesson_IsBusy()
        {
            _viewModel.IsBusy = true;
            var lessonIdWithBill = Guid.NewGuid();
            var lessonIdWithoutBill = Guid.NewGuid();
            ArrangeForDeleteLessonTests(lessonIdWithBill, lessonIdWithoutBill);
            await ActForDeleteLessonTests(lessonIdWithoutBill);
            AssertForDeleteLessonTests(lessonIdWithoutBill, success: false, infoMessage: "Can't delete lesson while busy.",
                severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task DeleteLesson_InvalidLesson()
        {
            var lessonIdWithBill = Guid.NewGuid();
            var lessonIdWithoutBill = Guid.NewGuid();
            ArrangeForDeleteLessonTests(lessonIdWithBill, lessonIdWithoutBill);
            await ActForDeleteLessonTests(lessonIdWithBill);
            AssertForDeleteLessonTests(lessonIdWithBill, success: false, 
                infoMessage: "Can't delete lesson 'Lesson with bill' for 'Student' because it's associated to bill 'Invoice_Name'",
                severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task DeleteLesson_DbError()
        {
            var lessonIdWithBill = Guid.NewGuid();
            var lessonIdWithoutBill = Guid.NewGuid();
            ArrangeForDeleteLessonTests(lessonIdWithBill, lessonIdWithoutBill);
            await ActForDeleteLessonTests(lessonIdWithoutBill, dbError: true);
            AssertForDeleteLessonTests(lessonIdWithoutBill, success: false, dbError: true,
                infoMessage: "Constraint failed.", severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task DeleteLesson()
        {
            var lessonIdWithBill = Guid.NewGuid();
            var lessonIdWithoutBill = Guid.NewGuid();
            ArrangeForDeleteLessonTests(lessonIdWithBill, lessonIdWithoutBill);
            await ActForDeleteLessonTests(lessonIdWithoutBill);
            AssertForDeleteLessonTests(lessonIdWithoutBill, success: true,
                infoMessage: $"Lesson 'Lesson without bill' deleted successfully for 'Student'",
                severity: InfoBarType.Success);
        }

        // Update Payment status

        private void ArrangeForUpdatePaymentStatusTests(Guid lessonIdWithBill, Guid lessonIdWithoutBill)
        {
            var student = new StudentOption(Guid.NewGuid(), "Student");
            var service = new ServiceSummary(Guid.NewGuid(), "Service", false, 30);
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            var lessonWithBill = new LessonSummary
            (lessonIdWithBill, date, "Lesson with bill", 25, true, student.Id, student.FullName, Guid.NewGuid(), "Invoice_Name", false, 60, 25, false, 5, false, 10, 0, null);
            var lessonWithoutBill = new LessonSummary
            (lessonIdWithoutBill, date, "Lesson without bill", 25, false, student.Id, student.FullName, null, string.Empty, false, 60, 25, false, 5, false, 10, 0, null);
            _viewModel.Lessons.Add(lessonWithBill);
            _viewModel.Lessons.Add(lessonWithoutBill);
            _viewModel.Students.Add(student);
            _viewModel.Services.Add(service);
        }

        private async Task ActForUpdatePaymentStatusTests(Guid lessonId, bool dbError = false, bool markAsPaid = true)
        {
            //Mock
            if (dbError)
            {
                _mockLessonRepo.Setup(r => r.UpdateLessonsPayment(new List<Guid> { lessonId }, markAsPaid))
                    .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            // Act
            await _viewModel.ChangePayment(lessonId);
        }

        private void AssertForUpdatePaymentStatusTests(Guid lessonId, bool success,
            string? infoMessage, InfoBarType severity, bool isBusy = false, bool dbError = false, bool markAsPaid = true)
        {
            if (success || dbError)
            {
                _mockLessonRepo.Verify(r => r.UpdateLessonsPayment(new List<Guid> { lessonId }, markAsPaid), Times.Once);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.UpdateLessonsPayment(It.IsAny<List<Guid>>(), It.IsAny<bool>()), Times.Never);
            }
            VerifyAction(infoMessage, severity, isOpen: true, studentsCount: 1, servicesCount: 1, lessonCount: 2,
                isBusy: isBusy);

            if (success)
            {
                var updatedLesson = _viewModel.Lessons[1];
                Assert.AreEqual(markAsPaid, updatedLesson.IsPaid);
            }
        }

        [TestMethod]
        public async Task UpdatePaymentStatus_IsBusy()
        {
            _viewModel.IsBusy = true;
            var lessonIdWithBill = Guid.NewGuid();
            var lessonIdWithoutBill = Guid.NewGuid();
            ArrangeForUpdatePaymentStatusTests(lessonIdWithBill, lessonIdWithoutBill);
            await ActForUpdatePaymentStatusTests(lessonIdWithoutBill);
            AssertForUpdatePaymentStatusTests(lessonIdWithoutBill, success: false, infoMessage: "Can't change payment while busy.",
                severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task UpdatePaymentStatus_MarkAsUnpaid()
        {
            var lessonIdWithBill = Guid.NewGuid();
            var lessonIdWithoutBill = Guid.NewGuid();
            ArrangeForUpdatePaymentStatusTests(lessonIdWithBill, lessonIdWithoutBill);
            await ActForUpdatePaymentStatusTests(lessonIdWithBill, markAsPaid: false);
            AssertForUpdatePaymentStatusTests(lessonIdWithBill, success: true, markAsPaid: false,
                infoMessage: "Lesson 'Lesson with bill' marked as unpaid.",
                severity: InfoBarType.Success);
        }

        [TestMethod]
        public async Task UpdatePaymentStatus_DbError()
        {
            var lessonIdWithBill = Guid.NewGuid();
            var lessonIdWithoutBill = Guid.NewGuid();
            ArrangeForUpdatePaymentStatusTests(lessonIdWithBill, lessonIdWithoutBill);
            await ActForUpdatePaymentStatusTests(lessonIdWithoutBill, dbError: true);
            AssertForUpdatePaymentStatusTests(lessonIdWithoutBill, success: false, dbError: true,
                infoMessage: "Constraint failed.", severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task UpdatePaymentStatus()
        {
            var lessonIdWithBill = Guid.NewGuid();
            var lessonIdWithoutBill = Guid.NewGuid();
            ArrangeForUpdatePaymentStatusTests(lessonIdWithBill, lessonIdWithoutBill);
            await ActForUpdatePaymentStatusTests(lessonIdWithoutBill);
            AssertForUpdatePaymentStatusTests(lessonIdWithoutBill, success: true,
                infoMessage: $"Lesson 'Lesson without bill' marked as paid.",
                severity: InfoBarType.Success);
        }

    }
}
