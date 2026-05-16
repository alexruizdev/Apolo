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
            Assert.IsEmpty(student.Lessons);
            Assert.IsEmpty(student.Specifications);
        }

        [TestMethod]
        public void TestLessonSummary()
        {
            var longNote = "This is a long note, more than 70 characters, enough to test the functionality.";
            var shortNote = "This is a long note, more than 70 characters, enough to test the funct...";
            var lesson = new LessonSummary(Id: Guid.NewGuid(), DateOnly.FromDateTime(DateTime.Today), "Lesson",
                FinalPrice: 65, IsPaid: true, StudentId: Guid.NewGuid(), "Student", BillingDocumentId: Guid.NewGuid(),
                "2025-09-E-0001", IsPricePerHour: true, DurationMinutes: 60, BasePrice: 30, IsOnline: false, TravelAllowance: 5, 
                IsWeekenOrHoliday: false, WeekendFee: 5, longNote);

            Assert.AreEqual(shortNote, lesson.ShortNote);
        }

        [TestMethod]
        public void TestLesson()
        {
            var date = new DateOnly(2025, 11, 12);
            var studentId = Guid.NewGuid();
            var billingDocumentId = Guid.NewGuid();

            var shortNote = "This is a short note.";
            var lesson = new Lesson(date, "Lesson", isPaid: false, studentId, null, isPricePerHour: false,
                durationMinutes: null, basePrice: 90, isOnline: true, travelAllowance: 5, isWeekenOrHoliday: true, weekendFee: 5,
                shortNote);
            Assert.AreEqual(95, lesson.FinalPrice);

            Assert.Throws<ArgumentException>(() => lesson.Set(isPricePerHour: true, duration: null, price: 35, online: false, 
                travel: 5, weekend: false, fee:5));

            Assert.IsEmpty(Lesson.Truncate(null, 10));
            Assert.AreEqual(shortNote,Lesson.Truncate(shortNote, 70));

            Assert.IsTrue(lesson.Set(isPricePerHour: true, duration: 64, price: 35, online: false, travel: 7.5m,
                weekend: false, fee: 7.5m));
            Assert.IsTrue(lesson.IsPricePerHour);
            Assert.IsNotNull(lesson.DurationMinutes);
            Assert.AreEqual(64, lesson.DurationMinutes.Value);
            Assert.AreEqual(35, lesson.BasePrice);
            Assert.IsFalse(lesson.IsOnline);
            Assert.AreEqual(7.5m, lesson.TravelAllowance);
            Assert.IsFalse(lesson.IsWeekenOrHoliday);
            Assert.AreEqual(7.5m, lesson.WeekendFee);
            Assert.AreEqual(44.83m, lesson.FinalPrice);

            lesson.IsPaid = true;
            Assert.IsFalse(lesson.Set(isPricePerHour: true, duration: 90, price: 35, online: false, travel: 7.5m,
                weekend: false, fee: 7.5m));
            Assert.IsNotNull(lesson.DurationMinutes);
            Assert.AreEqual(64, lesson.DurationMinutes.Value);

            lesson.IsPaid = false;
            lesson.BillingDocumentId = billingDocumentId;
            Assert.IsFalse(lesson.Set(isPricePerHour: true, duration: 90, price: 35, online: false, travel: 7.5m,
                weekend: false, fee: 7.5m));
            Assert.IsNotNull(lesson.DurationMinutes);
            Assert.AreEqual(64, lesson.DurationMinutes.Value);
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
