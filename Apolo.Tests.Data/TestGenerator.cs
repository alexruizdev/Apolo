using Models;
using System.Reflection.Metadata.Ecma335;

namespace Apolo.Tests.Data
{
    public static class TestGenerator
    {
        // Service const
        public const string ServiceName1 = "Service / hour";
        public const string ServiceName2 = "Contract service";
        public const decimal ServicePrice1 = 10m;
        public const decimal ServicePrice2 = 20m;

        // Payer const
        public const string PayerName1 = "John";
        public const string PayerLastName1 = "Doe";
        public const string Address1 = "123 Main St";
        public const string ZipCode1 = "12345";
        public const string City1 = "Anytown";
        public const string TaxId1 = "123456789";

        // Student const
        public const string StudentName1 = "Jane";
        public const string StudentLastName1 = "Smith";
        public const string StudentName2 = "Alice";
        public const string StudentLastName2 = "Johnson";

        // Lesson const
        public static readonly DateOnly LessonOldDate = DateOnly.FromDateTime(DateTime.Now.AddMonths(-6));
        public static readonly DateOnly LessonNewDate = DateOnly.FromDateTime(DateTime.Now);
        public const string LessonNamePaid = "Lesson paid";
        public const string LessonNameUnpaid = "Lesson unpaid";
        public const int NormalDuration = 60;
        public const int LongDuration = 180;
        public const int ShortDuration = 25;
        public const decimal LessonPricePerAttendance = 50;
        public const decimal LessonTravelAllowance = 10;
        public const decimal LessonWeekendFee = 5;
        public const string LessonNotes = "This is a test lesson.";
        public const decimal LessonTotalPrice = 65; // 50 + 10 travel + 5 weekend fee

        // Specification const
        public const string SpecificationName1 = "Specification 1";
        public const string SpecificationName2 = "Specification 2";

        // Invoice const
        public const int InvoiceId1 = 1;
        public const string InvoiceName1 = "Invoice 1";

        // Service constructors
        public static Service CreateService1() => new Service
        {
            Id = Guid.NewGuid(),
            Name = ServiceName1,
            Price = ServicePrice1,
            IsPricePerHour = true
        };
        public static Service CreateService2() => new Service
        {
            Id = Guid.NewGuid(),
            Name = ServiceName2,
            Price = ServicePrice2,
            IsPricePerHour = false
        };
        public static Service CreateServiceDuplicate1() => new Service
        {
            Id = Guid.NewGuid(),
            Name = ServiceName1.ToLower(),
            Price = ServicePrice1,
            IsPricePerHour = true
        };
        public static Service CreateTemporaryService(Guid id) => new Service
        {
            Id = id,
            Name = "Temporary",
            Price = 10,
        };

        // Payer constructors
        public static Payer CreatePayer1(bool emptyInfo) => new Payer
        {
            Id = Guid.NewGuid(),
            FirstName = PayerName1,
            LastName = PayerLastName1,
            Address = emptyInfo ? null : Address1,
            ZipCode = emptyInfo ? null : ZipCode1,
            City = emptyInfo ? null : City1,
            TaxId = emptyInfo ? null : TaxId1
        };

        public static Payer CreateTemporaryPayer(Guid id) => new Payer
        {
            Id = id,
            FirstName = "Temporary",
            LastName = "Payer"
        };

        // Student constructors
        public static Student CreateStudent1(Guid payerId) => new Student
        {
            Id = Guid.NewGuid(),
            PayerId = payerId,
            FirstName = StudentName1,
            LastName = StudentLastName1
        };
        public static Student CreateStudent2(Guid payerId) => new Student
        {
            Id = Guid.NewGuid(),
            PayerId = payerId,
            FirstName = StudentName2,
            LastName = StudentLastName2
        };

