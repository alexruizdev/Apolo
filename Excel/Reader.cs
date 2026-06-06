using ClosedXML.Excel;
using Models;

namespace Excel
{
    public interface IReader
    {
        public List<Service> Services { get; } 
        public List<Payer> Payers { get; } 
        public List<Student> Students { get; } 
        public List<Specification> Specifications { get; } 
        public List<Lesson> Lessons { get; } 
        public List<BillingDocument> Invoices { get; }
        Task ReadExcel(string filePath);
    }
    public class Reader : IReader
    {
        public List<Service> Services { get; } = new List<Service>();
        public List<Payer> Payers { get; } = new List<Payer>();
        public List<Student> Students { get; } = new List<Student>();
        public List<Specification> Specifications { get; } = new List<Specification>();
        public List<Lesson> Lessons { get; } = new List<Lesson>();
        public List<BillingDocument> Invoices { get; } = new List<BillingDocument>();

        private IXLTable GetTable(XLWorkbook workbook, string name)
        {
            var worksheet = workbook.Worksheet(name);
            var table = worksheet.Table(name);
            if (table == null)
            {
                throw new Exception($"Table '{name}' not found in the workbook.");
            }
            return table;
        }

        public async Task ReadExcel(string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    ReadServices(workbook);
                    ReadPayers(workbook);
                    ReadStudents(workbook);
                    ReadSpecification(workbook);
                    ReadPayment(workbook);
                    ReadLessons(workbook);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading Excel file: {ex.Message}");
            }
        }

        private void ReadServices(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Services");
            var rows = table.DataRange.RowsUsed();
            foreach (var row in rows)
            {
                string name = row.Cell(1).GetValue<string>().Trim();
                decimal price = 0;
                if (row.Cell(2).DataType == XLDataType.Number)
                    price = row.Cell(2).GetValue<decimal>();

                if (string.IsNullOrEmpty(name))
                    throw new InvalidDataException("Service name cannot be empty.");

                bool isPricePerHour = row.Cell(3).GetValue<string>().Trim().ToLower() == "true";
                string id = row.Cell(4).GetValue<string>().Trim();

                Services.Add(new Service()
                {
                    Id = Guid.Parse(id),
                    Name = name,
                    IsPricePerHour = isPricePerHour,
                    Price = price
                });
            }
        }

