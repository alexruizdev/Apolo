using Models;

namespace Apolo.Tests.Models
{
    [TestClass]
    public class ModelsTests
    {
        [TestMethod]
        public void TestPayerSummaryFullName()
        {
            var payer = new PayerSummary(new System.Guid(), "First", "Name", 0, null, null, null, null);
            Assert.AreEqual("First Name", payer.FullName);
        }

        [TestMethod]
        public void TestPayerFullName()
        {
            var payer = new Payer()
            {
                FirstName = "First",
                LastName = "Name"
            };
            Assert.AreEqual("First Name", payer.FullName);
            Assert.AreEqual("First Name", payer.ToString());
            Assert.IsNull(payer.Address);
            Assert.IsNull(payer.ZipCode);
            Assert.IsNull(payer.City);
            Assert.IsNull(payer.TaxId);
            Assert.IsEmpty(payer.Students);
        }

        [TestMethod]
        public void TestStudentSummaryFullName()
        {
            var student = new StudentSummary(new System.Guid(), "First", "Name", new System.Guid(), "Payer Name");
            Assert.AreEqual("First Name", student.FullName);
        }

        [TestMethod]
        public void TestStudentFullName()
        {
            var student = new Student()
            {
                FirstName = "First",
                LastName = "Name",
                PayerId = new System.Guid()
            };
            Assert.AreEqual("First Name", student.FullName);
            Assert.IsEmpty(student.Attendances);
            Assert.IsEmpty(student.Specifications);
        }

        [TestMethod]
        public void TestLessonSummary()
        {
            var longNote = "This is a long note, more than 70 characters, enough to test the functionality.";
            var shortNote = "This is a long note, more than 70 characters, enough to test the funct...";
            var lesson = new LessonSummary(
                Guid.NewGuid(), "Lesson", DateOnly.FromDateTime(DateTime.Today),
                true, 60, 30,
                false, 5, false, 5, longNote,
                [ new AttendanceSummary(Guid.NewGuid(), Guid.NewGuid(), "Student 1", false),
                new AttendanceSummary(Guid.NewGuid(), Guid.NewGuid(), "Student 2", false)]);

            Assert.AreEqual(65, lesson.GrandTotal);
            Assert.AreEqual(shortNote, lesson.ShortNote);
        }

        [TestMethod]
        public void TestLesson()
        {
            var student1Id = Guid.NewGuid();
            var student2Id = Guid.NewGuid();
            var students = new List<StudentOption>()
            {
                new StudentOption(student1Id, "Student 1"),
                new StudentOption(student2Id, "Student 2")
            };

            var shortNote = "This is a short note.";
            var lesson = new Lesson()
            {
                Name = "Lesson",
                Date = DateOnly.FromDateTime(DateTime.Today),
                IsPricePerHour = false,
                DurationMinutes = null,
                PricePerAttendance = 90,
                IsOnline = true,
                TravelAllowance = 5,
                IsWeekenOrHoliday = true,
                WeekendFee = 5,
                Notes = shortNote
            };
            lesson.Attendances.Add(new Attendance() { Id = Guid.NewGuid(), StudentId = student1Id, IsPaid = false, Price = 95 });
            lesson.Attendances.Add(new Attendance() { Id = Guid.NewGuid(), StudentId = student2Id, IsPaid = false, Price = 95 });
            Assert.AreEqual(95, lesson.GetFinalPricePerStudent());

            Assert.Throws<ArgumentException>(() => Lesson.GetPrice(1, true, null, 90, true, 5, true, 5));
            Assert.Throws<ArgumentException>(() => Lesson.GetPrice(0, true, 60, 90, true, 5, true, 5));

            Assert.IsEmpty(Lesson.Truncate(null, 10));
            Assert.AreEqual(shortNote,Lesson.Truncate(shortNote, 70));

            var attendanceSummaries = lesson.AttendancesSummary(students);
            Assert.IsNotNull(attendanceSummaries);
            Assert.HasCount(2, attendanceSummaries);
            Assert.AreEqual("Student 1", attendanceSummaries[0].StudentName);
            Assert.AreEqual("Student 2", attendanceSummaries[1].StudentName);
        }   

        [TestMethod]
        public void TestUserProfile ()
        {
            var profile = new UserProfile();
            Assert.IsEmpty(profile.FullName);
            Assert.IsEmpty(profile.Address);
            Assert.IsEmpty(profile.ZipCode);
            Assert.IsEmpty(profile.City);
            Assert.IsEmpty(profile.Phone);
            Assert.IsEmpty(profile.TaxId);
            Assert.IsEmpty(profile.Email);
            Assert.IsEmpty(profile.BankName);
            Assert.IsEmpty(profile.BankAccount);
            Assert.AreEqual(0, profile.IvaPercent);
            Assert.AreEqual(0, profile.TravelAllowance);
            Assert.AreEqual(0, profile.WeekendFee);
        }

        [TestMethod]
        public void TestPayerActivityInfo()
        {
            var payer = new PayerActivityInfo()
            {
                PayerId = Guid.NewGuid(),
                PayerName = "Payer 1",
                LastLessonDate = new DateOnly(2024, 1, 17)
            };

            Assert.AreEqual("Payer 1 - Last activity: 17/01/2024", payer.Display);

            payer.LastLessonDate = null;

            Assert.AreEqual("Payer 1 - No recorded activity", payer.Display);
        }
    }
}
