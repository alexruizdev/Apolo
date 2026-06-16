
namespace Models
{
    public static class Helper
    {
        public static string GetFullName(string firstName, string lastName) => $"{firstName} {lastName}".Trim();

        public static List<Service> GetDummyServices() => new List<Service>
        {
            new Service { Name = "Math Tutoring", IsPricePerHour = true, Price = 40.00m },
            new Service { Name = "Science Tutoring", IsPricePerHour = true, Price = 45.00m },
            new Service { Name = "Language Lessons", IsPricePerHour = true, Price = 35.00m },
            new Service { Name = "Personal Coaching", IsPricePerHour = true, Price = 50.00m },
            new Service { Name = "Programming Lessons", IsPricePerHour = true, Price = 60.00m },
            new Service { Name = "Exam Preparation Package", IsPricePerHour = false, Price = 300.00m }
        };

        public static ServiceSummary ConvertToServiceSummary(Service service) => 
            new ServiceSummary (service.Id, service.Name, service.IsPricePerHour, (double)service.Price);

        public static List<ServiceSummary> GetDummyServiceSummaries() => 
            GetDummyServices().Select(s => ConvertToServiceSummary(s)).ToList();

        public static List<Payer> GetDummyPayers() => new List<Payer>
        {
            new Payer { FirstName = "John", LastName = "Doe", Address = "123 Main St", ZipCode = "10001", City = "New York", TaxId = "TX123456" },
            new Payer { FirstName = "Jane", LastName = "Smith", Address = "456 Oak Ave", ZipCode = "90001", City = "Los Angeles", TaxId = "TX654321" },
            new Payer { FirstName = "Carlos", LastName = "Gomez", Address = "Calle Mayor 10", ZipCode = "28001", City = "Madrid", TaxId = "ES12345678A" },
            new Payer { FirstName = "Maria", LastName = "Lopez", Address = "Av. Diagonal 200", ZipCode = "08018", City = "Barcelona", TaxId = "ES87654321B" },
            new Payer { FirstName = "Luca", LastName = "Rossi", Address = "Via Roma 15", ZipCode = "00100", City = "Rome", TaxId = "IT11223344" },
            new Payer { FirstName = "Emma", LastName = "Brown", Address = "789 Pine Rd", ZipCode = "SW1A 1AA", City = "London", TaxId = "UK998877" },
            new Payer { FirstName = "Noah", LastName = "Müller", Address = "Hauptstrasse 5", ZipCode = "10115", City = "Berlin", TaxId = "DE55667788" },
            new Payer { FirstName = "Olivia", LastName = "Dubois", Address = "12 Rue de Rivoli", ZipCode = "75001", City = "Paris", TaxId = "FR44556677" },
            new Payer { FirstName = "Isabel", LastName = "Fernandez", Address = "Calle Gran Via 45", ZipCode = "28013", City = "Madrid", TaxId = "ES99887766C" },
            new Payer { FirstName = "David", LastName = "Garcia", Address = "Calle Alcalá 120", ZipCode = "28009", City = "Madrid", TaxId = "ES11223344D" }
        };

        public static PayerOption ConvertToPayerOption(Payer payer) =>
            new PayerOption(payer.Id, payer.FullName);

        public static PayerActivityInfo ConvertToPayerActivityInfo(Payer payer) => new PayerActivityInfo
        {
            PayerId = payer.Id,
            PayerName = payer.FullName,
            LastLessonDate = payer.Students
                .SelectMany(s => s.Lessons)
                .Select(l => (DateOnly?) l.Date)
                .Max()
        };

        public static PayerSummary ConvertToPayerSummary(Payer payer, decimal outstanding) =>
            new PayerSummary(payer.Id, payer.FirstName, payer.LastName, outstanding, payer.Address, payer.ZipCode, payer.City, payer.TaxId);

        public static List<Student> GetDummyStudents(ref List<Payer> payers) => new List<Student>
        {
            new Student { FirstName = "Alice", LastName = "Doe", PayerId = payers[0].Id }, // John Doe
            new Student { FirstName = "Bob", LastName = "Doe", PayerId = payers[0].Id }, // John Doe
            new Student { FirstName = "Charlie", LastName = "Smith", PayerId = payers[1].Id }, // Jane Smith
            new Student { FirstName = "Diana", LastName = "Smith", PayerId = payers[1].Id }, // Jane Smith
            new Student { FirstName = "Luis", LastName = "Gomez", PayerId = payers[2].Id }, // Carlos Gomez
            new Student { FirstName = "Sofia", LastName = "Lopez", PayerId = payers[3].Id }, // Maria Lopez
            new Student { FirstName = "Marco", LastName = "Rossi", PayerId = payers[4].Id }, // Luca Rossi
            new Student { FirstName = "Emily", LastName = "Brown", PayerId = payers[5].Id }, // Emma Brown
            new Student { FirstName = "Leon", LastName = "Müller", PayerId = payers[6].Id }, // Noah Muller
            new Student { FirstName = "Chloe", LastName = "Dubois", PayerId = payers[7].Id }, // Olivia Dubois
            new Student { FirstName = "Lucia", LastName = "Garcia", PayerId = payers[9].Id } // David Garcia
        };

