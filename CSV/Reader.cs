using CsvHelper;
using CsvHelper.Configuration;
using Models;

namespace CSV
{
    public interface IReader
    {
        public List<Service> Services { get; } 
        public List<Payer> Payers { get; } 
        public List<Student> Students { get; } 
        public List<Specification> Specifications { get; } 
        public List<Lesson> Lessons { get; } 
        public List<BillingDocument> Bills { get; }
        Task ReadCSV(string filePath, bool isArchive); 
    }

    public class Reader : IReader
    {
        public List<Service> Services { get; } = [];
        public List<Payer> Payers { get; } = [];
        public List<Student> Students { get; } = [];
        public List<Specification> Specifications { get; } = [];
        public List<Lesson> Lessons { get; } = [];
        public List<BillingDocument> Bills { get; } = [];

        readonly private CsvConfiguration Config = new(System.Globalization.CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
            HeaderValidated = null
        };

        public Task ReadCSV(string filePath, bool isArchive)
        {
            return Task.Run(() =>
            {
                try
                {
                    ReadServices(filePath);
                    ReadPayers(filePath);
                    ReadStudents(filePath);
                    if (!isArchive)
                        ReadSpecification(filePath);
                    ReadBills(filePath);
                    ReadLessons(filePath);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error reading CSV files: {ex.Message}");
                }
            });
        }

        private static string GenerateFullPath(string folder, string filename)
        {
            var path = Path.Combine(folder, $"{filename}.csv");
            if (!File.Exists(path))
                throw new ArgumentException($"Missing '{filename}.csv' in the folder '{folder}'.");
            return path;
        }

        private void ReadServices(string folder)
        {
            var path = GenerateFullPath(folder, "Services");

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, Config);
            csv.Context.RegisterClassMap<ServiceMap>();

            Services.AddRange([.. csv.GetRecords<Service>()]);
        }

        private void ReadPayers(string folder)
        {
            var path = GenerateFullPath(folder, "Payers");

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, Config);
            csv.Context.RegisterClassMap<PayerMap>();

            Payers.AddRange([.. csv.GetRecords<Payer>()]);
        }

        private void ReadStudents(string folder)
        {
            var path = GenerateFullPath(folder, "Students");

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, Config);
            csv.Context.RegisterClassMap<StudentMap>();

            Students.AddRange([.. csv.GetRecords<Student>()]);
        }

        private void ReadSpecification(string folder)
        {
            var path = GenerateFullPath(folder, "Specifications");

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, Config);
            csv.Context.RegisterClassMap<SpecificationMap>();

            Specifications.AddRange([.. csv.GetRecords<Specification>()]);
        }

        private void ReadLessons(string folder)
        {
            var path = GenerateFullPath(folder, "Lessons");

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, Config);
            csv.Context.RegisterClassMap<LessonMap>();
            var lessons = csv.GetRecords<LessonData>();

            Lessons.AddRange([.. lessons.Select(l => new Lesson(l.Date, l.Name, l.IsPaid, l.StudentId, l.BillingDocumentId,
                l.IsPricePerHour, l.DurationMinutes, l.BasePrice, l.IsOnline, l.TravelAllowance, l.IsWeekendOrHoliday,
                l.WeekendFee, l.Tip, l.Notes) 
            {
                Id = l.Id
            })]);
        }

        private void ReadBills(string folder)
        {
            var path = GenerateFullPath(folder, "Bills");

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, Config);
            csv.Context.RegisterClassMap<BillMap>();
            var bills = csv.GetRecords<BillData>();

            Bills.AddRange([.. bills.Select(b => new BillingDocument(b.CreatedUTC) 
            { 
                Id = b.Id, 
                Type = b.Type,
                PayerId = b.PayerId,
                SequenceNumber = b.SequenceNumber
            })]);
        }
    }
}
