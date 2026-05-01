using Models;

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
        public static readonly DateOnly LessonDate1 = DateOnly.Parse("08/19/2012");
        public const string LessonName1 = "Lesson 1";
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
        public static Lesson CreateLesson1() => new Lesson
        {
            Id = Guid.NewGuid(),
            Name = LessonName1,
            Date = LessonDate1,
            IsPricePerHour = true,
            DurationMinutes = NormalDuration,
            PricePerAttendance = LessonPricePerAttendance,
            IsOnline = false,
            TravelAllowance = LessonTravelAllowance,
            IsWeekenOrHoliday = true,
            WeekendFee = LessonWeekendFee,
            Notes = LessonNotes
        };

        public static Lesson CreateLesson1(Guid studentId)
        {
            var lesson = CreateLesson1();
            lesson.Attendaces = new List<Attendance>
            {
                new Attendance
                {
                    Id = Guid.NewGuid(),
                    LessonId = lesson.Id,
                    StudentId = studentId
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
    }
}
