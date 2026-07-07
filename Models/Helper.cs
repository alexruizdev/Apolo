
using System.Linq.Expressions;

namespace Models
{
    public class DummyData
    {
        // ================================================================= //
        //  SERVICES : 6                                                     //
        // ================================================================= //
        public List<ServiceSummary> ServiceSummaries =>
            [.. Services.Select(Helper.ConvertToServiceSummary)];
        public List<Service> Services =
        [
            new Service { Name = "Math Tutoring", IsPricePerHour = true, Price = 40.00m },
            new Service { Name = "Science Tutoring", IsPricePerHour = true, Price = 45.00m },
            new Service { Name = "Language Lessons", IsPricePerHour = true, Price = 35.00m },
            new Service { Name = "Personal Coaching", IsPricePerHour = true, Price = 50.00m },
            new Service { Name = "Programming Lessons", IsPricePerHour = true, Price = 60.00m },
            new Service { Name = "Exam Preparation Package", IsPricePerHour = false, Price = 300.00m }
        ];

        // ================================================================= //
        //  PAYERS : 10 | 5                                                  //
        // ================================================================= //
        public List<PayerOption> PayerOptions => [.. Payers.Select(Helper.ConvertToPayerOption)];
        public List<PayerOption> PayerOptionsByUnbilledLessons ()
        {
            List<PayerOption> result = [];
            foreach (var p in Payers)
            {
                List<Guid> ids = [.. Students.Where(s => s.PayerId == p.Id).Select(s => s.Id)];
                int count = Lessons.Count(l => !l.IsPaid && l.BillingDocumentId == null && ids.Contains(l.StudentId));
                result.Add(Helper.ConvertToPayerOptionWithCount(p, count));
            }
            return result;
        }

        public List<PayerActivityInfo> PayerActivities()
        {
            List<PayerActivityInfo> result = [];
            foreach (var p in Payers)
            {
                List<Guid> ids = [.. Students.Where(s => s.PayerId == p.Id).Select(s => s.Id)];
                List<Lesson> lessons = [.. Lessons.Where(l => ids.Contains(l.StudentId))];
                result.Add(Helper.ConvertToPayerActivityInfo(p, lessons));
            }
            return result;
        }
        public List<PayerSummary> PayerSummaries()
        {
            List<PayerSummary> result = [];
            foreach (var p in Payers)
            {
                List<Guid> ids = [.. Students.Where(s => s.PayerId == p.Id).Select(s => s.Id)];
                List<Lesson> lessons = [.. Lessons.Where(l => ids.Contains(l.StudentId))];
                var outstanding = lessons.Where(l => !l.IsPaid).Sum(l => l.FinalPrice);
                result.Add(Helper.ConvertToPayerSummary(p, outstanding));
            }
            return result;
        }
        public List<Payer> Payers =
        [
            //  2 Students - 3 Bills
            new Payer { FirstName = "John", LastName = "Doe", Address = "123 Main St", ZipCode = "10001", City = "New York", TaxId = "TX123456" },
            //  2 Students - 3 Bills
            new Payer { FirstName = "Jane", LastName = "Smith", Address = "456 Oak Ave", ZipCode = "90001", City = "Los Angeles", TaxId = "TX654321" },
            //  1 Students - 3 Bills
            new Payer { FirstName = "Carlos", LastName = "Gomez", Address = "Calle Mayor 10", ZipCode = "28001", City = "Madrid", TaxId = "ES12345678A" },
            //  1 Students - 3 Bills
            new Payer { FirstName = "Maria", LastName = "Lopez", Address = "Av. Diagonal 200", ZipCode = "08018", City = "Barcelona", TaxId = "ES87654321B" },
            //  1 Students - 3 Bills
            new Payer { FirstName = "Luca", LastName = "Rossi", Address = "Via Roma 15", ZipCode = "00100", City = "Rome", TaxId = "IT11223344" },
            //  1 Students - 3 Bills
            new Payer { FirstName = "Emma", LastName = "Brown", Address = "789 Pine Rd", ZipCode = "SW1A 1AA", City = "London", TaxId = "UK998877" },
            //  1 Students - 3 Bills
            new Payer { FirstName = "Noah", LastName = "Müller", Address = "Hauptstrasse 5", ZipCode = "10115", City = "Berlin", TaxId = "DE55667788" },
            //  1 Students - 3 Bills
            new Payer { FirstName = "Olivia", LastName = "Dubois", Address = "12 Rue de Rivoli", ZipCode = "75001", City = "Paris", TaxId = "FR44556677" },
            //  0 Students - 0 Bills
            new Payer { FirstName = "Isabel", LastName = "Fernandez", Address = "Calle Gran Via 45", ZipCode = "28013", City = "Madrid", TaxId = "ES99887766C" },
            //  1 Students - 0 Bills
            new Payer { FirstName = "David", LastName = "Garcia", Address = "Calle Alcalá 120", ZipCode = "28009", City = "Madrid", TaxId = "ES11223344D" }
        ];
        public List<PayerOption> ArchivePayerOptions => [.. ArchivePayers.Select(Helper.ConvertToPayerOption)];
        public List<Payer> ArchivePayers =
        [
            new Payer { FirstName = "Aiden", LastName = "Clark", Address = "742 Evergreen Terrace", ZipCode = "60601", City = "Chicago", TaxId = "US44556677" },
            new Payer { FirstName = "Sofia", LastName = "Martinez", Address = "Av. Reforma 350", ZipCode = "06500", City = "Mexico City", TaxId = "MX99882211" },
            new Payer { FirstName = "Yuki", LastName = "Tanaka", Address = "1-2-3 Shibuya", ZipCode = "150-0002", City = "Tokyo", TaxId = "JP55443322" },
            new Payer { FirstName = "Oliver", LastName = "Wilson", Address = "25 King Street", ZipCode = "M5H 1A1", City = "Toronto", TaxId = "CA22334455" },
            new Payer { FirstName = "Fatima", LastName = "Al-Farsi", Address = "Al Wahda Street 18", ZipCode = "00000", City = "Dubai", TaxId = "AE77665544" }
        ];