        public static StudentOption ConvertToStudentOption(Student student) =>
            new StudentOption(student.Id, student.FullName);

        public static List<StudentOption> GetDummyStudentOptions()
        {
            var payers = GetDummyPayers();
            return GetDummyStudents(ref payers).Select(s => ConvertToStudentOption(s)).ToList();
        }

        public static List<Specification> GetDummySpecifications(ref List<Student> students, ref List<Service> services)
            => new List<Specification>
        {
            new Specification { Name = "Math Tutoring - Alice", StudentId = students[0].Id, ServiceId = services[0].Id, DurationMinutes = 60, Price = null, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 5 },
            new Specification { Name = "Science Tutoring - Bob", StudentId = students[1].Id, ServiceId = services[1].Id, DurationMinutes = 90, Price = 75.00m, IsOnline = false, IsWeekendOrHoliday = true, UsageCount = 3 },
            new Specification { Name = "English Lessons - Charlie", StudentId = students[2].Id, ServiceId = services[2].Id, DurationMinutes = 60, Price = null, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 8 },
            new Specification { Name = "Physics Coaching - Diana", StudentId = students[3].Id, ServiceId = services[1].Id, DurationMinutes = 120, Price = 120.00m, IsOnline = false, IsWeekendOrHoliday = true, UsageCount = 2 },
            new Specification { Name = "Programming Basics - Luis", StudentId = students[4].Id, ServiceId = services[5].Id, DurationMinutes = 0, Price = null, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 6 },
            new Specification { Name = "Spanish Practice - Sofia", StudentId = students[5].Id, ServiceId = services[0].Id, DurationMinutes = 45, Price = 35.00m, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 10 },
            new Specification { Name = "Fitness Session - Marco", StudentId = students[6].Id, ServiceId = services[3].Id, DurationMinutes = 60, Price = null, IsOnline = false, IsWeekendOrHoliday = true, UsageCount = 4 },
            new Specification { Name = "Business Coaching - Emily", StudentId = students[7].Id, ServiceId = services[1].Id, DurationMinutes = 120, Price = 150.00m, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 1 },
            new Specification { Name = "German Lessons - Leon", StudentId = students[8].Id, ServiceId = services[0].Id, DurationMinutes = 60, Price = null, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 7 },
            new Specification { Name = "French Lessons - Chloe", StudentId = students[9].Id, ServiceId = services[0].Id, DurationMinutes = 60, Price = 55.00m, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 9 }
        };

