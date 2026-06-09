using Microsoft.EntityFrameworkCore;
using Models;
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
            // Act
            var results = (await _repository.GetLessonsAsync(showOnlyUnpaid: true, months: null)).ToList();

            // Assert
            Assert.HasCount(21, results);
            Assert.AreEqual("French Lessons - Chloe", results[0].Name);
            Assert.IsTrue(results[0].IsPricePerHour);
            Assert.AreEqual(60, results[0].DurationMinutes);
            Assert.AreEqual(35, results[0].BasePrice);
            Assert.IsFalse(results[0].IsOnline);
            Assert.AreEqual(7, results[0].TravelAllowance);
            Assert.IsTrue(results[0].IsWeekendOrHoliday);
            Assert.AreEqual(15, results[0].WeekendFee);
            Assert.IsNull(results[0].Notes);
            Assert.IsFalse(results[0].IsPaid);
        }

        [TestMethod]
        public async Task GetLessonsAsync_FilterByMonths_FiltersOldData()
        {
            var now = DateTimeOffset.UtcNow;
            var months = ((now.Year - 2025) * 12) + now.Month - 8;            
            var results = (await _repository.GetLessonsAsync(showOnlyUnpaid: false, months: months)).ToList();

            // Assert
            Assert.HasCount(9, results);
            Assert.AreEqual("French Lessons - Chloe", results[0].Name);
        }

        // --- CREATE TESTS ---

        [TestMethod]
        public async Task AddLessonAsync_CreatesLessonAndLessons()
        {
            // Act
            var lesson = await _repository.AddLessonAsync(DateOnly.FromDateTime(DateTime.Now), "Guitar Class", isPaid: false, 
                _data.Students[0].Id, null, false, 60, 25.0m, false, 0, false, 0, 10, "Note");

            // Assert
            Assert.AreEqual("Guitar Class", lesson.Name);
            Assert.IsFalse(lesson.IsPricePerHour);
            Assert.AreEqual(25.0m, lesson.FinalPrice);
            Assert.AreEqual(60, lesson.DurationMinutes);
            Assert.AreEqual(25.0m, lesson.BasePrice);
            Assert.IsFalse(lesson.IsOnline);
            Assert.AreEqual(0, lesson.TravelAllowance);
            Assert.IsFalse(lesson.IsWeekendOrHoliday);
            Assert.AreEqual(0, lesson.WeekendFee);
            Assert.AreEqual(10, lesson.Tip);
            Assert.AreEqual("Note", lesson.Notes);
            var dbLesson = await _context.Lessons.FirstAsync(l => l.Id == lesson.Id);
            Assert.AreEqual(_data.Students[0].Id, dbLesson.StudentId);
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
            var updatedLesson = await _repository.UpdateLesson(_data.Lessons[19].Id, DateOnly.FromDateTime(DateTime.Now), "Guitar Class",
                false, 60, 25.0m, false, 0, false, 0, 15.5m, "Updated note");
            var dbLesson = await _context.Lessons.FirstAsync(l => l.Id == updatedLesson.Id);
            Assert.AreEqual("Guitar Class", dbLesson.Name);
            Assert.IsFalse(dbLesson.IsPricePerHour);
            Assert.AreEqual("Updated note", dbLesson.Notes);
            Assert.AreEqual(60, dbLesson.DurationMinutes);
            Assert.AreEqual(25.0m, dbLesson.BasePrice);
            Assert.IsFalse(dbLesson.IsOnline);
            Assert.AreEqual(0, dbLesson.TravelAllowance);
            Assert.IsFalse(dbLesson.IsWeekendOrHoliday);
            Assert.AreEqual(0, dbLesson.WeekendFee);
            Assert.AreEqual(15.5m, dbLesson.Tip);
        }

        [TestMethod]
        public async Task UpdateLessonsAsync()
        {
            var lessonIds = _data.Lessons.
                Where(l => l.StudentId == _data.Students[0].Id).
                Select(l => l.Id).ToList();

            await _repository.UpdateLessonsPayment(lessonIds, isPaid: true);

            Assert.AreEqual(20, _context.Lessons.Count(a => a.IsPaid));

        }

        [TestMethod]
        public async Task UpdateLessonsAsyncEmpty()
        {
            await _repository.UpdateLessonsPayment([], isPaid: true);

            Assert.AreEqual(19, _context.Lessons.Count(l => l.IsPaid));

        }

        [TestMethod]
        public async Task UnassignBillToLessons()
        {
            var lessonIds = _data.Lessons.Select(l => l.Id).ToList();

            await _repository.UnassignBillToLessons(lessonIds);

            foreach (var lesson in _context.Lessons)
            {
                Assert.IsNull(lesson.BillingDocumentId);
            }
        }

    }
}
