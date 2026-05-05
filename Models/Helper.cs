namespace Models
{
    public static class Helper
    {
        public static string GetFullName(string firstName, string lastName) => $"{firstName} {lastName}".Trim();

        public static (List<Service> Services, List<Payer> Payers, List<Student> Students,
            List<Specification> Specifications, List<Lesson> Lessons, List<Invoice> Invoices)
            GetData()
        {
                        var services = new List<Service>();
            services.Add(new Service() { Name= "Service 1", IsPricePerHour = true , Price= 10m});
            services.Add(new Service() { Name= "Service 2", IsPricePerHour = true, Price= 20m});
            services.Add(new Service() { Name= "Service 3", IsPricePerHour = false , Price= 30m});

            var payers = new List<Payer>();
            payers.Add(new Payer() { FirstName = "Payer", LastName = "1" });
            payers.Add(new Payer() { FirstName = "Payer", LastName = "2" });
            payers.Add(new Payer() { FirstName = "Payer", LastName = "3" });

            var students = new List<Student>();
            students.Add(new Student() { FirstName = "Student", LastName = "1", PayerId = payers[0].Id });
            students.Add(new Student() { FirstName = "Student", LastName = "2", PayerId = payers[1].Id });
            students.Add(new Student() { FirstName = "Student", LastName = "3", PayerId = payers[2].Id });

            var specifications = new List<Specification>();
            specifications.Add(new Specification() { Name = "Spec 1", StudentId = students[0].Id, ServiceId = services[0].Id, 
                DurationMinutes = 60, Price = 30, IsOnline = false, IsWeekenOrHoliday = false });
            specifications.Add(new Specification() { Name = "Spec 2", StudentId = students[1].Id, ServiceId = services[1].Id, 
                DurationMinutes = 30, Price = 15, IsOnline = true, IsWeekenOrHoliday = false });
            specifications.Add(new Specification() { Name = "Spec 3", StudentId = students[2].Id, ServiceId = services[2].Id, 
                DurationMinutes = 0, Price = null, IsOnline = false, IsWeekenOrHoliday = true });

            var lessons = new List<Lesson>();
            lessons.Add(new Lesson() { Date = new DateOnly(2024, 1, 1), Name = "Lesson 1", IsPricePerHour = true, 
                DurationMinutes = 60, PricePerAttendance = 30, IsOnline = false, TravelAllowance = 10, IsWeekenOrHoliday = false,
                WeekendFee = 5, Notes = "small note" });
            lessons[0].Attendances.Add(new Attendance() { LessonId = lessons[0].Id, StudentId = students[0].Id, IsPaid = false, 
                Price = 30 });
            lessons.Add(new Lesson() { Date = new DateOnly(2024, 1, 1), Name = "Lesson 2", IsPricePerHour = true, 
                DurationMinutes = 0, PricePerAttendance = 15, IsOnline = false, TravelAllowance = 15, IsWeekenOrHoliday = true,
                WeekendFee = 5, Notes = "long note" });
            lessons[1].Attendances.Add(new Attendance() { LessonId = lessons[1].Id, StudentId = students[1].Id, IsPaid = false, 
                Price = 15 });
            lessons.Add(new Lesson() { Date = new DateOnly(2024, 1, 1), Name = "Lesson 3", IsPricePerHour = false, 
                DurationMinutes = 45, PricePerAttendance = 45, IsOnline = true, TravelAllowance = 10, IsWeekenOrHoliday = false,
                WeekendFee = 10, Notes = null });
            lessons[2].Attendances.Add(new Attendance() { LessonId = lessons[2].Id, StudentId = students[2].Id, IsPaid = true, 
                Price = 60 });

            var invoices = new List<Invoice>();
            invoices.Add(new Invoice() { Id = 1, Name = "Invoice_1", CreatedUTC = DateTime.Now, PayerId = payers[0].Id });
            invoices[0].Lines.Add(new InvoiceAttendance() { InvoiceId = invoices[0].Id, AttendanceId = lessons[0].Attendances.First().Id });
            invoices.Add(new Invoice() { Id = 2, Name = "Invoice_2", CreatedUTC = DateTime.Now, PayerId = payers[1].Id });
            invoices[1].Lines.Add(new InvoiceAttendance() { InvoiceId = invoices[1].Id, AttendanceId = lessons[1].Attendances.First().Id });
            invoices.Add(new Invoice() { Id = 3, Name = "Invoice_3", CreatedUTC = DateTime.Now, PayerId = payers[2].Id });
            invoices[2].Lines.Add(new InvoiceAttendance() { InvoiceId = invoices[2].Id, AttendanceId = lessons[2].Attendances.First().Id });

            return (services, payers, students, specifications, lessons, invoices);
        }
    }
}