        public static List<BillingDocument> GetDummyBillingDocuments(ref List<Payer> payers) => new List<BillingDocument>
        {
            new BillingDocument(new DateTime(2024, 6, 30)) { Type = DocumentType.Invoice, SequenceNumber = 1, PayerId = payers[0].Id }, // John Doe
            new BillingDocument(new DateTime(2025, 2, 28)) { Type = DocumentType.Invoice, SequenceNumber = 2, PayerId = payers[0].Id }, // John Doe
            new BillingDocument(new DateTime(2025, 10, 31)) { Type = DocumentType.Ticket, SequenceNumber = 10, PayerId = payers[0].Id }, // John Doe

            new BillingDocument(new DateTime(2024, 7, 31)) { Type = DocumentType.Invoice, SequenceNumber = 2, PayerId = payers[1].Id }, // Jane Smith
            new BillingDocument(new DateTime(2025, 3, 31)) { Type = DocumentType.Invoice, SequenceNumber = 3, PayerId = payers[1].Id },// Jane Smith
            new BillingDocument(new DateTime(2025, 11, 30)) { Type = DocumentType.Invoice, SequenceNumber = 11, PayerId = payers[1].Id },// Jane Smith

            new BillingDocument(new DateTime(2024, 8, 31)) { Type = DocumentType.Ticket, SequenceNumber = 3, PayerId = payers[2].Id }, // Carlos Gomez
            new BillingDocument(new DateTime(2025, 4, 30)) { Type = DocumentType.Ticket, SequenceNumber = 4, PayerId = payers[2].Id }, // Carlos Gomez
            new BillingDocument(new DateTime(2025, 12, 31)) { Type = DocumentType.Invoice, SequenceNumber = 12, PayerId = payers[2].Id }, // Carlos Gomez

            new BillingDocument(new DateTime(2024, 9, 30)) { Type = DocumentType.Invoice, SequenceNumber = 4, PayerId = payers[3].Id }, // Maria Lopez
            new BillingDocument(new DateTime(2025, 5, 31)) { Type = DocumentType.Invoice, SequenceNumber = 5, PayerId = payers[3].Id }, // Maria Lopez
            new BillingDocument(new DateTime(2026, 1, 31)) { Type = DocumentType.Ticket, SequenceNumber = 1, PayerId = payers[3].Id }, // Maria Lopez

            new BillingDocument(new DateTime(2024, 10, 31)) { Type = DocumentType.Ticket, SequenceNumber = 5, PayerId = payers[4].Id }, // Luca Rossi
            new BillingDocument(new DateTime(2025, 6, 30)) { Type = DocumentType.Invoice, SequenceNumber = 6, PayerId = payers[4].Id }, // Luca Rossi
            new BillingDocument(new DateTime(2026, 2, 28)) { Type = DocumentType.Invoice, SequenceNumber = 2, PayerId = payers[4].Id }, // Luca Rossi

            new BillingDocument(new DateTime(2024, 11, 30)) { Type = DocumentType.Invoice, SequenceNumber = 6, PayerId = payers[5].Id }, // Emma Brown
            new BillingDocument(new DateTime(2025, 7, 31)) { Type = DocumentType.Ticket, SequenceNumber = 7, PayerId = payers[5].Id }, // Emma Brown
            new BillingDocument(new DateTime(2026, 3, 31)) { Type = DocumentType.Invoice, SequenceNumber = 3, PayerId = payers[5].Id }, // Emma Brown

            new BillingDocument(new DateTime(2024, 12, 31)) { Type = DocumentType.Invoice, SequenceNumber = 7, PayerId = payers[6].Id }, // Noah Muller
            new BillingDocument(new DateTime(2025, 8, 31)) { Type = DocumentType.Invoice, SequenceNumber = 8, PayerId = payers[6].Id }, // Noah Muller
            new BillingDocument(new DateTime(2026, 4, 30)) { Type = DocumentType.Ticket, SequenceNumber = 4, PayerId = payers[6].Id }, // Noah Muller

            new BillingDocument(new DateTime(2025, 1, 31)) { Type = DocumentType.Ticket, SequenceNumber = 1, PayerId = payers[7].Id }, // Olivia Dubois
            new BillingDocument(new DateTime(2025, 9, 30)) { Type = DocumentType.Invoice, SequenceNumber = 9, PayerId = payers[7].Id }, // Olivia Dubois
            new BillingDocument(new DateTime(2026, 5, 31)) { Type = DocumentType.Invoice, SequenceNumber = 5, PayerId = payers[7].Id } // Olivia Dubois
        };

