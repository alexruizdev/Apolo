namespace Models
{
    public static class Helper
    {
        public static string GetFullName(string firstName, string lastName) => $"{firstName} {lastName}".Trim();

        public static (List<Service> Services, List<Payer> Payers, List<Student> Students,
            List<Specification> Specifications, List<Lesson> Lessons, List<BillingDocument> Invoices)
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
                DurationMinutes = 60, Price = 30, IsOnline = false, IsWeekendOrHoliday = false });
            specifications.Add(new Specification() { Name = "Spec 2", StudentId = students[1].Id, ServiceId = services[1].Id, 
                DurationMinutes = 30, Price = 15, IsOnline = true, IsWeekendOrHoliday = false });
            specifications.Add(new Specification() { Name = "Spec 3", StudentId = students[2].Id, ServiceId = services[2].Id, 
                DurationMinutes = 0, Price = null, IsOnline = false, IsWeekendOrHoliday = true });

            var bills = new List<BillingDocument>();
            bills.Add(new BillingDocument(new DateTime(2025, 1, 1)) { Id = Guid.NewGuid(), Type = DocumentType.Invoice, SequenceNumber = 0, 
                PayerId = payers[0].Id});
            bills.Add(new BillingDocument(new DateTime(2024, 1, 1)) { Id = Guid.NewGuid(), Type = DocumentType.Ticket, SequenceNumber = 0, 
                PayerId = payers[1].Id});

            var lessons = new List<Lesson>();
            lessons.Add(new Lesson(new DateOnly(2025, 1, 1), "Lesson 1", isPaid: false, students[0].Id, bills[0].Id, 
                isPricePerHour: true, durationMinutes: 60, basePrice: 30, isOnline: false, travelAllowance: 10, 
                isWeekendOrHoliday: false, weekendFee: 5, tip: 0, notes: "smalll note"));
            lessons.Add(new Lesson(new DateOnly(2024, 1, 1), "Lesson 2", isPaid: true, students[1].Id, bills[1].Id,
                isPricePerHour: false, durationMinutes: null, basePrice: 45, isOnline: true, travelAllowance: 10,
                isWeekendOrHoliday: true, weekendFee: 5, tip: 0, notes: "long note"));
            lessons.Add(new Lesson(new DateOnly(2026, 1, 1), "Lesson 3", isPaid: false, students[2].Id, null,
                isPricePerHour: false, durationMinutes: null, basePrice: 45, isOnline: true, travelAllowance: 10,
                isWeekendOrHoliday: true, weekendFee: 5, tip: 0, notes: null));
            lessons.Add(new Lesson(new DateOnly(2026, 1, 1), "Lesson 4", isPaid: false, students[2].Id, null,
                isPricePerHour: false, durationMinutes: null, basePrice: 60, isOnline: false, travelAllowance: 10,
                isWeekendOrHoliday: false, weekendFee: 5, tip: 0, notes: null));

            return (services, payers, students, specifications, lessons, bills);
        }
    }
}