        // Lesson constructors
        public static Lesson CreateLessonPaid() => new Lesson
        {
            Id = Guid.NewGuid(),
            Name = LessonNamePaid,
            Date = LessonOldDate,
            IsPricePerHour = true,
            DurationMinutes = NormalDuration,
            PricePerAttendance = LessonPricePerAttendance,
            IsOnline = false,
            TravelAllowance = LessonTravelAllowance,
            IsWeekenOrHoliday = true,
            WeekendFee = LessonWeekendFee,
            Notes = LessonNotes
        };
        public static Lesson CreateLessonUnpaid(bool paid = false) => new Lesson
        {
            Id = Guid.NewGuid(),
            Name = paid ? LessonNamePaid : LessonNameUnpaid,
            Date = LessonNewDate,
            IsPricePerHour = false,
            DurationMinutes = LongDuration,
            PricePerAttendance = LessonPricePerAttendance,
            IsOnline = true,
            TravelAllowance = LessonTravelAllowance,
            IsWeekenOrHoliday = false,
            WeekendFee = LessonWeekendFee,
            Notes = null
        };

        private static DateOnly GetRandomDateLastNMonths(int months)
        {
            var random = new Random();
            var today = DateOnly.FromDateTime(DateTime.Today);
            var start = today.AddMonths(-months);

            // Calculate the range in days
            int range = today.DayNumber - start.DayNumber;

            // Pick a random number of days within that range and add to start
            return start.AddDays(random.Next(range));
        }

        private static bool RandomBool = Random.Shared.Next(2) == 0;

        public static Lesson CreateRandomLesson(string name, bool paid, int months) => new Lesson
        {
            Id = Guid.NewGuid(),
            Name = name,
            Date = GetRandomDateLastNMonths(months),
            IsPricePerHour = false,
            PricePerAttendance = LessonPricePerAttendance,
            IsOnline = RandomBool,
            TravelAllowance = LessonTravelAllowance,
            IsWeekenOrHoliday = RandomBool,
            WeekendFee = LessonWeekendFee
        };

        public static Lesson CreateLesson(Guid studentId, bool paid = false)
        {
            var lesson = paid ? CreateLessonPaid() : CreateLessonUnpaid();
            lesson.Attendaces = new List<Attendance>
            {
                new Attendance
                {
                    Id = Guid.NewGuid(),
                    LessonId = lesson.Id,
                    StudentId = studentId,
                    IsPaid = paid,
                    Price = lesson.GetFinalPricePerStudent()
                }
            };
            return lesson;
        }

        public static Lesson CreateRandomLesson(Guid studentId, string name, bool paid, int months)
        {
            var lesson = CreateRandomLesson(name, paid, months);
            lesson.Attendaces = new List<Attendance>    
            {
                new Attendance
                {
                    Id = Guid.NewGuid(),
                    LessonId = lesson.Id,
                    StudentId = studentId,
                    IsPaid = paid,
                    Price = lesson.GetFinalPricePerStudent()
                }
            };
            return lesson;
        }

        // Specification constructors
        public static Specification CreateSpecification1(Guid studentId, Guid serviceId) => new Specification
        {
            Id = Guid.NewGuid(),
            Name = SpecificationName1,
            StudentId = studentId,
            ServiceId = serviceId,
            DurationMinutes = LongDuration,
            Price = ServicePrice2,
            IsOnline = false,
            IsWeekenOrHoliday = true
        };

        public static Specification CreateSpecification2(Guid studentId, Guid serviceId) => new Specification
        {
            Id = Guid.NewGuid(),
            Name = SpecificationName2,
            StudentId = studentId,
            ServiceId = serviceId,
            DurationMinutes = ShortDuration,
            IsOnline = true,
            IsWeekenOrHoliday = false
        };

        public static Invoice CreateInvoice(List<Lesson> lessons, Guid payerId)
        {
            var invoice = new Invoice
            {
                Id = InvoiceId1,
                Name = InvoiceName1,
                CreatedUTC = DateTime.UtcNow,
                PayerId = payerId
            };

            invoice.Lines = lessons.SelectMany(l => l.Attendaces.Select(a => new InvoiceAttendance
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                AttendanceId = a.Id
            })).ToList();

            return invoice;
        }
    }
}
