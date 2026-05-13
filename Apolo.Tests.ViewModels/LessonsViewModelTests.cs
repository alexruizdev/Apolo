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
    public class LessonsViewModelTests
    {
        private Mock<ILessonRepository> _mockLessonRepo = null!;
        private Mock<IStudentRepository> _mockStudentRepo = null!;
        private Mock<IServiceRepository> _mockServiceRepo = null!;
        private Mock<ISpecificationRepository> _mockSpecificationRepo = null!;
        private Mock<IUserProfileService> _mockUserProfileService = null!;
        private LessonsViewModel _viewModel = null!;

        [TestInitialize]
        public void TestInit()
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
        [TestMethod]
        public async Task LoadAsync_WhenAlreadyBusy()
        {
            _viewModel.IsBusy = true;

            await _viewModel.LoadAsync();

            VerifyAction("Can't load lessons while busy.", InfoBarType.Warning, isOpen: true, 
                lessonCount: 0, studentsCount: 0, servicesCount: 0, isBusy: true);
            _mockStudentRepo.Verify(r => r.GetStudentOptionsAsync(), Times.Never);
            _mockServiceRepo.Verify(r => r.GetServicesAsync(), Times.Never);
            _mockLessonRepo.Verify(r => r.GetLessonsAsync(It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
        }

        [TestMethod]
        public async Task LoadAsync_ValidInput_PopulatesCollection()
        {
            var firstStudentLoad = new List<StudentOption>();
            firstStudentLoad.Add(new StudentOption(Guid.NewGuid(), "Old Man"));
            firstStudentLoad.Add(new StudentOption(Guid.NewGuid(), "Old Kid"));
            var secondStudentLoad = new List<StudentOption>();
            secondStudentLoad.Add(new StudentOption(Guid.NewGuid(), "New Man"));
            secondStudentLoad.Add(new StudentOption(Guid.NewGuid(), "New Kid"));
            var firstServiceLoad = new List<ServiceSummary>();
            firstServiceLoad.Add(new ServiceSummary(Guid.NewGuid(), "Old Service", false, 30));
            firstServiceLoad.Add(new ServiceSummary(Guid.NewGuid(), "Old Contract", false, 30));
            var secondServiceLoad = new List<ServiceSummary>();
            secondServiceLoad.Add(new ServiceSummary(Guid.NewGuid(), "New Service", true, 50));
            secondServiceLoad.Add(new ServiceSummary(Guid.NewGuid(), "New Contract", true, 50));
            var firstLessonLoad = new List<LessonSummary>();
            var firstAttendanceSummary1 = new AttendanceSummary(Guid.NewGuid(), firstStudentLoad[0].Id, firstStudentLoad[0].FullName, false);
            var firstAttendanceSummary2 = new AttendanceSummary(Guid.NewGuid(), firstStudentLoad[1].Id, firstStudentLoad[1].FullName, false);
            firstLessonLoad.Add(new LessonSummary(Guid.NewGuid(), "Old Lesson 1", DateOnly.FromDateTime(DateTime.Today), false, null, 30, false, 5, false, 5, null, [firstAttendanceSummary1]));
            firstLessonLoad.Add(new LessonSummary(Guid.NewGuid(), "Old Lesson 2", DateOnly.FromDateTime(DateTime.Today), false, null, 30, false, 5, false, 5, null, [firstAttendanceSummary2]));
            var secondAttendanceSummary1 = new List<AttendanceSummary> { new AttendanceSummary(Guid.NewGuid(), secondStudentLoad[0].Id, secondStudentLoad[0].FullName, false) };
            var secondAttendanceSummary2 = new List<AttendanceSummary> { new AttendanceSummary(Guid.NewGuid(), secondStudentLoad[1].Id, secondStudentLoad[1].FullName, false) };
            var secondLessonLoad = new List<LessonSummary>();
            secondLessonLoad.Add(new LessonSummary(Guid.NewGuid(), "New Lesson 1", DateOnly.FromDateTime(DateTime.Today), true, 60, 30, true, 5, true, 5, null, secondAttendanceSummary1));
            secondLessonLoad.Add(new LessonSummary(Guid.NewGuid(), "New Lesson 2", DateOnly.FromDateTime(DateTime.Today), true, 60, 30, true, 5, true, 5, null, secondAttendanceSummary2));
            _mockStudentRepo.SetupSequence(r => r.GetStudentOptionsAsync())
             .ReturnsAsync(firstStudentLoad)
             .ReturnsAsync(secondStudentLoad);

            _mockServiceRepo.SetupSequence(r => r.GetServicesAsync())
             .ReturnsAsync(firstServiceLoad)
             .ReturnsAsync(secondServiceLoad);

            _mockLessonRepo.SetupSequence(r => r.GetLessonsAsync(false, 1))
             .ReturnsAsync(firstLessonLoad)
             .ReturnsAsync(secondLessonLoad);

            await _viewModel.LoadAsync(); // test that Specifications.Clear() is working
            await _viewModel.LoadAsync(); // If LoadAsync is called twice, you should not have duplicate items in your list

            // 1. Verify repository was called with correct data
            _mockStudentRepo.Verify(r => r.GetStudentOptionsAsync(), Times.Exactly(2));
            _mockServiceRepo.Verify(r => r.GetServicesAsync(), Times.Exactly(2));
            _mockLessonRepo.Verify(r => r.GetLessonsAsync(false, 1), Times.Exactly(2));

            // 2. Verify the UI collection was updated correctly
            VerifyAction(null, InfoBarType.Success, isOpen: false, lessonCount: 2, studentsCount: 2, servicesCount: 2);
            var addedStudent = _viewModel.Students.First();
            var addedService = _viewModel.Services.First();
            var addedLesson = _viewModel.Lessons.First();
            Assert.AreEqual("New Man", addedStudent.FullName);
            Assert.AreEqual("New Service", addedService.Name);
            Assert.AreEqual("New Lesson 1", addedLesson.Name);
        }

        [TestMethod]
        public async Task LoadAsync_EmptyRepository_ResultingCollectionIsEmpty()
        {
            _mockStudentRepo.SetupSequence(r => r.GetStudentOptionsAsync())
                .ReturnsAsync(new List<StudentOption>());
            _mockServiceRepo.SetupSequence(r => r.GetServicesAsync())
                .ReturnsAsync(new List<ServiceSummary>());
            _mockLessonRepo.SetupSequence(r => r.GetLessonsAsync(false, 1))
                .ReturnsAsync(new List<LessonSummary>());


            await _viewModel.LoadAsync();

            _mockStudentRepo.Verify(r => r.GetStudentOptionsAsync(), Times.Once);
            _mockServiceRepo.Verify(r => r.GetServicesAsync(), Times.Once);
            _mockLessonRepo.Verify(r => r.GetLessonsAsync(false, 1), Times.Once);

            VerifyAction(null, InfoBarType.Success, isOpen: false, lessonCount: 0, studentsCount: 0, servicesCount: 0);
        }

        [TestMethod]
        public async Task LoadAsync_UpdateFilter()
        {
            _mockStudentRepo.SetupSequence(r => r.GetStudentOptionsAsync())
                .ReturnsAsync(new List<StudentOption>())
                .ReturnsAsync(new List<StudentOption>());
            _mockServiceRepo.SetupSequence(r => r.GetServicesAsync())
                .ReturnsAsync(new List<ServiceSummary>())
                .ReturnsAsync(new List<ServiceSummary>());
            _mockLessonRepo.SetupSequence(r => r.GetLessonsAsync(It.IsAny<bool>(), It.IsAny<int>()))
                .ReturnsAsync(new List<LessonSummary>())
                .ReturnsAsync(new List<LessonSummary>());


            _viewModel.ShownOnlyUnpaid = true;
            _viewModel.ShowLastNMonths = 3;

            _mockStudentRepo.Verify(r => r.GetStudentOptionsAsync(), Times.Exactly(2));
            _mockServiceRepo.Verify(r => r.GetServicesAsync(), Times.Exactly(2));
            _mockLessonRepo.Verify(r => r.GetLessonsAsync(true, 1), Times.Once);
            _mockLessonRepo.Verify(r => r.GetLessonsAsync(true, 3), Times.Once);

            VerifyAction(null, InfoBarType.Success, isOpen: false, lessonCount: 0, studentsCount: 0, servicesCount: 0);
        }

        // Validate student IDS

        [TestMethod]
        public void ValidateStudents_DuplicateStudentIds()
        {
            var studentId = Guid.NewGuid();
            var studentIds = new List<Guid> { studentId, studentId };
            var exception = Assert.Throws<InvalidDataException>(() => _viewModel.ValidateStudentIds(studentIds));
            Assert.AreEqual("Duplicate student IDs found in the attendance list.", exception.Message);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        [TestMethod]
        public void ValidateStudents_InvalidStudentIds()
        {
            var studentIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
            var exception = Assert.Throws<InvalidDataException>(() => _viewModel.ValidateStudentIds(studentIds));
            Assert.AreEqual("One or more student IDs in the attendance list do not exist.", exception.Message);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
            Assert.IsFalse(_viewModel.OpenInfoBar);
        }

        [TestMethod]
        public void ValidateStudents_EmptyStudentIds()
        {
            var studentIds = new List<Guid>();
            var result = _viewModel.ValidateStudentIds(studentIds);
            Assert.IsFalse(result);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.AreEqual("No student IDs provided.", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Warning, _viewModel.InfoBarType);
        }

        [TestMethod]
        public void ValidateStudents()
        {
            var studentId = Guid.NewGuid();
            var studentIds = new List<Guid> { studentId };
            _viewModel.Students.Add(new StudentOption(studentId, "Old Student"));
            _viewModel.Students.Add(new StudentOption(Guid.NewGuid(), "New Student"));
            var result = _viewModel.ValidateStudentIds(studentIds);
            Assert.IsTrue(result);
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
            var result = _viewModel.ValidateLessonInput(ref invalidName, ref duration, isPricePerHour: true, pricePerAttendance: 30);
            Assert.IsFalse(result);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.AreEqual("Lesson name is required.", _viewModel.InfoMessage);
            Assert.IsTrue(_viewModel.OpenInfoBar);
            Assert.AreEqual(InfoBarType.Warning, _viewModel.InfoBarType);
        }

        [TestMethod]
        public void ValidateLesson_InvalidLessonDurationRequired()
        {
            var name = "Lesson";
            int? duration = null;
            var result = _viewModel.ValidateLessonInput(ref name, ref duration, isPricePerHour: true, pricePerAttendance: 30);
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
            int? duration = -30;
            var result = _viewModel.ValidateLessonInput(ref name, ref duration, isPricePerHour: true, pricePerAttendance: 30);
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
            var name = "Lesson";
            var result = _viewModel.ValidateLessonInput(ref name, ref duration, isPricePerHour: true, pricePerAttendance: -30);
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
            var name = "Lesson";
            var result = _viewModel.ValidateLessonInput(ref name, ref duration, isPricePerHour: false, pricePerAttendance: 30);
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
            var attendanceSummary = new AttendanceSummary(Guid.NewGuid(), student.Id, student.FullName, false);
            var lesson = new LessonSummary(Guid.NewGuid(), "Old Lesson 1", DateOnly.FromDateTime(DateTime.Today), false, null, 30, false, 5, false, 5, null, [attendanceSummary]);
            _viewModel.Lessons.Add(lesson);
            var result = _viewModel.GetLesson(lesson.Id);
            Assert.AreEqual(lesson.Name, result.lesson.Name);
            Assert.AreEqual(0, result.index);
            Assert.IsFalse(_viewModel.IsBusy);
            Assert.IsNull(_viewModel.InfoMessage);
        }

        // Add lesson
        private void ArrangeForAddLessonTests()
        {
            var student = new StudentOption(Guid.NewGuid(), "Old Man");
            var service = new ServiceSummary(Guid.NewGuid(), "Old Service", false, 30);
            _viewModel.Students.Add(student);
            _viewModel.Services.Add(service);
        }

        private async Task ActForAddLessonTests(Guid? studentId, ServiceSummary service, Guid lessonId,
            bool invalidLesson = false, bool dbError = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            var name = invalidLesson ? "" : "Lesson";
            string notes = "Some notes";
            List<Guid> ids = studentId is null ? [] : [studentId.Value];
            Lesson result = new Lesson()
            {
                Id = lessonId,
                Date = date,
                Name = name,
                IsPricePerHour = service.IsPricePerHour,
                DurationMinutes = null,
                PricePerAttendance = 30,
                IsOnline = true,
                TravelAllowance = 10,
                IsWeekenOrHoliday = false,
                WeekendFee = 20,
                Notes = notes
            };
            if (studentId is not null)
            {
                result.Attendances.Add(new Attendance()
                { LessonId = lessonId, StudentId = studentId.Value, IsPaid = false, Price = 30 });
            }

            //Mock
            if (dbError)
            {
                _mockLessonRepo.Setup(r => r.AddLessonAsync(
                    date, name, service.IsPricePerHour, null, 30, true, 10, false, 20, notes, ids))
                .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            else
            {
                _mockLessonRepo.Setup(r => r.AddLessonAsync(
                    date, name, service.IsPricePerHour, null, 30, true, 10, false, 20, notes, ids))
                 .ReturnsAsync(result);
            }
            // Act
            await _viewModel.AddLessonAsync(date, name, service,
                    60, 30, true, false, notes, ids);
        }

        private void AssertForAddLessonTests(Guid? studentId, Guid lessonId, bool success, 
            string? infoMessage, InfoBarType severity,  bool isBusy = false, bool dbError = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            string notes = "Some notes";
            List<Guid> ids = studentId is null ? [] : [studentId.Value];
            Guid? lessonID = _viewModel.Lessons.Count > 0 ? _viewModel.Lessons[0].Id : null;

            if (success || dbError)
            {
                _mockLessonRepo.Verify(r => r.AddLessonAsync(date, "Lesson",
                    false, null, 30,
                    true, 10, false, 20,
                    notes, ids), Times.Once);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.AddLessonAsync(It.IsAny<DateOnly>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<decimal>(),
                    It.IsAny<bool>(), It.IsAny<decimal>(), It.IsAny<bool>(), It.IsAny<decimal>(),
                    It.IsAny<string?>(), It.IsAny<IReadOnlyList<Guid>>()), Times.Never);
            }
            VerifyAction(infoMessage, severity, isOpen: true, 
                studentsCount: 1, servicesCount: 1, lessonCount: success ? 1 : 0, isBusy: isBusy);

            if (success)
            {
                var addedLesson = _viewModel.Lessons.First();
                Assert.AreEqual(lessonID, addedLesson.Id);
                Assert.AreEqual("Lesson", addedLesson.Name);
                Assert.AreEqual(date, addedLesson.Date);
                Assert.IsFalse(addedLesson.IsPricePerHour);
                Assert.IsNull(addedLesson.DurationMinutes);
                Assert.AreEqual(30, addedLesson.PricePerAttendance);
                Assert.IsTrue(addedLesson.IsOnline);
                Assert.AreEqual(10, addedLesson.TravelAllowance);
                Assert.IsFalse(addedLesson.IsWeekenOrHoliday);
                Assert.AreEqual(20, addedLesson.WeekendFee);
                Assert.AreEqual(notes, addedLesson.Notes);
                Assert.HasCount(studentId is null ? 0 : 1, addedLesson.Attendances);
            }
        }

        [TestMethod]
        public async Task AddLesson_WhenAlreadyBusy()
        {
            var lessonId = Guid.NewGuid();
            ArrangeForAddLessonTests();
            _viewModel.IsBusy = true;
            await ActForAddLessonTests(_viewModel.Students[0].Id, _viewModel.Services[0], lessonId);
            AssertForAddLessonTests(_viewModel.Students[0].Id, lessonId, success: false, 
                infoMessage: "Can't add lesson while busy.", severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task AddLesson_InvalidStudent()
        {
            var lessonId = Guid.NewGuid();
            ArrangeForAddLessonTests();
            await ActForAddLessonTests(null, _viewModel.Services[0], lessonId);
            AssertForAddLessonTests(null, lessonId, success: false, infoMessage: "No student IDs provided.",
                severity: InfoBarType.Warning);
        }

        [TestMethod]
        public async Task AddLesson_InvalidName()
        {
            var lessonId = Guid.NewGuid();
            ArrangeForAddLessonTests();
            await ActForAddLessonTests(_viewModel.Students[0].Id, _viewModel.Services[0], lessonId, invalidLesson: true);
            AssertForAddLessonTests(_viewModel.Students[0].Id, lessonId, success: false, infoMessage: "Lesson name is required.",
                severity: InfoBarType.Warning);
        }
        [TestMethod]
        public async Task AddLesson_DBException()
        {
            var lessonId = Guid.NewGuid();
            ArrangeForAddLessonTests();
            await ActForAddLessonTests(_viewModel.Students[0].Id, _viewModel.Services[0], lessonId, dbError: true);
            AssertForAddLessonTests(_viewModel.Students[0].Id, lessonId, success: false, dbError: true,
                infoMessage: "Constraint failed.", severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task AddLesson()
        {
            var lessonId = Guid.NewGuid();
            ArrangeForAddLessonTests();
            await ActForAddLessonTests(_viewModel.Students[0].Id, _viewModel.Services[0], lessonId);
            AssertForAddLessonTests(_viewModel.Students[0].Id, lessonId, success: true, 
                infoMessage: "Lesson 'Lesson' added successfully.", severity: InfoBarType.Success);
        }

        // Update lesson

        private void ArrangeForUpdateLessonTests(Guid lessonId, Guid attendanceId, Guid studentId)
        {
            var student = new StudentOption(studentId, "Student");
            var service = new ServiceSummary(Guid.NewGuid(), "Service", false, 30);
            var attendance1 = new AttendanceSummary(attendanceId, studentId, "Student", false);
            var attendance2 = new AttendanceSummary(Guid.NewGuid(), studentId, "Student", false);
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            var originalLesson = new LessonSummary
            (lessonId, "Old lesson", date, false, 60, 25, false, 5, false, 10, null, [attendance1]);
            var otherLesson = new LessonSummary
            (Guid.NewGuid(), "Other lesson", date, false, 60, 25, false, 5, false, 10, null, [attendance2]);
            _viewModel.Lessons.Add(otherLesson);
            _viewModel.Lessons.Add(originalLesson);
            _viewModel.Students.Add(student);
            _viewModel.Services.Add(service);
        }

        private async Task ActForUpdateLessonTests(Guid lessonId, Guid attendanceId, Guid studentId,
            bool invalidLesson = false, bool dbError = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1999, 8, 17));
            var name = invalidLesson ? "" : "Lesson";
            bool isPricePerHour = false;
            int? duration = null;
            decimal pricePerAttendance = 30;
            bool isOnline = true;
            decimal travelAllowance = 10;
            bool isWeekendOrHoliday = true;
            decimal weekendFee = 20;
            string notes = "Some notes";
            Lesson result = new Lesson()
            {
                Id = lessonId,
                Date = date,
                Name = name,
                IsPricePerHour = isPricePerHour,
                DurationMinutes = duration,
                PricePerAttendance = pricePerAttendance,
                IsOnline = isOnline,
                TravelAllowance = travelAllowance,
                IsWeekenOrHoliday = isWeekendOrHoliday,
                WeekendFee = weekendFee,
                Notes = notes
            };
            result.Attendances.Add(new Attendance()
            { Id = attendanceId, LessonId = lessonId, StudentId = studentId, IsPaid = false, Price = 30 });

            //Mock
            if (dbError)
            {
                _mockLessonRepo.Setup(r => r.UpdateLesson(lessonId, date, name, isPricePerHour, duration, pricePerAttendance,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, notes))
                .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            else
            {
                _mockLessonRepo.Setup(r => r.UpdateLesson(lessonId, date, name, isPricePerHour, duration, pricePerAttendance,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, notes))
                 .ReturnsAsync(result);
            }
            // Act
            await _viewModel.UpdateLessonAsync(lessonId, date, name, isPricePerHour, duration, pricePerAttendance, isOnline,
                travelAllowance, isWeekendOrHoliday, weekendFee, notes);
        }

        private void AssertForUpdateLessonTests(Guid lessonId, Guid attendanceId, Guid studentId, bool success, 
            string? infoMessage, InfoBarType severity, bool isBusy = false, bool dbError = false)
        {
            var date = DateOnly.FromDateTime(new DateTime(1999, 8, 17));
            var name = "Lesson";
            bool isPricePerHour = false;
            int? duration = null;
            decimal pricePerAttendance = 30;
            bool isOnline = true;
            decimal travelAllowance = 10;
            bool isWeekendOrHoliday = true;
            decimal weekendFee = 20;
            string notes = "Some notes";

            if (success || dbError)
            {
                _mockLessonRepo.Verify(r => r.UpdateLesson(lessonId, date, name, isPricePerHour, duration, pricePerAttendance,
                    isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, notes), Times.Once);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.UpdateLesson(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<string>(),
                    It.IsAny<bool>(), It.IsAny<int?>(), It.IsAny<decimal>(),
                    It.IsAny<bool>(), It.IsAny<decimal>(), It.IsAny<bool>(), It.IsAny<decimal>(),
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
                Assert.AreEqual(30, addedLesson.PricePerAttendance);
                Assert.IsTrue(addedLesson.IsOnline);
                Assert.AreEqual(10, addedLesson.TravelAllowance);
                Assert.IsTrue(addedLesson.IsWeekenOrHoliday);
                Assert.AreEqual(20, addedLesson.WeekendFee);
                Assert.AreEqual(notes, addedLesson.Notes);
                Assert.HasCount(1, addedLesson.Attendances);
            }
        }

        [TestMethod]
        public async Task UpdateLessonAsync_WhenAlreadyBusy()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForUpdateLessonTests(lessonId, attendanceId, studentId);
            _viewModel.IsBusy = true;
            await ActForUpdateLessonTests(lessonId, attendanceId, studentId);
            AssertForUpdateLessonTests(lessonId, attendanceId, studentId, success: false, 
                infoMessage: "Can't update lesson while busy.", severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task UpdateLessonAsync_InvalidLesson()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForUpdateLessonTests(lessonId, attendanceId, studentId);
            await ActForUpdateLessonTests(lessonId, attendanceId, studentId, invalidLesson: true);
            AssertForUpdateLessonTests(lessonId, attendanceId, studentId, success: false, 
                infoMessage: "Lesson name is required.", severity: InfoBarType.Warning);
        }

        [TestMethod]
        public async Task UpdateLessonAsync_DBError()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForUpdateLessonTests(lessonId, attendanceId, studentId);
            await ActForUpdateLessonTests(lessonId, attendanceId, studentId, dbError: true);
            AssertForUpdateLessonTests(lessonId, attendanceId, studentId, success: false, dbError: true, 
                infoMessage: "Constraint failed.", severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task UpdateLessonAsync()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForUpdateLessonTests(lessonId, attendanceId, studentId);
            await ActForUpdateLessonTests(lessonId, attendanceId, studentId);
            AssertForUpdateLessonTests(lessonId, attendanceId, studentId, success: true, 
                infoMessage: "Lesson 'Lesson' updated successfully.", severity: InfoBarType.Success);
        }

        // Update lesson note

        private async Task ActForUpdateLessonNotesTests(Guid lessonId,
            bool invalidLesson = false, bool dbError = false, bool emptyNote = false)
        {
            var lesson = _viewModel.Lessons.First(l => l.Id == lessonId);
            string? notes = emptyNote ? null : "Some notes";
            Lesson result = new Lesson()
            {
                Id = lessonId,
                Date = lesson.Date,
                Name = lesson.Name,
                IsPricePerHour = lesson.IsPricePerHour,
                DurationMinutes = lesson.DurationMinutes,
                PricePerAttendance = lesson.PricePerAttendance,
                IsOnline = lesson.IsOnline,
                TravelAllowance = lesson.TravelAllowance,
                IsWeekenOrHoliday = lesson.IsWeekenOrHoliday,
                WeekendFee = lesson.WeekendFee,
                Notes = notes
            };
            var attendance = lesson.Attendances.First();
            result.Attendances.Add(new Attendance()
            { Id = attendance.Id, LessonId = lessonId, StudentId = attendance.StudentId, IsPaid = attendance.IsPaid });

            //Mock
            if (dbError)
            {
                _mockLessonRepo.Setup(r => r.UpdateLessonNoteAsync(lessonId, notes))
                .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            else
            {
                _mockLessonRepo.Setup(r => r.UpdateLessonNoteAsync(lessonId, notes))
                 .ReturnsAsync(result);
            }
            // Act
            await _viewModel.UpdateLessonNoteAsync(lessonId, notes);
        }

        private void AssertForUpdateLessonNotesTests(Guid lessonId, bool success, string? infoMessage, InfoBarType severity,
            bool isBusy = false, bool dbError = false, bool emptyNote = false)
        {
            var oldLesson = _viewModel.Lessons.First(l => l.Id == lessonId);
            string? notes = emptyNote ? null : "Some notes";

            if (success || dbError)
            {
                _mockLessonRepo.Verify(r => r.UpdateLessonNoteAsync(lessonId, notes), Times.Once);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.UpdateLessonNoteAsync(It.IsAny<Guid>(), It.IsAny<string?>()), Times.Never);
            }
            VerifyAction(infoMessage, severity, isOpen: true, studentsCount: 1, servicesCount: 1, lessonCount: 2, isBusy: isBusy);

            if (success)
            {
                var addedLesson = _viewModel.Lessons.First(l => l.Id == lessonId);
                Assert.AreEqual("Old lesson", addedLesson.Name);
                Assert.AreEqual(notes, addedLesson.Notes);
                Assert.HasCount(1, addedLesson.Attendances);
            }
        }

        [TestMethod]
        public async Task UpdateLessonNote_IsBusy()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForUpdateLessonTests(lessonId, attendanceId, studentId);
            _viewModel.IsBusy = true;
            await ActForUpdateLessonNotesTests(lessonId);
            AssertForUpdateLessonNotesTests(lessonId, success: false, infoMessage: "Can't update lesson note while busy.",
                severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task UpdateLessonNote_DBError()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForUpdateLessonTests(lessonId, attendanceId, studentId);
            await ActForUpdateLessonNotesTests(lessonId, dbError: true);
            AssertForUpdateLessonNotesTests(lessonId, success: false, dbError: true, infoMessage: "Constraint failed.",
                severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task UpdateLessonNote_EmptyNote()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForUpdateLessonTests(lessonId, attendanceId, studentId);
            await ActForUpdateLessonNotesTests(lessonId, emptyNote: true);
            AssertForUpdateLessonNotesTests(lessonId, success: true, emptyNote: true, 
                infoMessage: "Lesson 'Old lesson' note updated successfully.", severity: InfoBarType.Success);
        }

        [TestMethod]
        public async Task UpdateLessonNote()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForUpdateLessonTests(lessonId, attendanceId, studentId);
            await ActForUpdateLessonNotesTests(lessonId);
            AssertForUpdateLessonNotesTests(lessonId, success: true, 
                infoMessage: "Lesson 'Old lesson' note updated successfully.", severity: InfoBarType.Success);
        }

        // Add attendance to lesson

        private void ArrangeForAddAttendanceTests(Guid lessonId, Guid? studentId)
        {
            var student1 = new StudentOption(Guid.NewGuid(), "Student1");
            var student2 = new StudentOption(studentId ?? Guid.NewGuid(), "Student2");
            var service = new ServiceSummary(Guid.NewGuid(), "Service", false, 30);
            var attendance = new AttendanceSummary(Guid.NewGuid(), student1.Id, student1.FullName, false);
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            var lesson = new LessonSummary
            (lessonId, "Lesson", date, false, null, 25, false, 5, false, 10, null, [attendance]);
            _viewModel.Lessons.Add(lesson);
            _viewModel.Students.Add(student1);
            _viewModel.Students.Add(student2);
            _viewModel.Services.Add(service);
        }

        private async Task ActForAddAttendanceTests(Guid lessonId, Guid attendanceId, Guid? studentId, bool dbError = false)
        {
            var lesson = _viewModel.Lessons.First(l => l.Id == lessonId);
            Lesson result = new Lesson()
            {
                Id = lessonId,
                Date = lesson.Date,
                Name = lesson.Name,
                IsPricePerHour = lesson.IsPricePerHour,
                DurationMinutes = lesson.DurationMinutes,
                PricePerAttendance = lesson.PricePerAttendance,
                IsOnline = lesson.IsOnline,
                TravelAllowance = lesson.TravelAllowance,
                IsWeekenOrHoliday = lesson.IsWeekenOrHoliday,
                WeekendFee = lesson.WeekendFee,
                Notes = lesson.Notes,
            };
            List<Guid> ids = studentId is null ? [] : [studentId.Value];
            var attendance = lesson.Attendances.First();
            result.Attendances.Add(new Attendance()
            { Id = attendance.Id, LessonId = lessonId, StudentId = attendance.StudentId, IsPaid = attendance.IsPaid, Price = 30 });
            if (studentId is not null)
            {
                result.Attendances.Add(new Attendance()
                { Id = attendanceId, LessonId = lessonId, StudentId = studentId.Value, IsPaid = false, Price = 50 });
            }

            //Mock
            if (dbError)
            {
                _mockLessonRepo.Setup(r => r.AddAttendanceAsync(lessonId, ids))
                .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            else
            {
                _mockLessonRepo.Setup(r => r.AddAttendanceAsync(lessonId, ids))
                 .ReturnsAsync(result);
            }
            // Act
            await _viewModel.AddAttendanceAsync(lessonId, ids);
        }

        private void AssertForAddAttendanceTests(Guid lessonId, Guid attendanceId, Guid? studentId, bool success, 
            string? infoMessage, InfoBarType severity, bool isBusy = false, bool dbError = false)
        {
            List<Guid> ids = studentId is null ? [] : [studentId.Value];
            if (success || dbError)
            {
                _mockLessonRepo.Verify(r => r.AddAttendanceAsync(lessonId, ids), Times.Once);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.AddAttendanceAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>()), Times.Never);
            }
            VerifyAction(infoMessage, severity, isOpen: true, studentsCount: 2, servicesCount: 1, lessonCount: 1, isBusy: isBusy);

            if (success)
            {
                var modifiedLesson = _viewModel.Lessons.First(l => l.Id == lessonId);
                var newAttendance = modifiedLesson.Attendances.First(a => a.Id == attendanceId);
                Assert.AreEqual("Lesson", modifiedLesson.Name);
                Assert.HasCount(2, modifiedLesson.Attendances);
                Assert.AreEqual("Student2", newAttendance.StudentName);
                Assert.IsFalse(newAttendance.IsPaid);
            }
        }

        [TestMethod]
        public async Task AddAttendanceAsync_IsBusy()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForAddAttendanceTests(lessonId, studentId);
            _viewModel.IsBusy = true;
            await ActForAddAttendanceTests(lessonId, attendanceId, studentId);
            AssertForAddAttendanceTests(lessonId, attendanceId, studentId, success: false, 
                infoMessage: "Can't add attendance while busy.", severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task AddAttendanceAsync_InvalidStudents()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            ArrangeForAddAttendanceTests(lessonId, null);
            await ActForAddAttendanceTests(lessonId, attendanceId, null);
            AssertForAddAttendanceTests(lessonId, attendanceId, null, success: false, infoMessage: "No student IDs provided.",
                severity: InfoBarType.Warning);
        }

        [TestMethod]
        public async Task AddAttendanceAsync_DBError()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForAddAttendanceTests(lessonId, studentId);
            await ActForAddAttendanceTests(lessonId, attendanceId, studentId, dbError: true);
            AssertForAddAttendanceTests(lessonId, attendanceId, studentId, success: false, dbError: true, 
                infoMessage: "Constraint failed.", severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task AddAttendanceAsync()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            ArrangeForAddAttendanceTests(lessonId, studentId);
            await ActForAddAttendanceTests(lessonId, attendanceId, studentId);
            AssertForAddAttendanceTests(lessonId, attendanceId, studentId, success: true, 
                infoMessage: "1 student(s) were added to Lesson 'Lesson' successfully.", severity: InfoBarType.Success);
        }

        // Remove attendance from lesson

        private void ArrangeForAttendanceTests(Guid lessonId, Guid attendanceId, bool additionalAttendance = true)
        {
            var student = new StudentOption(Guid.NewGuid(), "Student");
            var service = new ServiceSummary(Guid.NewGuid(), "Service", false, 30);
            var attendance1 = new AttendanceSummary(attendanceId, student.Id, student.FullName, false);
            var attendance2 = new AttendanceSummary(Guid.NewGuid(), student.Id, student.FullName, false);
            var date = DateOnly.FromDateTime(new DateTime(1993, 8, 17));
            var lesson = new LessonSummary
            (lessonId, "Lesson", date, false, null, 25, false, 5, false, 10, null,
            additionalAttendance ? [attendance1, attendance2] : [attendance1]);

            _viewModel.Lessons.Add(lesson);
            _viewModel.Students.Add(student);
            _viewModel.Services.Add(service);
        }

        private async Task ActForRemoveAttendanceTests(Guid lessonId, Guid attendanceId, bool dbError = false)
        {
            var lesson = _viewModel.Lessons.First(l => l.Id == lessonId);
            Lesson result = new Lesson()
            {
                Id = lessonId,
                Date = lesson.Date,
                Name = lesson.Name,
                IsPricePerHour = lesson.IsPricePerHour,
                DurationMinutes = lesson.DurationMinutes,
                PricePerAttendance = lesson.PricePerAttendance,
                IsOnline = lesson.IsOnline,
                TravelAllowance = lesson.TravelAllowance,
                IsWeekenOrHoliday = lesson.IsWeekenOrHoliday,
                WeekendFee = lesson.WeekendFee,
                Notes = lesson.Notes,
            };
            foreach (var attendance in lesson.Attendances)
            {
                if (attendance.Id == attendanceId)
                    continue;
                result.Attendances.Add(new Attendance()
                { Id = attendance.Id, LessonId = lessonId, StudentId = attendance.StudentId, IsPaid = attendance.IsPaid, Price = 30 });
            }
            //Mock
            if (dbError)
            {
                _mockLessonRepo.Setup(r => r.RemoveAttendanceAsync(lessonId, attendanceId))
                .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            else
            {
                _mockLessonRepo.Setup(r => r.RemoveAttendanceAsync(lessonId, attendanceId))
                 .ReturnsAsync(result);
            }
            // Act
            await _viewModel.RemoveAttendanceAsync(lessonId, attendanceId);
        }

        private void AssertForRemoveAttendanceTests(Guid lessonId, Guid attendanceId, bool success, string? infoMessage,
            InfoBarType severity, bool isBusy = false, bool dbError = false, bool additionalAttendance = true)
        {
            if (success || dbError)
            {
                _mockLessonRepo.Verify(r => r.RemoveAttendanceAsync(lessonId, attendanceId), Times.Once);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.RemoveAttendanceAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
            }
            VerifyAction(infoMessage, severity, isOpen: true, 
                studentsCount: 1, servicesCount: 1, lessonCount: additionalAttendance ? 1 : 0, isBusy: isBusy);

            if (success)
            {
                if (!additionalAttendance)
                    return; // Lesson should be removed, so no need to check attendances
                var modifiedLesson = _viewModel.Lessons.First(l => l.Id == lessonId);
                Assert.AreEqual("Lesson", modifiedLesson.Name);
                Assert.HasCount(additionalAttendance ? 1 : 0, modifiedLesson.Attendances);
                Assert.AreNotEqual(attendanceId, modifiedLesson.Attendances.First().Id);
            }
        }

        [TestMethod]
        public async Task RemoveAttendanceAsync_IsBusy()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            ArrangeForAttendanceTests(lessonId, attendanceId);
            _viewModel.IsBusy = true;
            await ActForRemoveAttendanceTests(lessonId, attendanceId);
            AssertForRemoveAttendanceTests(lessonId, attendanceId, success: false, 
                infoMessage: "Can't remove attendance while busy.", severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task RemoveAttendanceAsync_DBError()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            ArrangeForAttendanceTests(lessonId, attendanceId);
            await ActForRemoveAttendanceTests(lessonId, attendanceId, dbError: true);
            AssertForRemoveAttendanceTests(lessonId, attendanceId, success: false, dbError: true, 
                infoMessage: "Constraint failed.", severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task RemoveAttendanceAsync_RemoveLesson()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            ArrangeForAttendanceTests(lessonId, attendanceId, additionalAttendance: false);
            await ActForRemoveAttendanceTests(lessonId, attendanceId);
            AssertForRemoveAttendanceTests(lessonId, attendanceId, success: true, 
                infoMessage: "Lesson 'Lesson' was deleted after removing last attendant.", severity: InfoBarType.Success, additionalAttendance: false);
        }

        [TestMethod]
        public async Task RemoveAttendanceAsync()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            ArrangeForAttendanceTests(lessonId, attendanceId);
            await ActForRemoveAttendanceTests(lessonId, attendanceId);
            AssertForRemoveAttendanceTests(lessonId, attendanceId, success: true, 
                infoMessage: "Student was removed from lesson 'Lesson' successfully.", severity: InfoBarType.Success);
        }

        // Update attendance

        private async Task ActForUpdateAttendanceTests(Guid lessonId, Guid attendanceId, bool dbError = false)
        {
            var lesson = _viewModel.Lessons.First(l => l.Id == lessonId);
            Lesson result = new Lesson()
            {
                Id = lessonId,
                Date = lesson.Date,
                Name = lesson.Name,
                IsPricePerHour = lesson.IsPricePerHour,
                DurationMinutes = lesson.DurationMinutes,
                PricePerAttendance = lesson.PricePerAttendance,
                IsOnline = lesson.IsOnline,
                TravelAllowance = lesson.TravelAllowance,
                IsWeekenOrHoliday = lesson.IsWeekenOrHoliday,
                WeekendFee = lesson.WeekendFee,
                Notes = lesson.Notes,
            };
            foreach (var attendance in lesson.Attendances)
            {
                result.Attendances.Add(new Attendance()
                { Id = attendance.Id, LessonId = lessonId, StudentId = attendance.StudentId, IsPaid = attendance.IsPaid, Price = 30 });
            }
            result.Attendances.First(a => a.Id == attendanceId).IsPaid = true;

            //Mock
            if (dbError)
            {
                _mockLessonRepo.Setup(r => r.UpdateAttendanceAsync(lessonId, attendanceId, true))
                .ThrowsAsync(new DbUpdateException("Constraint failed."));
            }
            else
            {
                _mockLessonRepo.Setup(r => r.UpdateAttendanceAsync(lessonId, attendanceId, true))
                 .ReturnsAsync(result);
            }
            // Act
            await _viewModel.UpdateAttendanceAsync(lessonId, attendanceId, true);
        }

        private void AssertForUpdateAttendanceTests(Guid lessonId, Guid attendanceId, bool success, string? infoMessage,
            InfoBarType severity, bool isBusy = false, bool dbError = false)
        {
            if (success || dbError)
            {
                _mockLessonRepo.Verify(r => r.UpdateAttendanceAsync(lessonId, attendanceId, true), Times.Once);
            }
            else
            {
                _mockLessonRepo.Verify(r => r.UpdateAttendanceAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>()), Times.Never);
            }
            VerifyAction(infoMessage, severity, isOpen: true, studentsCount: 1, servicesCount: 1, lessonCount: 1, isBusy: isBusy);
            var modifiedLesson = _viewModel.Lessons.First();
            Assert.AreEqual("Lesson", modifiedLesson.Name);
            Assert.HasCount(2, modifiedLesson.Attendances);

            if (success)
            {
                Assert.IsTrue(modifiedLesson.Attendances.First(a => a.Id == attendanceId).IsPaid);
            }
        }

        [TestMethod]
        public async Task UpdateAttendanceAsync_IsBusy()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            ArrangeForAttendanceTests(lessonId, attendanceId);
            _viewModel.IsBusy = true;
            await ActForUpdateAttendanceTests(lessonId, attendanceId);
            AssertForUpdateAttendanceTests(lessonId, attendanceId, success: false, 
                infoMessage: "Can't update attendance while busy.", severity: InfoBarType.Warning, isBusy: true);
        }

        [TestMethod]
        public async Task UpdateAttendanceAsync_DBError()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            ArrangeForAttendanceTests(lessonId, attendanceId);
            await ActForUpdateAttendanceTests(lessonId, attendanceId, dbError: true);
            AssertForUpdateAttendanceTests(lessonId, attendanceId, success: false, dbError: true, 
                infoMessage: "Constraint failed.", severity: InfoBarType.Error);
        }

        [TestMethod]
        public async Task UpdateAttendanceAsync()
        {
            var lessonId = Guid.NewGuid();
            var attendanceId = Guid.NewGuid();
            ArrangeForAttendanceTests(lessonId, attendanceId);
            await ActForUpdateAttendanceTests(lessonId, attendanceId);
            AssertForUpdateAttendanceTests(lessonId, attendanceId, success: true, 
                infoMessage: "Lesson 'Lesson' attendant was updated successfully.", severity: InfoBarType.Success);
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
    }
}