        // ================================================================= //
        //  STUDENTS : 11 | 8                                                //
        // ================================================================= //
        public List<StudentOption> StudentOptions => [.. Students.Select(Helper.ConvertToStudentOption)];
        public List<StudentSummary> StudentSummaries => [.. Students.Select(s => Helper.ConvertToStudentSummary(
            s, Payers.First(p => p.Id == s.PayerId)))];
        public List<Student> Students;

        public List<StudentOption> ArchiveStudentOptions => [.. ArchiveStudents.Select(Helper.ConvertToStudentOption)];
        public List<StudentSummary> ArchiveStudentSummaries => [.. ArchiveStudents.Select(s => Helper.ConvertToStudentSummary(
            s, ArchivePayers.First(p => p.Id == s.PayerId)))];
        public List<Student> ArchiveStudents;

        // ================================================================= //
        //  SPECIFICATIONS : 10                                              //
        // ================================================================= //
        // Travel (10) Weekend (5)
        public static decimal Specification1FinalPrice => 40m;
        public static decimal Specification2FinalPrice => 152.5m;
        public static decimal Specification3FinalPrice => 45m;
        public static decimal Specification4FinalPrice => 270m;
        public static decimal Specification5FinalPrice => 300;
        public static decimal Specification6FinalPrice => 26.5m;
        public static decimal Specification7FinalPrice => 65;
        public static decimal Specification8FinalPrice => 300;
        public static decimal Specification9FinalPrice => 40;
        public static decimal Specification10FinalPrice => 60;
        public List<SpecificationOption> SpecificationOptions => [.. Specifications.Select(Helper.ConvertToSpecificationOption)];
        public List<SpecificationSummary> SpecificationSummaries => [.. Specifications.Select(spec => Helper.ConvertToSpecificationSummary(
            spec, Students.First(student => student.Id == spec.StudentId), Services.First(service => service.Id == spec.ServiceId)))];
        public List<Specification> Specifications;

        // ================================================================= //
        //  INVOICES : 24 | 10                                               //
        // ================================================================= //
        public List<BillSummary> BillSummaries => [.. Bills.Select(Helper.ConverToBillSumary)];
        public List<BillSummary> ArchiveBillSummaries => [.. ArchiveBills.Select(Helper.ConverToBillSumary)];
        public List<BillingDocument> Bills;
        public List<BillingDocument> ArchiveBills;

        // ================================================================= //
        //  LESSONS : 44 | 23                                                //
        // ================================================================= //
        public List<LessonLine> LessonLinesByPayer (Guid payerId)
        {
            List<Guid> ids = [.. Students.Where(s => s.PayerId == payerId).Select(s => s.Id)];

            return [.. Lessons
                .Where(l => ids.Contains(l.StudentId))
                .Select(l => Helper.ConvertToLessonLine(l, Students.First(s => s.Id == l.StudentId)))];
        }
        public List<LessonLine> LessonsLinesByBill (Guid billId)
            => [.. Lessons.Where(l => l.BillingDocumentId == billId).Select(l => Helper.ConvertToLessonLine(l, Students.First(s => s.Id == l.StudentId)))];
        public List<LessonSummary> LessonSummaries =>
            [.. Lessons.Select(l => Helper.ConvertToLessonSummary(l, Students.FirstOrDefault(s => s.Id == l.StudentId), Bills.FirstOrDefault(b => b.Id == l.BillingDocumentId)))];
        public List<Lesson> Lessons;
        public List<Lesson> ArchiveLessons;

        // ================================================================= //
        //  CONSTRUCTOR                                                      //
        // ================================================================= //

