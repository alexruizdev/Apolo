using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyModel;
using Models;
using Repository;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Apolo.Tests.Data
{
    [TestClass]
    public class LessonRepositoryTests : RepositoryTests
    {
        private LessonRepository _repository = null!;

        [TestInitialize]
        public override void Setup()
        {
            base.Setup();
            _repository = new LessonRepository(_context);
        }

        // --- GET TESTS (Filtering & Projections) ---

        [TestMethod]
        public async Task GetLessonsAsync_FilterByUnpaid_ReturnsCorrectLessons()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson1 = TestGenerator.CreateLesson(student.Id, paid: true);
            var lesson2 = TestGenerator.CreateLesson(student.Id, paid: false);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.AddRange(lesson1, lesson2);
            await _context.SaveChangesAsync();

            // Act
            var results = (await _repository.GetLessonsAsync(showOnlyUnpaid: true, months: null)).ToList();

            // Assert
            Assert.HasCount(1, results);
            Assert.AreEqual(TestGenerator.LessonNameUnpaid, results[0].Name);
            Assert.IsFalse(results[0].IsPricePerHour);
            Assert.AreEqual(TestGenerator.LongDuration, results[0].DurationMinutes);
            Assert.AreEqual(TestGenerator.LessonPricePerAttendance, results[0].PricePerAttendance);
            Assert.IsTrue(results[0].IsOnline);
            Assert.AreEqual(TestGenerator.LessonTravelAllowance, results[0].TravelAllowance);
            Assert.IsFalse(results[0].IsWeekenOrHoliday);
            Assert.AreEqual(TestGenerator.LessonWeekendFee, results[0].WeekendFee);
            Assert.IsNull(results[0].Notes);
            Assert.HasCount(1, results[0].Attendances);
            Assert.IsFalse(results[0].Attendances[0].IsPaid);
        }

        [TestMethod]
        public async Task GetLessonsAsync_FilterByMonths_FiltersOldData()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson1 = TestGenerator.CreateLesson(student.Id, paid: true);
            var lesson2 = TestGenerator.CreateLesson(student.Id, paid: false);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.AddRange(lesson1, lesson2);
            await _context.SaveChangesAsync();

            // Act (Filter for last 3 months)
            var results = (await _repository.GetLessonsAsync(showOnlyUnpaid: false, months: 3)).ToList();

            // Assert
            Assert.HasCount(1, results);
            Assert.AreEqual(TestGenerator.LessonNameUnpaid, results[0].Name);
        }

        [TestMethod]
        public async Task AddLessonsAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson1 = TestGenerator.CreateLesson(student.Id, paid: true);
            var lesson2 = TestGenerator.CreateLesson(student.Id, paid: false);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act (Filter for last 3 months)
            await _repository.AddLessonsAsync([ lesson1, lesson2]);

            // Assert
            Assert.HasCount(2, _context.Lessons);
        }

        // --- CREATE TESTS ---

        [TestMethod]
        public async Task AddLessonAsync_CreatesLessonAndAttendances()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            _context.Payers.Add(payer);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act
            var lesson = await _repository.AddLessonAsync(
                DateOnly.FromDateTime(DateTime.Now), "Guitar Class",
                false, 60, 25.0m, false, 0, false, 0, "Note", [student.Id]);

            // Assert
            Assert.AreEqual("Guitar Class", lesson.Name);
            Assert.IsFalse(lesson.IsPricePerHour);
            Assert.AreEqual(60, lesson.DurationMinutes);
            Assert.AreEqual(25.0m, lesson.PricePerAttendance);
            Assert.IsFalse(lesson.IsOnline);
            Assert.AreEqual(0, lesson.TravelAllowance);
            Assert.IsFalse(lesson.IsWeekenOrHoliday);
            Assert.AreEqual(0, lesson.WeekendFee);
            Assert.AreEqual("Note", lesson.Notes);
            var dbLesson = await _context.Lessons.Include(l => l.Attendaces).FirstAsync(l => l.Id == lesson.Id);
            Assert.HasCount(1, dbLesson.Attendaces);
            Assert.AreEqual(student.Id, dbLesson.Attendaces.First().StudentId);
        }

        // --- UPDATE TESTS ---

        [TestMethod]
        public async Task UpdateLessonNoteAsync_InvalidId_ThrowsException()
        {
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.UpdateLessonNoteAsync(Guid.NewGuid(), "New Note");
            });
        }

        [TestMethod]
        public async Task UpdateLessonNoteAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson = TestGenerator.CreateLesson(student.Id, paid: true);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            var updatedLesson = await _repository.UpdateLessonNoteAsync(lesson.Id, "Updated Note");
            Assert.AreEqual("Updated Note", updatedLesson.Notes);
            var dbLesson = await _context.Lessons.FirstAsync(l => l.Id == lesson.Id);
            Assert.AreEqual("Updated Note", dbLesson.Notes);
        }

        [TestMethod]
        public async Task UpdateLessonAsync_InvalidId_ThrowsException()
        {
            // Arrange
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.UpdateLesson(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Now), "Guitar Class",
                false, 60, 25.0m, 
                false, 0, false, 0, "Note");
            });
        }

        [TestMethod]
        public async Task UpdateLessonAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson = TestGenerator.CreateLesson(student.Id, paid: true);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            var updatedLesson = await _repository.UpdateLesson(lesson.Id, TestGenerator.LessonNewDate, "Guitar Class",
                false, 60, 25.0m, 
                false, 0, false, 0, "Updated note");
            Assert.AreEqual("Guitar Class", updatedLesson.Name);
            Assert.IsFalse(updatedLesson.IsPricePerHour);
            Assert.AreEqual("Updated note", updatedLesson.Notes);
            Assert.AreEqual(60, updatedLesson.DurationMinutes);
            Assert.AreEqual(25.0m, updatedLesson.PricePerAttendance);
            Assert.IsFalse(updatedLesson.IsOnline);
            Assert.AreEqual(0, updatedLesson.TravelAllowance);
            Assert.IsFalse(updatedLesson.IsWeekenOrHoliday);
            Assert.AreEqual(0, updatedLesson.WeekendFee);
            var dbLesson = await _context.Lessons.FirstAsync(l => l.Id == lesson.Id);
            Assert.AreEqual("Guitar Class", dbLesson.Name);
            Assert.IsFalse(dbLesson.IsPricePerHour);
            Assert.AreEqual("Updated note", dbLesson.Notes);
            Assert.AreEqual(60, dbLesson.DurationMinutes);
            Assert.AreEqual(25.0m, dbLesson.PricePerAttendance);
            Assert.IsFalse(dbLesson.IsOnline);
            Assert.AreEqual(0, dbLesson.TravelAllowance);
            Assert.IsFalse(dbLesson.IsWeekenOrHoliday);
            Assert.AreEqual(0, dbLesson.WeekendFee);
        }

        // --- ATTENDANCE MANAGEMENT TESTS ---

        [TestMethod]
        public async Task AddAttendanceAsync_SkipsExistingStudents_ThrowsIfNoneAdded()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson = TestGenerator.CreateLesson(student.Id, paid: true);
            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            // Act & Assert (Try adding the same student again)
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await _repository.AddAttendanceAsync(lesson.Id, [student.Id]);
            });
        }

        [TestMethod]
        public async Task AddAttendanceAsync_NullLesson_InvalidDataException()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            _context.Payers.Add(payer);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act & Assert (Try adding the same student again)
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.AddAttendanceAsync(new Guid(), [student.Id]);
            });
        }

        [TestMethod]
        public async Task AddAttendanceAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student1 = TestGenerator.CreateStudent1(payer.Id);
            var student2 = TestGenerator.CreateStudent2(payer.Id);
            var lesson = TestGenerator.CreateLesson(student1.Id, paid: true);
            _context.Payers.Add(payer);
            _context.Students.Add(student1);
            _context.Students.Add(student2);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            // Act & Assert (Try adding the same student again)
            await _repository.AddAttendanceAsync(lesson.Id, [student2.Id]);

            var result = _context.Attendances.Where(a => a.LessonId == lesson.Id).ToList();
            Assert.HasCount(2, result);
            Assert.IsTrue(result.First(a => a.StudentId == student1.Id).IsPaid);
            Assert.IsFalse(result.First(a => a.StudentId == student2.Id).IsPaid);
        }

        [TestMethod]
        public async Task RemoveAttendanceAsync_RemovesLessonIfLastStudent()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student1 = TestGenerator.CreateStudent1(payer.Id);
            var lesson = TestGenerator.CreateLesson(student1.Id, paid: true);
            _context.Payers.Add(payer);
            _context.Students.Add(student1);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            // Act
            await _repository.RemoveAttendanceAsync(lesson.Id, lesson.Attendaces.First().Id);

            // Assert
            Assert.HasCount(0, _context.Lessons);
        }

        [TestMethod]
        public async Task RemoveAttendanceAsync_InvalidLesson()
        {
            // Act
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.RemoveAttendanceAsync(new Guid(), new Guid());
            });
        }

        [TestMethod]
        public async Task RemoveAttendanceAsync_InvalidAttendance()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student1 = TestGenerator.CreateStudent1(payer.Id);
            var lesson = TestGenerator.CreateLesson(student1.Id, paid: true);
            _context.Payers.Add(payer);
            _context.Students.Add(student1);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            // Act
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.RemoveAttendanceAsync(lesson.Id, new Guid());
            });
        }

        [TestMethod]
        public async Task UpdateAttendanceAsync_UpdatesIsPaidStatus()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student1 = TestGenerator.CreateStudent1(payer.Id);
            var lesson = TestGenerator.CreateLesson(student1.Id, paid: false);
            _context.Payers.Add(payer);
            _context.Students.Add(student1);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            // Act
            await _repository.UpdateAttendanceAsync(lesson.Id, lesson.Attendaces.First().Id, true);

            // Assert
            var updated = await _context.Attendances.FindAsync(lesson.Attendaces.First().Id);
            Assert.IsTrue(updated!.IsPaid);
        }
    }
}