        private void ReadPayers(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Payers");
            var rows = table.DataRange.RowsUsed();
            foreach (var row in rows)
            {
                string firstName = row.Cell(1).GetValue<string>().Trim();
                string lastName = row.Cell(2).GetValue<string>().Trim();
                string address = row.Cell(3).GetValue<string>().Trim();
                string zip = row.Cell(4).GetValue<string>().Trim();
                string city = row.Cell(5).GetValue<string>().Trim();
                string taxId = row.Cell(6).GetValue<string>().Trim();
                string id = row.Cell(7).GetValue<string>().Trim();

                Payers.Add(new Payer()
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Address = address,
                    ZipCode = zip,
                    City = city,
                    TaxId = taxId,
                    Id = Guid.Parse(id)
                });
            }
        }

        private void ReadStudents(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Students");
            var rows = table.DataRange.RowsUsed();
            foreach (var row in rows)
            {
                string firstName = row.Cell(1).GetValue<string>().Trim();
                string lastName = row.Cell(2).GetValue<string>().Trim();
                string payerName = row.Cell(3).GetValue<string>().Trim();
                string id = row.Cell(4).GetValue<string>().Trim();
                string payerId = row.Cell(5).GetValue<string>().Trim();

                // Add student
                var student = new Student()
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Id = Guid.Parse(id),
                    PayerId = Guid.Parse(payerId),
                };
                Students.Add(student);

            }
        }

        private void ReadSpecification(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Specifications");
            var rows = table.DataRange.RowsUsed();            foreach (var row in rows)
            {
                string name = row.Cell(1).GetValue<string>().Trim();
                string studentName = row.Cell(2).GetValue<string>().Trim();
                string serviceName = row.Cell(3).GetValue<string>().Trim();
                int durationMinutes = 0;
                if (row.Cell(4).DataType == XLDataType.Number)
                    durationMinutes = row.Cell(4).GetValue<int>();
                decimal? price = null;
                if (row.Cell(5).DataType == XLDataType.Number)
                    price = row.Cell(5).GetValue<decimal>();
                bool isOnline = row.Cell(6).GetValue<string>().Trim().ToLower() == "true";
                bool weekend = row.Cell(7).GetValue<string>().Trim().ToLower() == "true";
                int usage = 0;
                if (row.Cell(8).DataType == XLDataType.Number)
                    usage = row.Cell(8).GetValue<int>();
                string id = row.Cell(9).GetValue<string>().Trim();
                string studentId = row.Cell(10).GetValue<string>().Trim();
                string serviceId = row.Cell(11).GetValue<string>().Trim();

                Specifications.Add(new Specification()
                {
                    Id = Guid.Parse(id),
                    Name = name,
                    StudentId = Guid.Parse(studentId),
                    ServiceId = Guid.Parse(serviceId),
                    DurationMinutes = durationMinutes,
                    Price = price,
                    IsOnline = isOnline,
                    IsWeekendOrHoliday = weekend,
                    UsageCount = usage
                });
            }
        }

        private void ReadLessons(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Lessons");
            var rows = table.DataRange.RowsUsed();

            foreach (var row in rows)
            {
                string date = row.Cell(1).GetValue<string>().Trim();
                var dtValue = DateTime.ParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                DateOnly lessonDate = DateOnly.FromDateTime(dtValue);
                string serviceName = row.Cell(2).GetValue<string>().Trim();
                string studentName = row.Cell(3).GetValue<string>().Trim();
                decimal price = row.Cell(4).GetValue<decimal>();
                bool paid = row.Cell(5).GetValue<string>().Trim().ToLower() == "true";
                string notes = row.Cell(6).GetValue<string>().Trim();
                string bill = row.Cell(7).GetValue<string>().Trim();
                bool pricePerHour = row.Cell(8).GetValue<string>().Trim().ToLower() == "true";
                int durationMinutes = 0;
                if (row.Cell(9).DataType == XLDataType.Number)
                    durationMinutes = row.Cell(9).GetValue<int>();
                decimal basePrice = row.Cell(10).GetValue<decimal>();
                bool isOnline = row.Cell(11).GetValue<string>().Trim().ToLower() == "true";
                decimal travelAllowance = row.Cell(12).GetValue<decimal>();
                bool weekend = row.Cell(13).GetValue<string>().Trim().ToLower() == "true";
                decimal fee = row.Cell(14).GetValue<decimal>();
                decimal tip = row.Cell(15).GetValue<decimal>();
                string id = row.Cell(16).GetValue<string>().Trim();
                string studentId = row.Cell(17).GetValue<string>().Trim();
                string billId = row.Cell(18).GetValue<string>().Trim();

                var lesson = new Lesson(
                    lessonDate,
                    serviceName,
                    isPaid: paid,
                    Guid.Parse(studentId),
                    string.IsNullOrEmpty(billId) ? null : Guid.Parse(billId),
                    isPricePerHour: pricePerHour,
                    durationMinutes: durationMinutes,
                    basePrice: basePrice,
                    isOnline: isOnline,
                    travelAllowance: travelAllowance,
                    isWeekendOrHoliday: weekend,
                    weekendFee: fee,
                    tip: tip,
                    notes: notes)
                {
                    Id = Guid.Parse(id)
                };
                
                Lessons.Add(lesson);
            }
        }

        private void ReadPayment(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Bills");
            var rows = table.DataRange.RowsUsed();
            foreach (var row in rows)
            {
                string name = row.Cell(1).GetValue<string>().Trim();
                string type = row.Cell(2).GetValue<string>().Trim();
                string date = row.Cell(3).GetValue<string>().Trim();
                var dtValue = DateTime.Parse(date);
                string payer = row.Cell(4).GetValue<string>().Trim();
                string total = row.Cell(5).GetValue<string>().Trim();
                string payerId = row.Cell(6).GetValue<string>().Trim();
                string id = row.Cell(7).GetValue<string>().Trim();
                int sequence = row.Cell(8).GetValue<int>();

                Invoices.Add(new BillingDocument(dtValue)
                {
                    Id = Guid.Parse(id),
                    PayerId = Guid.Parse(payerId),
                    Type = type.ToLower() == "invoice" ? DocumentType.Invoice : DocumentType.Ticket,
                    SequenceNumber = sequence
                });
            }
        }
    }
}