        public DummyData()
        {
            // Payers 
            Students =
            [
                new Student { FirstName = "Alice", LastName = "Doe", PayerId = Payers[0].Id }, // John Doe 
                new Student { FirstName = "Bob", LastName = "Doe", PayerId = Payers[0].Id }, // John Doe
                new Student { FirstName = "Charlie", LastName = "Smith", PayerId = Payers[1].Id }, // Jane Smith
                new Student { FirstName = "Diana", LastName = "Smith", PayerId = Payers[1].Id }, // Jane Smith
                new Student { FirstName = "Luis", LastName = "Gomez", PayerId = Payers[2].Id }, // Carlos Gomez
                new Student { FirstName = "Sofia", LastName = "Lopez", PayerId = Payers[3].Id }, // Maria Lopez
                new Student { FirstName = "Marco", LastName = "Rossi", PayerId = Payers[4].Id }, // Luca Rossi
                new Student { FirstName = "Emily", LastName = "Brown", PayerId = Payers[5].Id }, // Emma Brown
                new Student { FirstName = "Leon", LastName = "Müller", PayerId = Payers[6].Id }, // Noah Muller
                new Student { FirstName = "Chloe", LastName = "Dubois", PayerId = Payers[7].Id }, // Olivia Dubois
                new Student { FirstName = "Lucia", LastName = "Garcia", PayerId = Payers[9].Id } // David Garcia
            ];

            ArchiveStudents =
            [
                // Aiden Clarck
                new Student { FirstName = "Ethan", LastName = "Clark", PayerId = ArchivePayers[0].Id },
                new Student { FirstName = "Mia", LastName = "Clark", PayerId = ArchivePayers[0].Id },
                new Student { FirstName = "Lucas", LastName = "Clark", PayerId = ArchivePayers[0].Id },

                // Sofia Martinez
                new Student { FirstName = "Valentina", LastName = "Martinez", PayerId = ArchivePayers[1].Id },
                new Student { FirstName = "Diego", LastName = "Martinez", PayerId = ArchivePayers[1].Id },

                new Student { FirstName = "Hana", LastName = "Tanaka", PayerId = ArchivePayers[2].Id }, // Yuki Tanaza
                new Student { FirstName = "Noah", LastName = "Wilson", PayerId = ArchivePayers[3].Id }, // Oliver Wilson
                new Student { FirstName = "Layla", LastName = "Al-Farsi", PayerId = ArchivePayers[4].Id } // Fatima Al-Farsi
            ];

            Specifications =
            [
                // #5 - 1h Online - Base: 40 - Total: 40
                new Specification { Name = "Math Tutoring - Alice", StudentId = Students[0].Id, ServiceId = Services[0].Id, DurationMinutes = 60, Price = null, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 5 },
                // #3 - 1h 30 min Weekend - Base: 75 - Total: 152.5
                new Specification { Name = "Science Tutoring - Bob", StudentId = Students[1].Id, ServiceId = Services[1].Id, DurationMinutes = 90, Price = 75.00m, IsOnline = false, IsWeekendOrHoliday = true, UsageCount = 3 },
                // #8 - 1h Online - Base: 35 - Total: 45
                new Specification { Name = "English Lessons - Charlie", StudentId = Students[2].Id, ServiceId = Services[2].Id, DurationMinutes = 60, Price = null, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 8 },
                // #2 - 2h Weekend - Base: 120 - Total: 270
                new Specification { Name = "Physics Coaching - Diana", StudentId = Students[3].Id, ServiceId = Services[1].Id, DurationMinutes = 120, Price = 120.00m, IsOnline = false, IsWeekendOrHoliday = true, UsageCount = 2 },
                // #6 - Flat Rate Online - Base: 300 - Total: 300
                new Specification { Name = "Programming Basics - Luis", StudentId = Students[4].Id, ServiceId = Services[5].Id, DurationMinutes = 0, Price = null, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 6 },
                // #10 - 45 min Online - Base: 35 - Total: 26.5
                new Specification { Name = "Spanish Practice - Sofia", StudentId = Students[5].Id, ServiceId = Services[0].Id, DurationMinutes = 45, Price = 35.00m, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 10 },
                // #4 - 1h Weekend - Base: 50 - Total: 65
                new Specification { Name = "Fitness Session - Marco", StudentId = Students[6].Id, ServiceId = Services[3].Id, DurationMinutes = 60, Price = null, IsOnline = false, IsWeekendOrHoliday = true, UsageCount = 4 },
                // #1 - 2h Online - Base: 150 - Total: 300
                new Specification { Name = "Business Coaching - Emily", StudentId = Students[7].Id, ServiceId = Services[1].Id, DurationMinutes = 120, Price = 150.00m, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 1 },
                // #7 - 1h Online - Base: 40 - Total: 40
                new Specification { Name = "German Lessons - Leon", StudentId = Students[8].Id, ServiceId = Services[0].Id, DurationMinutes = 60, Price = null, IsOnline = true, IsWeekendOrHoliday = false, UsageCount = 7 },
                // #9 - 1h Online Weekend - Base: 55 - Total: 60
                new Specification { Name = "French Lessons - Chloe", StudentId = Students[9].Id, ServiceId = Services[0].Id, DurationMinutes = 60, Price = 55.00m, IsOnline = true, IsWeekendOrHoliday = true, UsageCount = 9 }
            ];

            Bills =
            [
                new BillingDocument(new DateTime(2024, 06, 30)) { Type = DocumentType.Invoice, SequenceNumber = 04, PayerId = Payers[0].Id }, // John Doe
                new BillingDocument(new DateTime(2025, 02, 28)) { Type = DocumentType.Invoice, SequenceNumber = 01, PayerId = Payers[0].Id }, // John Doe
                new BillingDocument(new DateTime(2025, 10, 31)) { Type = DocumentType.Ticket,  SequenceNumber = 07, PayerId = Payers[0].Id }, // John Doe

                new BillingDocument(new DateTime(2024, 07, 31)) { Type = DocumentType.Invoice, SequenceNumber = 05, PayerId = Payers[1].Id }, // Jane Smith
                new BillingDocument(new DateTime(2025, 03, 31)) { Type = DocumentType.Invoice, SequenceNumber = 02, PayerId = Payers[1].Id },// Jane Smith
                new BillingDocument(new DateTime(2025, 11, 30)) { Type = DocumentType.Invoice, SequenceNumber = 08, PayerId = Payers[1].Id },// Jane Smith

                new BillingDocument(new DateTime(2024, 08, 31)) { Type = DocumentType.Ticket,  SequenceNumber = 01, PayerId = Payers[2].Id }, // Carlos Gomez
                new BillingDocument(new DateTime(2025, 04, 30)) { Type = DocumentType.Ticket,  SequenceNumber = 04, PayerId = Payers[2].Id }, // Carlos Gomez
                new BillingDocument(new DateTime(2025, 12, 31)) { Type = DocumentType.Invoice, SequenceNumber = 09, PayerId = Payers[2].Id }, // Carlos Gomez

                new BillingDocument(new DateTime(2024, 09, 30)) { Type = DocumentType.Invoice, SequenceNumber = 07, PayerId = Payers[3].Id }, // Maria Lopez
                new BillingDocument(new DateTime(2025, 05, 31)) { Type = DocumentType.Invoice, SequenceNumber = 03, PayerId = Payers[3].Id }, // Maria Lopez
                new BillingDocument(new DateTime(2026, 01, 31)) { Type = DocumentType.Ticket,  SequenceNumber = 01, PayerId = Payers[3].Id }, // Maria Lopez

                new BillingDocument(new DateTime(2024, 10, 31)) { Type = DocumentType.Ticket,  SequenceNumber = 03, PayerId = Payers[4].Id }, // Luca Rossi
                new BillingDocument(new DateTime(2025, 06, 30)) { Type = DocumentType.Invoice, SequenceNumber = 05, PayerId = Payers[4].Id }, // Luca Rossi
                new BillingDocument(new DateTime(2026, 02, 28)) { Type = DocumentType.Invoice, SequenceNumber = 01, PayerId = Payers[4].Id }, // Luca Rossi

                new BillingDocument(new DateTime(2024, 11, 30)) { Type = DocumentType.Invoice, SequenceNumber = 08, PayerId = Payers[5].Id }, // Emma Brown
                new BillingDocument(new DateTime(2025, 07, 31)) { Type = DocumentType.Ticket,  SequenceNumber = 06, PayerId = Payers[5].Id }, // Emma Brown
                new BillingDocument(new DateTime(2026, 03, 31)) { Type = DocumentType.Invoice, SequenceNumber = 02, PayerId = Payers[5].Id }, // Emma Brown

                new BillingDocument(new DateTime(2024, 12, 31)) { Type = DocumentType.Invoice, SequenceNumber = 09, PayerId = Payers[6].Id }, // Noah Muller
                new BillingDocument(new DateTime(2025, 08, 31)) { Type = DocumentType.Invoice, SequenceNumber = 06, PayerId = Payers[6].Id }, // Noah Muller
                new BillingDocument(new DateTime(2026, 04, 30)) { Type = DocumentType.Ticket,  SequenceNumber = 02, PayerId = Payers[6].Id }, // Noah Muller

                new BillingDocument(new DateTime(2025, 01, 31)) { Type = DocumentType.Ticket,  SequenceNumber = 01, PayerId = Payers[7].Id }, // Olivia Dubois
                new BillingDocument(new DateTime(2025, 09, 30)) { Type = DocumentType.Invoice, SequenceNumber = 07, PayerId = Payers[7].Id }, // Olivia Dubois
                new BillingDocument(new DateTime(2026, 05, 31)) { Type = DocumentType.Invoice, SequenceNumber = 03, PayerId = Payers[7].Id }, // Olivia Dubois
            ];

            ArchiveBills =
            [
                new BillingDocument(new DateTime(2024, 03, 15)) { Type = DocumentType.Invoice, SequenceNumber = 01, PayerId = ArchivePayers[0].Id }, // Aiden Clarck
                new BillingDocument(new DateTime(2025, 12, 15)) { Type = DocumentType.Ticket,  SequenceNumber = 08, PayerId = ArchivePayers[0].Id }, // Aiden Clarck

                new BillingDocument(new DateTime(2024, 04, 30)) { Type = DocumentType.Invoice, SequenceNumber = 02, PayerId = ArchivePayers[1].Id }, // Sofia Martinez
                new BillingDocument(new DateTime(2025, 03, 30)) { Type = DocumentType.Ticket,  SequenceNumber = 02, PayerId = ArchivePayers[1].Id },// Sofia Martinez

                new BillingDocument(new DateTime(2024, 05, 30)) { Type = DocumentType.Invoice, SequenceNumber = 03, PayerId = ArchivePayers[2].Id }, // Yuki Tanaza
                new BillingDocument(new DateTime(2025, 04, 30)) { Type = DocumentType.Ticket,  SequenceNumber = 03, PayerId = ArchivePayers[2].Id }, // Yuki Tanaza

                new BillingDocument(new DateTime(2024, 09, 15)) { Type = DocumentType.Invoice, SequenceNumber = 06, PayerId = ArchivePayers[3].Id }, // Oliver Wilson
                new BillingDocument(new DateTime(2025, 05, 30)) { Type = DocumentType.Ticket,  SequenceNumber = 05, PayerId = ArchivePayers[3].Id }, // Oliver Wilson

                new BillingDocument(new DateTime(2024, 10, 15)) { Type = DocumentType.Ticket,  SequenceNumber = 02, PayerId = ArchivePayers[4].Id }, // Fatima Al-Farsi
                new BillingDocument(new DateTime(2025, 06, 15)) { Type = DocumentType.Invoice, SequenceNumber = 04, PayerId = ArchivePayers[4].Id }, // Fatima Al-Farsi
            ];

            Lessons =
            [
                // ----------- date --- yyyy  mm  dd -- lesson name --------------- paid -- student ------- bill ---------- p/h --  min --- price - online travel - hols - fee - tip ----- notes

                new Lesson(new DateOnly(2024, 06, 10), "Math Tutoring - Alice",     true,   Students[0].Id, Bills[0].Id,    true,   60,     40.00m, true,   5,      false,  0,      2,      null), // John Doe      - 40
                new Lesson(new DateOnly(2024, 06, 15), "Science Tutoring - Bob",    true,   Students[1].Id, Bills[0].Id,    true,   90,     45.00m, false,  5,      true,   0,      0,      null), // John Doe      - 72.5 
                new Lesson(new DateOnly(2024, 10, 05), "Math Tutoring - Alice",     true,   Students[0].Id, Bills[1].Id,    true,   60,     42.00m, true,   5,      false,  0,      2,      null), // John Doe      - 42
                new Lesson(new DateOnly(2024, 10, 20), "Science Tutoring - Bob",    true,   Students[1].Id, Bills[1].Id,    true,   90,     45.00m, false,  5,      false,  0,      0,      null), // John Doe      - 72.5
                new Lesson(new DateOnly(2025, 03, 05), "Math Tutoring - Alice",     true,   Students[0].Id, Bills[2].Id,    true,   90,     40.00m, true,   7.5m,   false,  0,      2,      null), // John Doe      - 60
                new Lesson(new DateOnly(2025, 03, 20), "Science Tutoring - Bob",    true,   Students[1].Id, Bills[2].Id,    true,   60,     45.00m, false,  7.5m,   false,  0,      0,      null), // John Doe      - 52.5
                new Lesson(new DateOnly(2025, 08, 03), "Math Tutoring - Alice",     true,   Students[0].Id, Bills[2].Id,    true,   60,     42.00m, true,   7.5m,   false,  0,      2,      null), // John Doe      - 42
                new Lesson(new DateOnly(2025, 08, 18), "Science Tutoring - Bob",    true,   Students[1].Id, Bills[2].Id,    true,   90,     45.00m, false,  7.5m,   true,   0,      0,      null), // John Doe      - 75

                new Lesson(new DateOnly(2024, 07, 01), "English Lessons - Charlie", true,   Students[2].Id, Bills[3].Id,    true,   60,     35.00m, true,   5,      false,  0,      3,      null), // Jane Smith    - 35
                new Lesson(new DateOnly(2024, 07, 12), "Physics Coaching - Diana",  true,   Students[3].Id, Bills[3].Id,    true,   120,    45.00m, false,  5,      true,   0,      5,      null), // Jane Smith    - 95
                new Lesson(new DateOnly(2024, 11, 01), "English Lessons - Charlie", true,   Students[2].Id, Bills[4].Id,    true,   60,     35.00m, true,   5,      false,  0,      1,      null), // Jane Smith    - 35
                new Lesson(new DateOnly(2024, 11, 15), "Physics Coaching - Diana",  true,   Students[3].Id, Bills[4].Id,    true,   120,    48.00m, false,  5,      true,   0,      4,      null), // Jane Smith    - 101 
                new Lesson(new DateOnly(2025, 04, 02), "English Lessons - Charlie", true,   Students[2].Id, Bills[5].Id,    true,   60,     35.00m, true,   7.5m,   false,  0,      2,      null), // Jane Smith    - 35
                new Lesson(new DateOnly(2025, 04, 15), "Physics Coaching - Diana",  true,   Students[3].Id, Bills[5].Id,    true,   120,    50.00m, false,  7.5m,   true,   0,      5,      null), // Jane Smith    - 107.5
                new Lesson(new DateOnly(2025, 09, 01), "English Lessons - Charlie", true,   Students[2].Id, Bills[5].Id,    true,   60,     35.00m, true,   7.5m,   false,  0,      2,      null), // Jane Smith    - 35  
                new Lesson(new DateOnly(2025, 09, 15), "Physics Coaching - Diana",  true,   Students[3].Id, Bills[5].Id,    true,   120,    50.00m, false,  7.5m,   true,   0,      4,      null), // Jane Smith    - 107.5

                new Lesson(new DateOnly(2024, 07, 20), "Programming Basics - Luis", true,   Students[4].Id, Bills[6].Id,    true,   90,     60.00m, true,   5,      false,  0,      0,      null), // Carlos Gomez  - 90
                new Lesson(new DateOnly(2024, 12, 01), "Programming Basics - Luis", true,   Students[4].Id, Bills[7].Id,    true,   90,     60.00m, true,   5,      false,  0,      0,      null), // Carlos Gomez  - 90  
                new Lesson(new DateOnly(2025, 05, 3), "Programming Basics - Luis",  true,   Students[4].Id, Bills[8].Id,    true,   90,     60.00m, true,   7.5m,   false,  0,      0,      null), // Carlos Gomez  - 90
                new Lesson(new DateOnly(2026, 01, 10), "Programming Basics - Luis", false,  Students[4].Id, null,           true,   90,     60.00m, true,   5,      false,  5,      0,      null), // Carlos Gomez  - 90

                new Lesson(new DateOnly(2024, 08, 05), "Spanish Practice - Sofia",  true,   Students[5].Id, Bills[9].Id,    false,  null,   35.00m, true,   5,      false,  0,      1.5m,   null), // Maria Lopez   - 35
                new Lesson(new DateOnly(2024, 12, 18), "Spanish Practice - Sofia",  true,   Students[5].Id, Bills[10].Id,   false,  null,   35.00m, true,   5,      false,  0,      2,      null), // Maria Lopez   - 35
                new Lesson(new DateOnly(2025, 05, 18), "Spanish Practice - Sofia",  true,   Students[5].Id, Bills[10].Id,   false,  null,   35.00m, true,   7.5m,   false,  0,      1,      null), // Maria Lopez   - 35
                new Lesson(new DateOnly(2026, 01, 25), "Spanish Practice - Sofia",  false,  Students[5].Id, Bills[11].Id,   false,  null,   35.00m, true,   10,     false,  5,      2,      null), // Maria Lopez   - 35

                new Lesson(new DateOnly(2024, 08, 18), "Fitness Session - Marco",   true,   Students[6].Id, Bills[12].Id,   true,   60,     50.00m, false,  5,      true,   0,      2,      null), // Luca Rossi    - 55
                new Lesson(new DateOnly(2025, 01, 05), "Fitness Session - Marco",   true,   Students[6].Id, Bills[13].Id,   true,   60,     50.00m, false,  7.5m,   false,  0,      2,      null), // Luca Rossi    - 57.5
                new Lesson(new DateOnly(2025, 06, 02), "Fitness Session - Marco",   true,   Students[6].Id, Bills[13].Id,   true,   60,     50.00m, false,  7.5m,   true,   0,      2,      null), // Luca Rossi    - 57.5
                new Lesson(new DateOnly(2026, 02, 05), "Fitness Session - Marco",   false,  Students[6].Id, Bills[14].Id,   true,   60,     50.00m, false,  10,     true,   5,      3,      null), // Luca Rossi    - 65  

                new Lesson(new DateOnly(2024, 09, 02), "Business Coaching - Emily", true,   Students[7].Id, Bills[15].Id,   true,   120,    50.00m, true,   5,      false,  0,      0,      null), // Emma Brown    - 100
                new Lesson(new DateOnly(2025, 01, 15), "Business Coaching - Emily", true,   Students[7].Id, Bills[16].Id,   true,   120,    55.00m, true,   7.5m,   false,  0,      0,      null), // Emma Brown    - 110
                new Lesson(new DateOnly(2025, 06, 20), "Business Coaching - Emily", true,   Students[7].Id, Bills[16].Id,   true,   120,    55.00m, true,   7.5m,   false,  0,      0,      null), // Emma Brown    - 110
                new Lesson(new DateOnly(2026, 02, 20), "Business Coaching - Emily", false,  Students[7].Id, Bills[17].Id,   true,   120,    55.00m, true,   10,     false,  5,      0,      null), // Emma Brown    - 110

                new Lesson(new DateOnly(2024, 09, 10), "German Lessons - Leon",     true,   Students[8].Id, Bills[18].Id,   true,   60,     35.00m, true,   5,      false,  0,      1,      null), // Noah Muller   - 35
                new Lesson(new DateOnly(2025, 02, 02), "German Lessons - Leon",     true,   Students[8].Id, Bills[19].Id,   true,   60,     35.00m, true,   7.5m,   false,  0,      1,      null), // Noah Muller   - 35
                new Lesson(new DateOnly(2025, 07, 05), "German Lessons - Leon",     true,   Students[8].Id, Bills[19].Id,   true,   60,     35.00m, true,   7.5m,   false,  0,      2,      null), // Noah Muller   - 35
                new Lesson(new DateOnly(2026, 03, 05), "German Lessons - Leon",     false,  Students[8].Id, Bills[20].Id,   true,   60,     35.00m, true,   10,     false,  5,      1,      null), // Noah Muller   - 35

                new Lesson(new DateOnly(2024, 09, 25), "French Lessons - Chloe",    true,   Students[9].Id, Bills[21].Id,   true,   60,     35.00m, false,  5,      true,   0,      2,      null), // Olivia Dubois - 40
                new Lesson(new DateOnly(2025, 02, 18), "French Lessons - Chloe",    true,   Students[9].Id, Bills[22].Id,   true,   60,     35.00m, false,  7.5m,   true,   0,      3,      null), // Olivia Dubois - 42.5
                new Lesson(new DateOnly(2025, 07, 22), "French Lessons - Chloe",    true,   Students[9].Id, Bills[22].Id,   true,   60,     35.00m, false,  7.5m,   true,   0,      3,      null), // Olivia Dubois - 42.5
                new Lesson(new DateOnly(2026, 03, 18), "French Lessons - Chloe",    false,  Students[9].Id, Bills[23].Id,   true,   60,     35.00m, false,  10,     true,   5,      2,      null), // Olivia Dubois - 50

                new Lesson(new DateOnly(2026, 04, 01), "French Lessons - Chloe",    false,  Students[9].Id, null,           true,   60,     37.5m, false,  10,      true,   5,      2,      null), // Olivia Dubois - 52.5
                new Lesson(new DateOnly(2026, 04, 10), "French Lessons - Chloe",    false,  Students[9].Id, null,           true,   60,     37.5m, false,  10,      true,   5,      3,      null), // Olivia Dubois - 52.5
                new Lesson(new DateOnly(2026, 04, 15), "French Lessons - Chloe",    false,  Students[9].Id, null,           true,   60,     37.5m, false,  10,      true,   5,      3,      null), // Olivia Dubois - 52.5
                new Lesson(new DateOnly(2026, 04, 23), "French Lessons - Chloe",    false,  Students[9].Id, null,           true,   60,     37.5m, false,  10,      true,   5,      2,      null), // Olivia Dubois - 52.5
            ];

            ArchiveLessons =
            [
                // ----------- date --- yyyy  mm  dd -- lesson name ------------------- paid -- student ------------- bill -------------- p/h ---  min --- price - online travel -- hols - fee - tip -- notes
                new Lesson(new DateOnly(2024, 03, 10), "Math Tutoring - Ethan",         true,  ArchiveStudents[0].Id, ArchiveBills[0].Id, true,     60,     37.5m,  true,   5,      false,  0,    2,    null), // Aiden Clarck
                new Lesson(new DateOnly(2024, 03, 15), "Science Tutoring - Mia",        true,  ArchiveStudents[1].Id, ArchiveBills[0].Id, true,     90,     29.5m,  false,  5,      true,   0,    0,    null), // Aiden Clarck
                new Lesson(new DateOnly(2024, 03, 05), "Chemist Tutoring - Lucas",      true,  ArchiveStudents[2].Id, ArchiveBills[0].Id, true,     60,     42.00m, true,   5,      false,  0,    2,    null), // Aiden Clarck
                new Lesson(new DateOnly(2025, 06, 10), "Math Tutoring - Ethan",         true,  ArchiveStudents[0].Id, ArchiveBills[1].Id, true,     60,     37.5m,  true,   7.5m,   false,  0,    2,    null), // Aiden Clarck
                new Lesson(new DateOnly(2025, 06, 15), "Science Tutoring - Mia",        true,  ArchiveStudents[1].Id, ArchiveBills[1].Id, true,     90,     29.5m,  false,  7.5m,   true,   0,    0,    null), // Aiden Clarck
                new Lesson(new DateOnly(2025, 10, 05), "Chemist Tutoring - Lucar",      true,  ArchiveStudents[2].Id, ArchiveBills[1].Id, true,     60,     42.00m, true,   7.5m,   false,  0,    2,    null), // Aiden Clarck

                new Lesson(new DateOnly(2024, 04, 01), "English Lessons - Valentina",   true,  ArchiveStudents[3].Id, ArchiveBills[2].Id, true,     60,     35.00m, true,   5,      false,  0,    3,    null), // Sofia Martinez
                new Lesson(new DateOnly(2024, 03, 12), "English Lessons - Diego",       true,  ArchiveStudents[4].Id, ArchiveBills[2].Id, true,     120,    29.5m,  false,  5,      true,   0,    5,    null), // Sofia Martinez
                new Lesson(new DateOnly(2024, 01, 01), "English Lessons - Valentina",   true,  ArchiveStudents[3].Id, ArchiveBills[2].Id, true,     60,     35.00m, true,   5,      false,  0,    1,    null), // Sofia Martinez
                new Lesson(new DateOnly(2024, 02, 15), "English Lessons - Diego",       true,  ArchiveStudents[4].Id, ArchiveBills[2].Id, true,     120,    48.00m, false,  5,      true,   0,    4,    null), // Sofia Martinez
                new Lesson(new DateOnly(2025, 04, 02), "English Lessons - Valentina",   true,  ArchiveStudents[3].Id, ArchiveBills[3].Id, true,     60,     35.00m, true,   7.5m,   false,  0,    2,    null), // Sofia Martinez
                new Lesson(new DateOnly(2025, 04, 15), "English Lessons - Diego",       true,  ArchiveStudents[4].Id, ArchiveBills[3].Id, true,     120,    50.00m, false,  7.5m,   true,   0,    5,    null), // Sofia Martinez
                new Lesson(new DateOnly(2025, 09, 01), "English Lessons - Valentina",   true,  ArchiveStudents[3].Id, ArchiveBills[3].Id, true,     60,     35.00m, true,   7.5m,   false,  0,    2,    null), // Sofia Martinez
                new Lesson(new DateOnly(2025, 09, 15), "English Lessons - Diego",       true,  ArchiveStudents[4].Id, ArchiveBills[3].Id, true,     120,    50.00m, false,  7.5m,   true,   0,    4,    null), // Sofia Martinez

                new Lesson(new DateOnly(2024, 07, 20), "Programming Basics - Hana",     true,  ArchiveStudents[5].Id, ArchiveBills[4].Id, true,     90,     60.00m, true,   5,      false,  0,    0,    null), // Yuki Tanaza
                new Lesson(new DateOnly(2024, 12, 01), "Programming Basics - Hana",     true,  ArchiveStudents[5].Id, ArchiveBills[4].Id, true,     90,     60.00m, true,   5,      false,  0,    0,    null), // Yuki Tanaza
                new Lesson(new DateOnly(2025, 05, 03), "Programming Basics - Hana",     true,  ArchiveStudents[5].Id, ArchiveBills[5].Id, true,     90,     60.00m, true,   7.5m,   false,  0,    0,    null), // Yuki Tanaza

                new Lesson(new DateOnly(2024, 08, 05), "Spanish Practice - Noah",       true,  ArchiveStudents[6].Id, ArchiveBills[6].Id, false,    null,   35.00m, true,   5,      false,  0,    1.5m, null), // Oliver Wilson
                new Lesson(new DateOnly(2024, 12, 18), "Spanish Practice - Noah",       true,  ArchiveStudents[6].Id, ArchiveBills[6].Id, false,    null,   35.00m, true,   5,      false,  0,    2,    null), // Oliver Wilson
                new Lesson(new DateOnly(2025, 05, 18), "Spanish Practice - Noah",       true,  ArchiveStudents[6].Id, ArchiveBills[7].Id, false,    null,   35.00m, true,   7.5m,   false,  0,    1,    null), // Oliver Wilson

                new Lesson(new DateOnly(2024, 08, 18), "Fitness Session - Layla",       true,  ArchiveStudents[7].Id, ArchiveBills[8].Id, true,     60,     50.00m, false,  5,      true,   0,    2,    null), // Fatima Al-Farsi
                new Lesson(new DateOnly(2025, 01, 05), "Fitness Session - Layla",       true,  ArchiveStudents[7].Id, ArchiveBills[9].Id, true,     60,     50.00m, false,  7.5m,   false,  0,    2,    null), // Fatima Al-Farsi
                new Lesson(new DateOnly(2025, 06, 02), "Fitness Session - Layla",       true,  ArchiveStudents[7].Id, ArchiveBills[9].Id, true,     60,     50.00m, false,  7.5m,   true,   0,    2,    null), // Fatima Al-Farsi
            ];
        }

    }
    public static class Helper
    {
        public static string GetFullName(string firstName, string lastName) => $"{firstName} {lastName}".Trim();

