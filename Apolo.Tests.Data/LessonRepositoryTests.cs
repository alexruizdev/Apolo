using Microsoft.EntityFrameworkCore;
using Repository;

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
            Assert.IsNull(results[0].DurationMinutes);
            Assert.AreEqual(TestGenerator.BasePrice, results[0].BasePrice);
            Assert.IsTrue(results[0].IsOnline);
            Assert.AreEqual(TestGenerator.LessonTravelAllowance, results[0].TravelAllowance);
            Assert.IsFalse(results[0].IsWeekenOrHoliday);
            Assert.AreEqual(TestGenerator.LessonWeekendFee, results[0].WeekendFee);
            Assert.IsNull(results[0].Notes);
            Assert.IsFalse(results[0].IsPaid);
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
        public async Task AddLessonAsync_CreatesLessonAndLessons()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            _context.Payers.Add(payer);
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Act
            var lesson = await _repository.AddLessonAsync(DateOnly.FromDateTime(DateTime.Now), "Guitar Class", isPaid: false, 
                student.Id, null, false, 60, 25.0m, false, 0, false, 0, 10, "Note");

            // Assert
            Assert.AreEqual("Guitar Class", lesson.Name);
            Assert.IsFalse(lesson.IsPricePerHour);
            Assert.AreEqual(25.0m, lesson.FinalPrice);
            Assert.AreEqual(60, lesson.DurationMinutes);
            Assert.AreEqual(25.0m, lesson.BasePrice);
            Assert.IsFalse(lesson.IsOnline);
            Assert.AreEqual(0, lesson.TravelAllowance);
            Assert.IsFalse(lesson.IsWeekenOrHoliday);
            Assert.AreEqual(0, lesson.WeekendFee);
            Assert.AreEqual(10, lesson.Tip);
            Assert.AreEqual("Note", lesson.Notes);
            var dbLesson = await _context.Lessons.FirstAsync(l => l.Id == lesson.Id);
            Assert.AreEqual(student.Id, dbLesson.StudentId);
        }

        // --- UPDATE TESTS ---

        [TestMethod]
        public async Task UpdateLessonAsync_InvalidId_ThrowsException()
        {
            // Arrange
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
            {
                await _repository.UpdateLesson(Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Now), "Guitar Class",
                false, 60, 25.0m, 
                false, 0, false, 0, 15.5m, "Note");
            });
        }

        [TestMethod]
        public async Task UpdateLessonAsync()
        {
            // Arrange
            var payer = TestGenerator.CreatePayer1(emptyInfo: true);
            var student = TestGenerator.CreateStudent1(payer.Id);
            var lesson = TestGenerator.CreateLesson(student.Id, paid: false);

            _context.Payers.Add(payer);
            _context.Students.Add(student);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            var updatedLesson = await _repository.UpdateLesson(lesson.Id, TestGenerator.LessonNewDate, "Guitar Class",
                false, 60, 25.0m, false, 0, false, 0, 15.5m, "Updated note");
            Assert.AreEqual("Guitar Class", updatedLesson.Name);
            Assert.IsFalse(updatedLesson.IsPricePerHour);
            Assert.AreEqual("Updated note", updatedLesson.Notes);
            Assert.AreEqual(60, updatedLesson.DurationMinutes);
            Assert.AreEqual(25.0m, updatedLesson.BasePrice);
            Assert.IsFalse(updatedLesson.IsOnline);
            Assert.AreEqual(0, updatedLesson.TravelAllowance);
            Assert.IsFalse(updatedLesson.IsWeekenOrHoliday);
            Assert.AreEqual(0, updatedLesson.WeekendFee);
            var dbLesson = await _context.Lessons.FirstAsync(l => l.Id == lesson.Id);
            Assert.AreEqual("Guitar Class", dbLesson.Name);
            Assert.IsFalse(dbLesson.IsPricePerHour);
            Assert.AreEqual("Updated note", dbLesson.Notes);
            Assert.AreEqual(60, dbLesson.DurationMinutes);
            Assert.AreEqual(25.0m, dbLesson.BasePrice);
            Assert.IsFalse(dbLesson.IsOnline);
            Assert.AreEqual(0, dbLesson.TravelAllowance);
            Assert.IsFalse(dbLesson.IsWeekenOrHoliday);
            Assert.AreEqual(0, dbLesson.WeekendFee);
            Assert.AreEqual(15.5m, dbLesson.Tip);
        }
    }
}