        public static List<Lesson> GetDummyLessons(ref List<Student> students, ref List<BillingDocument> billingDocuments)
             => new List<Lesson>
        {
            new Lesson(new DateOnly(2024, 6, 10), "Math Tutoring - Alice", true, students[0].Id, billingDocuments[0].Id, true, 60, 40.00m, true, 5, false, 10, 2, null), // John Doe
            new Lesson(new DateOnly(2024, 6, 15), "Science Tutoring - Bob", false, students[1].Id, billingDocuments[0].Id, true, 90, 45.00m, false, 10, true, 15, 0, null), // John Doe
            new Lesson(new DateOnly(2024, 10, 5), "Math Tutoring - Alice", false, students[0].Id, billingDocuments[1].Id, true, 60, 42.00m, true, 5, false, 10, 2, null), // John Doe
            new Lesson(new DateOnly(2024, 10, 20), "Science Tutoring - Bob", true, students[1].Id, billingDocuments[1].Id, true, 90, 45.00m, false, 10, false, 10, 0, null), // John Doe
            new Lesson(new DateOnly(2025, 3, 5), "Math Tutoring - Alice", true, students[0].Id, billingDocuments[2].Id, true, 90, 40.00m, true, 5, false, 10, 2, null), // John Doe
            new Lesson(new DateOnly(2025, 3, 20), "Science Tutoring - Bob", false, students[1].Id, billingDocuments[2].Id, true, 60, 45.00m, false, 10, false, 10, 0, null), // John Doe
            new Lesson(new DateOnly(2025, 8, 3), "Math Tutoring - Alice", true, students[0].Id, billingDocuments[2].Id, true, 60, 42.00m, true, 5, false, 10, 2, null), // John Doe
            new Lesson(new DateOnly(2025, 8, 18), "Science Tutoring - Bob", false, students[1].Id, billingDocuments[2].Id, true, 90, 45.00m, false, 10, true, 15, 0, null), // John Doe

            new Lesson(new DateOnly(2024, 7, 1), "English Lessons - Charlie", true, students[2].Id, billingDocuments[3].Id, true, 60, 35.00m, true, 5, false, 10, 3, null), // Jane Smith
            new Lesson(new DateOnly(2024, 7, 12), "Physics Coaching - Diana", false, students[3].Id, billingDocuments[3].Id, true, 120, 45.00m, false, 8, true, 20, 5, null), // Jane Smith
            new Lesson(new DateOnly(2024, 11, 1), "English Lessons - Charlie", false, students[2].Id, billingDocuments[4].Id, true, 60, 35.00m, true, 5, false, 10, 1, null), // Jane Smith
            new Lesson(new DateOnly(2024, 11, 15), "Physics Coaching - Diana", true, students[3].Id, billingDocuments[4].Id, true, 120, 48.00m, false, 8, true, 20, 4, null), // Jane Smith
            new Lesson(new DateOnly(2025, 4, 2), "English Lessons - Charlie", true, students[2].Id, billingDocuments[5].Id, true, 60, 35.00m, true, 5, false, 10, 2, null), // Jane Smith
            new Lesson(new DateOnly(2025, 4, 15), "Physics Coaching - Diana", false, students[3].Id, billingDocuments[5].Id, true, 120, 50.00m, false, 8, true, 20, 5, null), // Jane Smith
            new Lesson(new DateOnly(2025, 9, 1), "English Lessons - Charlie", true, students[2].Id, billingDocuments[5].Id, true, 60, 35.00m, true, 5, false, 10, 2, null), // Jane Smith
            new Lesson(new DateOnly(2025, 9, 15), "Physics Coaching - Diana", false, students[3].Id, billingDocuments[5].Id, true, 120, 50.00m, false, 8, true, 20, 4, null), // Jane Smith

            new Lesson(new DateOnly(2024, 7, 20), "Programming Basics - Luis", true, students[4].Id, billingDocuments[6].Id, true, 90, 60.00m, true, 5, false, 10, 0, null), // Carlos Gomez
            new Lesson(new DateOnly(2024, 12, 1), "Programming Basics - Luis", false, students[4].Id, billingDocuments[7].Id, true, 90, 60.00m, true, 5, false, 10, 0, null), // Carlos Gomez
            new Lesson(new DateOnly(2025, 5, 3), "Programming Basics - Luis", true, students[4].Id, billingDocuments[8].Id, true, 90, 60.00m, true, 5, false, 10, 0, null), // Carlos Gomez
            new Lesson(new DateOnly(2026, 1, 10), "Programming Basics - Luis", false, students[4].Id, null, true, 90, 60.00m, true, 5, false, 10, 0, null), // Carlos Gomez

            new Lesson(new DateOnly(2024, 8, 5), "Spanish Practice - Sofia", false, students[5].Id, billingDocuments[9].Id, false, null, 35.00m, true, 0, false, 0, 1.5m, null), // Maria Lopez
            new Lesson(new DateOnly(2024, 12, 18), "Spanish Practice - Sofia", true, students[5].Id, billingDocuments[10].Id, false, null, 35.00m, true, 0, false, 0, 2, null), // Maria Lopez
            new Lesson(new DateOnly(2025, 5, 18), "Spanish Practice - Sofia", false, students[5].Id, billingDocuments[10].Id, false, null, 35.00m, true, 0, false, 0, 1, null), // Maria Lopez
            new Lesson(new DateOnly(2026, 1, 25), "Spanish Practice - Sofia", false, students[5].Id, billingDocuments[11].Id, false, null, 35.00m, true, 0, false, 0, 2, null), // Maria Lopez

            new Lesson(new DateOnly(2024, 8, 18), "Fitness Session - Marco", true, students[6].Id, billingDocuments[12].Id, true, 60, 50.00m, false, 6, true, 12, 2, null), // Luca Rossi
            new Lesson(new DateOnly(2025, 1, 5), "Fitness Session - Marco", true, students[6].Id, billingDocuments[13].Id, true, 60, 50.00m, false, 6, false, 10, 2, null), // Luca Rossi
            new Lesson(new DateOnly(2025, 6, 2), "Fitness Session - Marco", true, students[6].Id, billingDocuments[13].Id, true, 60, 50.00m, false, 6, true, 12, 2, null), // Luca Rossi
            new Lesson(new DateOnly(2026, 2, 5), "Fitness Session - Marco", true, students[6].Id, billingDocuments[14].Id, true, 60, 50.00m, false, 6, true, 12, 3, null), // Luca Rossi

            new Lesson(new DateOnly(2024, 9, 2), "Business Coaching - Emily", false, students[7].Id, billingDocuments[15].Id, true, 120, 50.00m, true, 5, false, 10, 0, null), // Emma Brown 
            new Lesson(new DateOnly(2025, 1, 15), "Business Coaching - Emily", false, students[7].Id, billingDocuments[16].Id, true, 120, 55.00m, true, 5, false, 10, 0, null), // Emma Brown 
            new Lesson(new DateOnly(2025, 6, 20), "Business Coaching - Emily", false, students[7].Id, billingDocuments[16].Id, true, 120, 55.00m, true, 5, false, 10, 0, null), // Emma Brown 
            new Lesson(new DateOnly(2026, 2, 20), "Business Coaching - Emily", false, students[7].Id, billingDocuments[17].Id, true, 120, 55.00m, true, 5, false, 10, 0, null), // Emma Brown 

            new Lesson(new DateOnly(2024, 9, 10), "German Lessons - Leon", true, students[8].Id, billingDocuments[18].Id, true, 60, 35.00m, true, 5, false, 10, 1, null), // Noah Muller
            new Lesson(new DateOnly(2025, 2, 2), "German Lessons - Leon", true, students[8].Id, billingDocuments[19].Id, true, 60, 35.00m, true, 5, false, 10, 1, null), // Noah Muller
            new Lesson(new DateOnly(2025, 7, 5), "German Lessons - Leon", true, students[8].Id, billingDocuments[19].Id, true, 60, 35.00m, true, 5, false, 10, 2, null), // Noah Muller
            new Lesson(new DateOnly(2026, 3, 5), "German Lessons - Leon", true, students[8].Id, billingDocuments[20].Id, true, 60, 35.00m, true, 5, false, 10, 1, null), // Noah Muller

            new Lesson(new DateOnly(2024, 9, 25), "French Lessons - Chloe", false, students[9].Id, billingDocuments[21].Id, true, 60, 35.00m, false, 7, true, 15, 2, null), // Olivia Dubois
            new Lesson(new DateOnly(2025, 2, 18), "French Lessons - Chloe", false, students[9].Id, billingDocuments[22].Id, true, 60, 35.00m, false, 7, true, 15, 3, null), // Olivia Dubois
            new Lesson(new DateOnly(2025, 7, 22), "French Lessons - Chloe", false, students[9].Id, billingDocuments[22].Id, true, 60, 35.00m, false, 7, true, 15, 3, null), // Olivia Dubois
            new Lesson(new DateOnly(2026, 3, 18), "French Lessons - Chloe", false, students[9].Id, billingDocuments[23].Id, true, 60, 35.00m, false, 7, true, 15, 2, null) // Olivia Dubois
        };

