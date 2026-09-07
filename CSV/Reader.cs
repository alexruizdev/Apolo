using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CSV
{
    public interface IReader
    {
        public List<Service> Services { get; } 
        public List<Payer> Payers { get; } 
        public List<Student> Students { get; } 
        public List<Specification> Specifications { get; } 
        public List<Lesson> Lessons { get; } 
        public List<BillingDocument> Invoices { get; }
        Task ReadExcel(string filePath); // Keeping the name for now, but filePath will be a folder
    }

    public class Reader : IReader
    {
        public List<Service> Services { get; } = new List<Service>();
        public List<Payer> Payers { get; } = new List<Payer>();
        public List<Student> Students { get; } = new List<Student>();
        public List<Specification> Specifications { get; } = new List<Specification>();
        public List<Lesson> Lessons { get; } = new List<Lesson>();
        public List<BillingDocument> Invoices { get; } = new List<BillingDocument>();

        private string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string currentField = "";
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                    {
                        currentField += '\"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField);
                    currentField = "";
                }
                else
                {
                    currentField += c;
                }
            }
            result.Add(currentField);
            return result.ToArray();
        }

        private DateTime ReadDate(string dateRawString)
        {
            dateRawString = dateRawString.Trim();
            string[] formats = { "dd-MM-yyyy", "dd-MM-yyyy HH:mm:ss" };
            if (DateTime.TryParseExact(dateRawString, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime dtValue))
            {
                return dtValue;
            }
            throw new InvalidDataException($"Cell has an invalid date format: '{dateRawString}'. Expected 'yyyy-MM-dd'.");
        }

        public Task ReadExcel(string filePath)
        {
            return Task.Run(() =>
            {
                try
                {
                    ReadServices(filePath);
                    ReadPayers(filePath);
                    ReadStudents(filePath);
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

        private void ReadServices(string folder)
        {
            var path = Path.Combine(folder, "Services.csv");
            if (!File.Exists(path)) return;
            var lines = File.ReadAllLines(path).Skip(1);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = SplitCsvLine(line);
                Services.Add(new Service()
                {
                    Name = cols[0],
                    Price = decimal.TryParse(cols[1], out var p) ? p : 0,
                    IsPricePerHour = cols[2].ToLower() == "true",
                    Id = Guid.Parse(cols[3])
                });
            }
        }

        private void ReadPayers(string folder)
        {
            var path = Path.Combine(folder, "Payers.csv");
            if (!File.Exists(path)) return;
            var lines = File.ReadAllLines(path).Skip(1);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = SplitCsvLine(line);
                Payers.Add(new Payer()
                {
                    FirstName = cols[0],
                    LastName = cols[1],
                    Address = cols[2],
                    ZipCode = cols[3],
                    City = cols[4],
                    TaxId = cols[5],
                    Id = Guid.Parse(cols[6])
                });
            }
        }

        private void ReadStudents(string folder)
        {
            var path = Path.Combine(folder, "Students.csv");
            if (!File.Exists(path)) return;
            var lines = File.ReadAllLines(path).Skip(1);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = SplitCsvLine(line);
                Students.Add(new Student()
                {
                    FirstName = cols[0],
                    LastName = cols[1],
                    Id = Guid.Parse(cols[3]),
                    PayerId = Guid.Parse(cols[4])
                });
            }
        }

        private void ReadSpecification(string folder)
        {
            var path = Path.Combine(folder, "Specifications.csv");
            if (!File.Exists(path)) return;
            var lines = File.ReadAllLines(path).Skip(1);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = SplitCsvLine(line);
                Specifications.Add(new Specification()
                {
                    Name = cols[0],
                    DurationMinutes = int.TryParse(cols[3], out var d) ? d : 0,
                    Price = decimal.TryParse(cols[4], out var p) ? p : null,
                    IsOnline = cols[5].ToLower() == "true",
                    IsWeekendOrHoliday = cols[6].ToLower() == "true",
                    UsageCount = int.TryParse(cols[7], out var u) ? u : 0,
                    Id = Guid.Parse(cols[8]),
                    StudentId = Guid.Parse(cols[9]),
                    ServiceId = Guid.Parse(cols[10])
                });
            }
        }

        private void ReadLessons(string folder)
        {
            var path = Path.Combine(folder, "Lessons.csv");
            if (!File.Exists(path)) return;
            var lines = File.ReadAllLines(path).Skip(1);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = SplitCsvLine(line);
                var bId = Guid.TryParse(cols[17], out var id) ? (Guid?)id : null;
                Lessons.Add(new Lesson(
                    DateOnly.FromDateTime(ReadDate(cols[0])),
                    cols[1],
                    cols[4].ToLower() == "true",
                    Guid.Parse(cols[16]),
                    bId,
                    cols[7].ToLower() == "true",
                    int.TryParse(cols[8], out var d) ? d : 0,
                    decimal.TryParse(cols[9], out var bp) ? bp : 0,
                    cols[10].ToLower() == "true",
                    decimal.TryParse(cols[11], out var ta) ? ta : 0,
                    cols[12].ToLower() == "true",
                    decimal.TryParse(cols[13], out var wf) ? wf : 0,
                    decimal.TryParse(cols[14], out var t) ? t : 0,
                    cols[5]
                )
                {
                    Id = Guid.Parse(cols[15])
                });
            }
        }

        private void ReadBills(string folder)
        {
            var path = Path.Combine(folder, "Bills.csv");
            if (!File.Exists(path)) return;
            var lines = File.ReadAllLines(path).Skip(1);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = SplitCsvLine(line);
                Invoices.Add(new BillingDocument(ReadDate(cols[2]))
                {
                    Type = cols[1].ToLower() == "invoice" ? DocumentType.Invoice : DocumentType.Ticket,
                    PayerId = Guid.Parse(cols[5]),
                    Id = Guid.Parse(cols[6]),
                    SequenceNumber = int.TryParse(cols[7], out var s) ? s : 0
                });
            }
        }
    }
}
