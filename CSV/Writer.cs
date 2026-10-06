using CsvHelper;
using Models;
using System.Globalization;

namespace CSV
{
    public interface IWriter
    {
        void WriteCSV(in string folder, in (List<Service> services, List<Payer> payers,
            List<Student> students, List<Specification> specifications, List<Lesson> lessons, 
            List<BillingDocument> bills) data, bool archive = false);
    }

    public class Writer : IWriter
    {
        public void WriteCSV(in string folder, in (List<Service> services, 
            List<Payer> payers, List<Student> students, List<Specification> specifications,
            List<Lesson> lessons, List<BillingDocument> bills) data, bool archive = false)
        {
            string destinationFolder = Path.Combine(folder, $"Apolo_{(archive ? "Archive" : "Export")}_{DateTime.Now:yyyyMMdd_HHmmss}");
            
            try
            {
                Directory.CreateDirectory(destinationFolder);

                WriteServices(destinationFolder, data.services);
                WritePayers(destinationFolder, data.payers);
                WriteStudents(destinationFolder, data.students, data.payers);
                if (!archive) 
                    WriteSpecifications(destinationFolder, data.specifications, data.students, data.services);
                WriteLessons(destinationFolder, data.lessons, data.students, data.bills);
                WriteInvoices(destinationFolder, data.bills, data.payers, data.lessons);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error writing CSV files: {ex.Message}");
            }
        }

        private static void WriteServices(string folder, List<Service> services)
        {
            using var sw = new StreamWriter(Path.Combine(folder, "Services.csv"));
            using var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);
            csv.WriteRecords(services);
        }

        private static void WritePayers(string folder, List<Payer> payers)
        {
            using var sw = new StreamWriter(Path.Combine(folder, "Payers.csv"));
            using var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);
            csv.WriteRecords(payers);
        }

        private static void WriteStudents(string folder, List<Student> students, List<Payer> payers)
        {
            var payerLookup = payers.ToDictionary(p => p.Id, p => p);

            var exportData = students.Select(student => new
            {
                FirstName = student.FirstName,
                LastName = student.LastName,
                PayerName = payerLookup.TryGetValue(student.PayerId, out var payer) ? payer.FullName : "",
                Id = student.Id,
                PayerId = student.PayerId
            });

            using var sw = new StreamWriter(Path.Combine(folder, "Students.csv"));
            using var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);
            csv.WriteRecords(exportData);
        }

        private static void WriteSpecifications(string folder, List<Specification> specifications, List<Student> students, List<Service> services)
        {
            var studentLookup = students.ToDictionary(s => s.Id, s => s);
            var serviceLookup = services.ToDictionary(s => s.Id, s => s);

            var exportData = specifications.Select(spec => new
            {
                Name = spec.Name,
                StudentName = studentLookup.TryGetValue(spec.StudentId, out var stu) ? stu.FullName : "",
                ServiceName = serviceLookup.TryGetValue(spec.ServiceId, out var srv) ? srv.Name : "",
                DurationMinutes = spec.DurationMinutes,
                Price = spec.Price,
                IsOnline = spec.IsOnline,
                IsWeekendOrHoliday = spec.IsWeekendOrHoliday,
                UsageCount = spec.UsageCount,
                Id = spec.Id,
                StudentId = spec.StudentId,
                ServiceId = spec.ServiceId
            });

            using var sw = new StreamWriter(Path.Combine(folder, "Specifications.csv"));
            using var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);
            csv.WriteRecords(exportData);
        }

        private static void WriteLessons(string folder, List<Lesson> lessons, List<Student> students, List<BillingDocument> bills)
        {
            var studentLookup = students.ToDictionary(s => s.Id, s => s);
            var billLookup = bills.ToDictionary(s => s.Id, b => b);

            var exportData = lessons.Select(lesson => new
            {
                Id = lesson.Id,
                Date = lesson.Date.ToString("dd-MM-yyyy"),
                Name = lesson.Name,
                StudentName = studentLookup.TryGetValue(lesson.StudentId, out var stu) ? stu.FullName : "",
                FinalPrice = lesson.FinalPrice,
                IsPaid = lesson.IsPaid,
                Notes = lesson.Notes ?? string.Empty,
                BillName = lesson.BillingDocumentId.HasValue && billLookup.TryGetValue(lesson.BillingDocumentId.Value, out var b) ? b.DocumentNumber : "",
                IsPricePerHour = lesson.IsPricePerHour,
                DurationMinutes = lesson.DurationMinutes,
                BasePrice = lesson.BasePrice,
                IsOnline = lesson.IsOnline,
                TravelAllowance = lesson.TravelAllowance,
                IsWeekendOrHoliday = lesson.IsWeekendOrHoliday,
                WeekendFee = lesson.WeekendFee,
                Tip = lesson.Tip,
                StudentId = lesson.StudentId,
                BillId = lesson.BillingDocumentId
            });

            using var sw = new StreamWriter(Path.Combine(folder, "Lessons.csv"));
            using var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);
            csv.WriteRecords(exportData);
        }

        private static void WriteInvoices(string folder, List<BillingDocument> bills, List<Payer> payers, List<Lesson> lessons)
        {
            var billLookup = lessons
                .Where(l => l.BillingDocumentId.HasValue)
                .GroupBy(l => l.BillingDocumentId!.Value)
                .ToDictionary(group => group.Key, group => group.Sum(l => l.FinalPrice));
            var payerLookup = payers.ToDictionary(p => p.Id, p => p.FullName);

            var exportData = bills.Select(bill => new
            {
                DocumentNumber = bill.DocumentNumber,
                Type = bill.Type,
                CreatedUTC = bill.CreatedUTC.ToString("dd-MM-yyyy HH:mm:ss"),
                PayerName = payerLookup.TryGetValue(bill.PayerId, out var pName) ? pName : "",
                Total = billLookup.TryGetValue(bill.Id, out var total) ? total : 0m,
                PayerId = bill.PayerId,
                Id = bill.Id,
                SequenceNumber = bill.SequenceNumber
            });

            using var sw = new StreamWriter(Path.Combine(folder, "Bills.csv"));
            using var csv = new CsvWriter(sw, CultureInfo.InvariantCulture);
            csv.WriteRecords(exportData);
        }
    }
}
