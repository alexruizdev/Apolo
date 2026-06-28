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
            Assert.AreEqual("First Name", payer.Name);
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
            Assert.AreEqual("First Name", student.Name);
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
                IsWeekendOrHoliday: false, WeekendFee: 5, Tip: 0, longNote);

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
                durationMinutes: null, basePrice: 90, isOnline: true, travelAllowance: 5, isWeekendOrHoliday: true, weekendFee: 5, tip: 0,
                shortNote);
            Assert.AreEqual(95, lesson.FinalPrice);

            Assert.Throws<ArgumentException>(() => lesson.Set(isPricePerHour: true, duration: null, price: 35, online: false,
                travel: 5, weekend: false, fee: 5));

            Assert.IsEmpty(Lesson.Truncate(null, 10));
            Assert.AreEqual(shortNote, Lesson.Truncate(shortNote, 70));

            Assert.IsTrue(lesson.Set(isPricePerHour: true, duration: 64, price: 35, online: false, travel: 7.5m,
                weekend: false, fee: 7.5m));
            Assert.IsTrue(lesson.IsPricePerHour);
            Assert.IsNotNull(lesson.DurationMinutes);
            Assert.AreEqual(64, lesson.DurationMinutes.Value);
            Assert.AreEqual(35, lesson.BasePrice);
            Assert.IsFalse(lesson.IsOnline);
            Assert.AreEqual(7.5m, lesson.TravelAllowance);
            Assert.IsFalse(lesson.IsWeekendOrHoliday);
            Assert.AreEqual(7.5m, lesson.WeekendFee);
            Assert.AreEqual(45m, lesson.FinalPrice);

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
        public void TestUserProfile()
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
        public void TestFullName()
        {
            var fullName = Helper.GetFullName("First", "Last");
            Assert.AreEqual("First Last", fullName);
            fullName = Helper.GetFullName("First", "");
            Assert.AreEqual("First", fullName);
            fullName = Helper.GetFullName("", "Last");
            Assert.AreEqual("Last", fullName);
            fullName = Helper.GetFullName("", "");
            Assert.AreEqual("", fullName);
        }

        [TestMethod]
        public void TestConvertToServiceSummary()
        {
            var service = new Service()
            {
                Id = Guid.NewGuid(),
                Name = "Service 1",
                IsPricePerHour = true,
                Price = 30
            };
            var summary = Helper.ConvertToServiceSummary(service);
            Assert.AreEqual(service.Id, summary.Id);
            Assert.AreEqual(service.Name, summary.Name);
            Assert.AreEqual(service.IsPricePerHour, summary.IsPricePerHour);
            Assert.AreEqual((double)service.Price, summary.Price);
        }

        [TestMethod]
        public void TestDummyData()
        {
            var data = new DummyData();

            // Services
            {
                Assert.HasCount(6, data.Services);
                Assert.AreEqual(1, data.Services.Count(s => !s.IsPricePerHour));
            }

            // Payers
            {
                Assert.HasCount(10, data.Payers);

                var payerActivities = data.PayerActivities();
                Assert.AreEqual("John Doe", payerActivities[0].PayerName);
                Assert.IsNotNull(payerActivities[0].LastLessonDate);
                Assert.AreEqual("John Doe - Last activity: 18/08/2025", payerActivities[0].Display);
                Assert.AreEqual("Isabel Fernandez", payerActivities[8].PayerName);
                Assert.IsNull(payerActivities[8].LastLessonDate);
                Assert.AreEqual("Isabel Fernandez - No recorded activity", payerActivities[8].Display);

                var payerOptions = data.PayerOptionsByUnbilledLessons();
                Assert.HasCount(10, payerOptions);
                Assert.AreEqual("Carlos Gomez - 1 lesson", payerOptions[2].FullName);
                Assert.AreEqual("Olivia Dubois - 4 lessons", payerOptions[7].FullName);

                var payerSummaries = data.PayerSummaries();
                Assert.AreEqual(0, payerSummaries[0].Outstanding);
                Assert.AreEqual(0, payerSummaries[1].Outstanding);
                Assert.AreEqual(90, payerSummaries[2].Outstanding);
                Assert.AreEqual(35, payerSummaries[3].Outstanding);
                Assert.AreEqual(65, payerSummaries[4].Outstanding);
                Assert.AreEqual(110, payerSummaries[5].Outstanding);
                Assert.AreEqual(35, payerSummaries[6].Outstanding);
                Assert.AreEqual(260, payerSummaries[7].Outstanding);
                Assert.AreEqual(0, payerSummaries[8].Outstanding);
                Assert.AreEqual(0, payerSummaries[9].Outstanding);

                Assert.HasCount(5, data.ArchivePayers);
            }

            // Students
            {
                Assert.HasCount(11, data.Students);
                var studentOption = data.StudentOptions.First();
                var studentSummary = data.StudentSummaries.Last();
                Assert.AreEqual("Alice Doe", studentOption.FullName);
                Assert.AreEqual("Lucia", studentSummary.FirstName);
                Assert.AreEqual("Garcia", studentSummary.LastName);
                Assert.AreEqual("David Garcia", studentSummary.PayerName);

                Assert.HasCount(8, data.ArchiveStudents);
                var archiveStudentOption = data.ArchiveStudentOptions.First();
                var archiveStudentSummary = data.ArchiveStudentSummaries.Last();
                Assert.AreEqual("Ethan Clark", archiveStudentOption.FullName);
                Assert.AreEqual("Layla", archiveStudentSummary.FirstName);
                Assert.AreEqual("Al-Farsi", archiveStudentSummary.LastName);
                Assert.AreEqual("Fatima Al-Farsi", archiveStudentSummary.PayerName);
            }

            // Specifications
            {
                Assert.HasCount(10, data.SpecificationOptions);
                var spec = data.SpecificationOptions.First();
                Assert.AreEqual("Math Tutoring - Alice", spec.Display);
                Assert.IsNull(spec.Price);
                Assert.AreEqual(60, spec.DurationMinutes);
                Assert.IsTrue(spec.IsOnline);
                Assert.IsFalse(spec.IsWeekend);

                var specSummary = data.SpecificationSummaries[1];
                Assert.AreEqual("Science Tutoring - Bob", specSummary.Name);
                Assert.AreEqual("Bob Doe", specSummary.StudentName);
                Assert.AreEqual("Science Tutoring", specSummary.ServiceName);
                Assert.AreEqual(90, specSummary.DurationMinutes);
                Assert.IsNotNull(specSummary.Price);
                Assert.AreEqual(75, specSummary.Price.Value);
                Assert.IsFalse(specSummary.IsOnline);
                Assert.IsTrue(specSummary.IsWeekendOrHoliday);
                Assert.AreEqual(3, specSummary.UsageCount);
            }

            // Bills
            {
                Assert.HasCount(24, data.Bills);
                var bill = data.BillSummaries.First();
                Assert.AreEqual(DocumentType.Invoice, bill.Type);
                Assert.AreEqual("06-2024-E-0004", bill.Name);
                Assert.AreEqual("30/06/2024", bill.Date);

                Assert.HasCount(10, data.ArchiveBills);
                var archiveBill = data.ArchiveBillSummaries[1];
                Assert.AreEqual(DocumentType.Ticket, archiveBill.Type);
                Assert.AreEqual("TCK-12-2025-0008", archiveBill.Name);
                Assert.AreEqual("15/12/2025", archiveBill.Date);
            }

            // Lessons
            {
                Assert.HasCount(44, data.Lessons);

                Assert.AreEqual(40, data.Lessons[0].FinalPrice);
                Assert.AreEqual(72.5m, data.Lessons[1].FinalPrice);
                Assert.AreEqual(42, data.Lessons[2].FinalPrice);
                Assert.AreEqual(72.5m, data.Lessons[3].FinalPrice);
                Assert.AreEqual(60, data.Lessons[4].FinalPrice);
                Assert.AreEqual(52.5m, data.Lessons[5].FinalPrice);
                Assert.AreEqual(42, data.Lessons[6].FinalPrice);
                Assert.AreEqual(75, data.Lessons[7].FinalPrice);
                Assert.AreEqual(35, data.Lessons[8].FinalPrice);
                Assert.AreEqual(95, data.Lessons[9].FinalPrice);
                Assert.AreEqual(35, data.Lessons[10].FinalPrice);
                Assert.AreEqual(101, data.Lessons[11].FinalPrice);
                Assert.AreEqual(35, data.Lessons[12].FinalPrice);
                Assert.AreEqual(107.5m, data.Lessons[13].FinalPrice);
                Assert.AreEqual(35, data.Lessons[14].FinalPrice);
                Assert.AreEqual(107.5m, data.Lessons[15].FinalPrice);
                Assert.AreEqual(90, data.Lessons[16].FinalPrice);
                Assert.AreEqual(90, data.Lessons[17].FinalPrice);
                Assert.AreEqual(90, data.Lessons[18].FinalPrice);
                Assert.AreEqual(90, data.Lessons[19].FinalPrice);
                Assert.AreEqual(35, data.Lessons[20].FinalPrice);
                Assert.AreEqual(35, data.Lessons[21].FinalPrice);
                Assert.AreEqual(35, data.Lessons[22].FinalPrice);
                Assert.AreEqual(35, data.Lessons[23].FinalPrice);
                Assert.AreEqual(55, data.Lessons[24].FinalPrice);
                Assert.AreEqual(57.5m, data.Lessons[25].FinalPrice);
                Assert.AreEqual(57.5m, data.Lessons[26].FinalPrice);
                Assert.AreEqual(65, data.Lessons[27].FinalPrice);
                Assert.AreEqual(100, data.Lessons[28].FinalPrice);
                Assert.AreEqual(110, data.Lessons[29].FinalPrice);
                Assert.AreEqual(110, data.Lessons[30].FinalPrice);
                Assert.AreEqual(110, data.Lessons[31].FinalPrice);
                Assert.AreEqual(35, data.Lessons[32].FinalPrice);
                Assert.AreEqual(35, data.Lessons[33].FinalPrice);
                Assert.AreEqual(35, data.Lessons[34].FinalPrice);
                Assert.AreEqual(35, data.Lessons[35].FinalPrice);
                Assert.AreEqual(40, data.Lessons[36].FinalPrice);
                Assert.AreEqual(42.5m, data.Lessons[37].FinalPrice);
                Assert.AreEqual(42.5m, data.Lessons[38].FinalPrice);
                Assert.AreEqual(50, data.Lessons[39].FinalPrice);
                Assert.AreEqual(52.5m, data.Lessons[40].FinalPrice);
                Assert.AreEqual(52.5m, data.Lessons[41].FinalPrice);
                Assert.AreEqual(52.5m, data.Lessons[42].FinalPrice);
                Assert.AreEqual(52.5m, data.Lessons[43].FinalPrice);

                var lessonsByPayer = data.LessonLinesByPayer(data.Payers[0].Id);
                Assert.HasCount(8, lessonsByPayer);
                Assert.AreEqual(new DateOnly(2024, 06, 10), lessonsByPayer[0].Date);
                Assert.AreEqual("Math Tutoring - Alice", lessonsByPayer[0].Name);
                Assert.AreEqual("Alice Doe", lessonsByPayer[0].StudentName);
                Assert.AreEqual(40, lessonsByPayer[0].FinalPrice);
                Assert.IsTrue(lessonsByPayer[0].IsPaid);

                var lessonsByBill = data.LessonsLinesByBill(data.Bills[2].Id);
                Assert.HasCount(4, lessonsByBill);
                Assert.AreEqual(new DateOnly(2025, 03, 05), lessonsByBill[0].Date);
                Assert.AreEqual("Math Tutoring - Alice", lessonsByBill[0].Name);
                Assert.AreEqual("Alice Doe", lessonsByBill[0].StudentName);
                Assert.AreEqual(60, lessonsByBill[0].FinalPrice);
                Assert.IsTrue(lessonsByBill[0].IsPaid);

                Assert.HasCount(23, data.ArchiveLessons);
            }
        }
        [TestMethod]
        public void GenerateReport()
        {
            // online, weekend, price per hour, 2 weekly
            ProposalInput input = new()
            {
                ServiceName = "Test",
                BasePrice = 30,
                IsOnline = true,
                TravelAllowance = 10,
                IsWeekendOrHoliday = true,
                WeekendFee = 5,
                IsPricePerHour = true,
                Duration = 90,
                Frequency = 2,
                Unit = FrequencyUnit.PerWeek
            };

            ProposalReport report = ProposalService.CalculateProposal(input);
            Assert.AreEqual("Test", report.ServiceName);
            Assert.AreEqual(35, report.BasePrice);
            Assert.AreEqual("1.5", report.RateMultiplier);
            Assert.AreEqual(52.5m, report.Subtotal);
            Assert.AreEqual(90, report.Duration);
            Assert.AreEqual(5, report.WeekendFeeApplied);
            Assert.AreEqual(0, report.TravelAllowanceApplied);

            Assert.AreEqual(52.5m, report.PricePerSession);
            Assert.AreEqual(8.66, report.SessionsPerMonth);
            Assert.AreEqual(454.65m, report.PricePerMonth);

            Assert.IsNotNull(report.AlternativeFee);
            Assert.AreEqual("WEEK DAY OPTION", report.AlternativeFee.Label);
            Assert.AreEqual(2, report.AlternativeFee.Frequency);
            Assert.AreEqual(FrequencyUnit.PerWeek, report.AlternativeFee.Unit);
            Assert.AreEqual(8.66, report.AlternativeFee.SessionsPerMonth);
            Assert.AreEqual(389.7m, report.AlternativeFee.TotalPricePerMonth);

            Assert.IsNotNull(report.AlternativeTravel);
            Assert.AreEqual("IN-PERSON OPTION", report.AlternativeTravel.Label);
            Assert.AreEqual(2, report.AlternativeTravel.Frequency);
            Assert.AreEqual(FrequencyUnit.PerWeek, report.AlternativeTravel.Unit);
            Assert.AreEqual(8.66, report.AlternativeTravel.SessionsPerMonth);
            Assert.AreEqual(541.25m, report.AlternativeTravel.TotalPricePerMonth);

            Assert.IsNotNull(report.BudgetRequested);
            Assert.AreEqual("REQUEST BUDGET", report.BudgetRequested.Label);
            Assert.AreEqual(2, report.BudgetRequested.Frequency);
            Assert.AreEqual(FrequencyUnit.PerWeek, report.BudgetRequested.Unit);
            Assert.AreEqual(8.66, report.BudgetRequested.SessionsPerMonth);
            Assert.AreEqual(454.65m, report.BudgetRequested.TotalPricePerMonth);

            Assert.IsNotNull(report.BudgetMinus);
            Assert.AreEqual("REDUCED OPTIONS (-1)", report.BudgetMinus.Label);
            Assert.AreEqual(1, report.BudgetMinus.Frequency);
            Assert.AreEqual(FrequencyUnit.PerWeek, report.BudgetMinus.Unit);
            Assert.AreEqual(4.33, report.BudgetMinus.SessionsPerMonth);
            Assert.AreEqual(227.325m, report.BudgetMinus.TotalPricePerMonth);

            Assert.IsNotNull(report.BudgetPlus);
            Assert.AreEqual("EXPANDED OPTIONS (+1)", report.BudgetPlus.Label);
            Assert.AreEqual(3, report.BudgetPlus.Frequency);
            Assert.AreEqual(FrequencyUnit.PerWeek, report.BudgetPlus.Unit);
            Assert.AreEqual(12.99, report.BudgetPlus.SessionsPerMonth);
            Assert.AreEqual(681.975m, report.BudgetPlus.TotalPricePerMonth);

            // in person, weekday, flat rate, 1 monthly
            input.IsOnline = false;
            input.IsWeekendOrHoliday = false;
            input.IsPricePerHour = false;
            input.Unit = FrequencyUnit.PerMonth;
            input.Frequency = 1;

            report = ProposalService.CalculateProposal(input);
            Assert.AreEqual("Test", report.ServiceName);
            Assert.AreEqual(30, report.BasePrice);
            Assert.AreEqual("1", report.RateMultiplier);
            Assert.AreEqual(30, report.Subtotal);
            Assert.AreEqual(90, report.Duration);
            Assert.AreEqual(0, report.WeekendFeeApplied);
            Assert.AreEqual(10, report.TravelAllowanceApplied);

            Assert.AreEqual(40m, report.PricePerSession);
            Assert.AreEqual(1, report.SessionsPerMonth);
            Assert.AreEqual(40, report.PricePerMonth);

            Assert.IsNotNull(report.AlternativeFee);
            Assert.AreEqual("WEEKEND OR HOLIDAY OPTION", report.AlternativeFee.Label);
            Assert.AreEqual(1, report.AlternativeFee.Frequency);
            Assert.AreEqual(FrequencyUnit.PerMonth, report.AlternativeFee.Unit);
            Assert.AreEqual(1, report.AlternativeFee.SessionsPerMonth);
            Assert.AreEqual(45m, report.AlternativeFee.TotalPricePerMonth);

            Assert.IsNotNull(report.AlternativeTravel);
            Assert.AreEqual("ONLINE OPTION", report.AlternativeTravel.Label);
            Assert.AreEqual(1, report.AlternativeTravel.Frequency);
            Assert.AreEqual(FrequencyUnit.PerMonth, report.AlternativeTravel.Unit);
            Assert.AreEqual(1, report.AlternativeTravel.SessionsPerMonth);
            Assert.AreEqual(30m, report.AlternativeTravel.TotalPricePerMonth);

            Assert.IsNotNull(report.BudgetRequested);
            Assert.AreEqual("REQUEST BUDGET", report.BudgetRequested.Label);
            Assert.AreEqual(1, report.BudgetRequested.Frequency);
            Assert.AreEqual(FrequencyUnit.PerMonth, report.BudgetRequested.Unit);
            Assert.AreEqual(1, report.BudgetRequested.SessionsPerMonth);
            Assert.AreEqual(40, report.BudgetRequested.TotalPricePerMonth);

            Assert.IsNotNull(report.BudgetMinus);
            Assert.AreEqual("REDUCED OPTIONS (-1)", report.BudgetMinus.Label);
            Assert.AreEqual(1, report.BudgetMinus.Frequency);
            Assert.AreEqual(FrequencyUnit.PerMonth, report.BudgetMinus.Unit);
            Assert.AreEqual(1, report.BudgetMinus.SessionsPerMonth);
            Assert.AreEqual(40, report.BudgetMinus.TotalPricePerMonth);

            Assert.IsNotNull(report.BudgetPlus);
            Assert.AreEqual("EXPANDED OPTIONS (+1)", report.BudgetPlus.Label);
            Assert.AreEqual(2, report.BudgetPlus.Frequency);
            Assert.AreEqual(FrequencyUnit.PerMonth, report.BudgetPlus.Unit);
            Assert.AreEqual(2, report.BudgetPlus.SessionsPerMonth);
            Assert.AreEqual(80, report.BudgetPlus.TotalPricePerMonth);
        }
    }
}