        // Service
        public static ServiceSummary ConvertToServiceSummary(Service service) =>
            new(service.Id, service.Name, service.IsPricePerHour, (double)service.Price);

        public static Expression<Func<Service, ServiceSummary>> AsServiceSummary =>
            s => ConvertToServiceSummary(s);

        // Payer
        public static PayerOption ConvertToPayerOption(Payer payer) =>
            new(payer.Id, payer.FullName);

        public static PayerOption ConvertToPayerOptionWithCount(Payer p, int c) =>
            new(p.Id, c > 0
                ? $"{p.FirstName} {p.LastName} - {c} lesson{(c == 1 ? "" : "s")}"
                : $"{p.FirstName} {p.LastName}");

        public static PayerActivityInfo ConvertToPayerActivityInfo(Payer payer, List<Lesson> lessons) 
            => new()
            {
            PayerId = payer.Id,
            PayerName = payer.FullName,
            LastLessonDate = lessons.Max(l => (DateOnly?) l.Date)
        };

        public static PayerSummary ConvertToPayerSummary(Payer payer, decimal outstanding) =>
            new(payer.Id, payer.FirstName, payer.LastName, outstanding, payer.Address, payer.ZipCode, payer.City, payer.TaxId);

        // Students
        public static StudentOption ConvertToStudentOption(Student student) =>
            new(student.Id, student.FullName);