        public static LessonSummary ConvertToLessonSummary(Lesson l) => new LessonSummary(
            l.Id,
            l.Date,
            l.Name,
            l.FinalPrice,
            l.IsPaid,
            l.StudentId,
            l.Student == null ? string.Empty : l.Student.FullName,
            l.BillingDocumentId,
            l.BillingDocument == null ? string.Empty : l.BillingDocument.DocumentNumber,
            l.IsPricePerHour,
            l.DurationMinutes,
            l.BasePrice,
            l.IsOnline,
            l.TravelAllowance,
            l.IsWeekendOrHoliday,
            l.WeekendFee,
            l.Tip,
            l.Notes);

        public static List<LessonSummary> GetDummyLessonSummaries()
        {
            var data = GetDummyData();
            return data.Lessons.Select(ConvertToLessonSummary).ToList();
        }

        public static (List<Service> Services, List<Payer> Payers, List<Student> Students,
            List<Specification> Specifications, List<Lesson> Lessons, List<BillingDocument> Invoices)
            GetDummyData()
        {
            var services = GetDummyServices();

            var payers = GetDummyPayers();

            var students = GetDummyStudents(ref payers);

            var specs = GetDummySpecifications(ref students, ref services);

            var billingDocuments = GetDummyBillingDocuments(ref payers);

            var lessons = GetDummyLessons(ref students, ref billingDocuments);

            return (services, payers, students, specs, lessons, billingDocuments);
        }

    }
    public interface ISummary
    {
        Guid Id { get; }
        string Name { get; }
    }
}