        public static StudentSummary ConvertToStudentSummary(Student s, Payer p) =>
            new(s.Id, s.FirstName, s.LastName, p.Id, p.FullName);

        // Specifications
        public static SpecificationOption ConvertToSpecificationOption(Specification s)
            => new(s.Id, s.Name, s.ServiceId, (double?)s.Price, s.DurationMinutes, s.IsOnline, s.IsWeekendOrHoliday);

        public static SpecificationSummary ConvertToSpecificationSummary(Specification spec, Student student, Service service) =>
            new(spec.Id, spec.Name, student.Id, student.FullName, service.Id, service.Name, spec.DurationMinutes, (double?)spec.Price, spec.IsOnline, spec.IsWeekendOrHoliday, spec.UsageCount);

        // Invoices
        public static BillSummary ConverToBillSumary(BillingDocument? b) => b is null ?
            new BillSummary(null, Guid.NewGuid(), DocumentType.Invoice, 0, string.Empty, new DateTime()) :
            new BillSummary(b.Id, b.PayerId, b.Type, b.SequenceNumber, b.DocumentNumber, b.CreatedUTC);

        // Lessons
        public static LessonLine ConvertToLessonLine(Lesson l, Student s) =>
            new(l.Id, l.StudentId, l.Date, l.Name, s.FullName, l.FinalPrice, l.IsPaid, l.DurationMinutes);
        public static LessonSummary ConvertToLessonSummary(Lesson l, Student? s, BillingDocument? b) 
            => new(
                l.Id,
                l.Date,
                l.Name,
                l.FinalPrice,
                l.IsPaid,
                l.StudentId,
                s == null ? string.Empty : s.FullName,
                l.BillingDocumentId,
                b == null ? string.Empty : b.DocumentNumber,
                l.IsPricePerHour,
                l.DurationMinutes,
                l.BasePrice,
                l.IsOnline,
                l.TravelAllowance,
                l.IsWeekendOrHoliday,
                l.WeekendFee,
                l.Tip,
                l.Notes);
    }
    public interface ISummary
    {
        Guid Id { get; }
        string Name { get; }
    }
}
