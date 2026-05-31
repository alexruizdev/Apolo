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

        public async Task ReadExcel(string filePath)
        {
            try
            {
                using (var workbook = new XLWorkbook(filePath))
                {
                    ReadServices(workbook);
                    ReadStudents(workbook);
                    ReadLessons(workbook);
                    ReadPayment(workbook);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error reading Excel file: {ex.Message}");
            }
        }

        private void ReadServices(XLWorkbook workbook)
        {
            var worksheet = workbook.Worksheet("Service");
            var range = worksheet.Range("Services");
            var rows = range.RowsUsed();

            foreach (var row in rows)
            {
                string name = row.Cell(1).GetValue<string>().Trim();
                decimal price = 0;
                if (row.Cell(3).DataType == XLDataType.Number)
                    price = row.Cell(3).GetValue<decimal>();

                if (string.IsNullOrEmpty(name))
                    throw new InvalidDataException("Service name cannot be empty.");

                Services.Add(new Service()
                {
                    Name = name,
                    IsPricePerHour = true,
                    Price = price
                });
            }
        }

        private void ReadStudents(XLWorkbook workbook)
        {
            var worksheet = workbook.Worksheet("Students");
            var table = workbook.Table("Students");
            var rows = table.DataRange.RowsUsed();

            var serviceLookup = Services.ToDictionary(s => s.Name, s => s);

            // Add Astex Online
            Payers.Add(new Payer()
            {
                FirstName = "Astex Online Classes"
            });
            Students.Add(new Student()
            {
                FirstName = "Astex Online Classes",
                PayerId = Payers.Last().Id,
            });

            foreach (var row in rows)
            {
                string name = row.Field("Name").GetValue<string>().Trim();
                string serviceName = row.Field("Service").GetValue<string>().Trim();
                string channel = row.Field("Channel").GetValue<string>().Trim();
                string payerName = row.Field("Payer").GetValue<string>().Trim();
                string paymentMethod = row.Field("Payment Method").GetValue<string>().Trim();
                decimal contractPrice = 0;
                if (row.Field("Contract Price").TryGetValue(out decimal result))
                    contractPrice = result;
                string currency = row.Field("Currency").GetValue<string>().Trim();
                int defaultTime = 0;
                if (row.Field("Default Time").TryGetValue(out int defaultTimeResult))
                    defaultTime = defaultTimeResult;

                // TODO: remove after creating new Excel
                if (name == "Pino")
                    continue;
                
                if (name == "Pino, Jaime & Diego")
                    payerName = "Pino Herrero";

                if (name.ToLower().Contains("astex"))
                    payerName = "Astex Online Classes";

                if (name == "Filip")
                    name = "Filip Langsam";

                if (name == "Pablo")
                    name = "Pablo de Alier";

                if (name == "Ela")
                    name = "Ela Nowacka";

                // Add payer
                if (string.IsNullOrEmpty(payerName))
                    payerName = name; // Student is the payer

                var payer = Payers.FirstOrDefault(p => p.FirstName == payerName);
                if (payer is null) // Payer doesn't exist yet, create new
                {
                    payer = new Payer()
                    {
                        FirstName = payerName
                    };
                    Payers.Add(payer);
                }

                // Add student
                var student = Students.FirstOrDefault(s => s.FirstName == name);
                if (student is null)
                {
                    student = new Student()
                    {
                        FirstName = name,
                        PayerId = payer.Id,
                    };
                    Students.Add(student);
                }

                // Add specification
                if (!string.IsNullOrEmpty(serviceName))
                {
                    if (!serviceLookup.TryGetValue(serviceName, out var service))
                    {
                        throw new ArgumentException($"{serviceName} is not defined in Service tab.");
                    }

                    Specifications.Add(new Specification()
                    {
                        Name = $"{serviceName} {defaultTime}",
                        StudentId = student.Id,
                        ServiceId = service.Id,
                        DurationMinutes = defaultTime,
                        IsOnline = defaultTime == 0
                    });

                    if (name == "Pino Herrero")
                    {
                        Specifications.Add(new Specification()
                        {
                            Name = "Online",
                            StudentId = student.Id,
                            ServiceId = service.Id,
                            DurationMinutes = 30,
                            IsOnline = true
                        });
                    }
                }
            }
        }

        private Student GetStudent(string studentName, ref decimal finalPrice)
        {
            if (studentName.ToLower() == "pino")
                studentName = "Pino Herrero";
            if (studentName.ToLower() == "pino & jaime herreo")
            {
                studentName = "Pino, Jaime & Diego";
                finalPrice = 40;
            }
            if (studentName.ToLower() == "pino & jaime")
            {
                studentName = "Pino, Jaime & Diego";
                finalPrice = 40;
            }
            if (studentName == "Astex Online Classes")
                finalPrice = 16;

            if (studentName.ToLower() == "filip")
                studentName = "Filip Langsam";

            if (studentName.ToLower() == "pablo")
                studentName = "Pablo de Alier";

            if (studentName.ToLower() == "ela")
                studentName = "Ela Nowacka";

            if (studentName.ToLower().Contains("world"))
            {
                studentName = "Worldstrides";
            }

            var student = Students.FirstOrDefault(s => s.FirstName.ToLower() == studentName.ToLower());
            if (student is null)
                throw new Exception($"Invalid student name {studentName}");

            return student;
        }

        private void ReadLessons(XLWorkbook workbook)
        {
            var worksheet = workbook.Worksheet("Lessons");
            var table = workbook.Table("Lessons");
            var rows = table.DataRange.RowsUsed();

            foreach (var row in rows)
            {
                if (!row.Field("Lesson Date").TryGetValue(out DateTime dtValue))
                    continue;
                DateOnly lessonDate = DateOnly.FromDateTime(dtValue);
                string studentName = row.Field("Student").GetValue<string>().Trim();
                string serviceName = row.Field("Service").GetValue<string>().Trim();
                string channel = row.Field("Channel").GetValue<string>().Trim();
                int durationMinutes = 0;
                if (row.Field("Time (min)").TryGetValue(out int result))
                    durationMinutes = result;
                decimal finalPrice = 0;
                if (row.Field("Price").TryGetValue(out decimal priceResul))
                    finalPrice = priceResul;
                bool online = row.Field("Online").GetValue<bool>();
                decimal travelAllowance = 0;
                if (row.Field("Commuting").TryGetValue(out decimal travelAllowanceResult))
                    travelAllowance = travelAllowanceResult;

                // Old version fixes (delete)
                if (string.IsNullOrWhiteSpace(studentName ))
                    continue;
                
                // Search student
                var student = GetStudent(studentName, ref finalPrice);
                studentName = student.FirstName;

                if (finalPrice == 0)
                {
                    row.Field("Input").TryGetValue(out finalPrice);
                    if (finalPrice == 0)
                        continue;
                }

                var lesson = new Lesson(lessonDate, 
                    string.IsNullOrEmpty(serviceName) ? "Lesson" : serviceName,
                    isPaid: false, student.Id, null,
                    isPricePerHour: false, durationMinutes: durationMinutes, basePrice: finalPrice, 
                    isOnline: online, travelAllowance: 0,
                    isWeekenOrHoliday: false, weekendFee: 0, notes: null);
                
                Lessons.Add(lesson);
            }
        }

        private void ReadPayment(XLWorkbook workbook)
        {
            var worksheet = workbook.Worksheet("Payment");
            var table = workbook.Table("Payment");
            var rows = table.DataRange.RowsUsed();

            int invoiceCounter = 1;

            var temporaryList = new List<(DateOnly paymentDate, string studentName, decimal payment)>();

            foreach (var row in rows)
            {
                // Read Excel
                if (!row.Field("Date").TryGetValue(out DateTime dtValue))
                    continue;
                DateOnly paymentDate = DateOnly.FromDateTime(dtValue);
                string studentName = row.Field("Student").GetValue<string>().Trim();
                decimal payment = 0;
                if (row.Field("Payment").TryGetValue(out decimal result))
                    payment = result;

                // Check data
                if (studentName.ToLower().Contains("tip"))
                    continue; // tips are not considered payments
                if (studentName.ToLower().Contains("impuesto"))
                    continue; // taxes are not considered payments
                if (string.IsNullOrWhiteSpace(studentName))
                    continue;
                if (payment == 0)
                    continue;

                // Add to list as a tuple
                temporaryList.Add((paymentDate, studentName, payment));
            }

            var sortedList = temporaryList.OrderBy(p => p.paymentDate).ToList();
            var leftoverPayments = new Dictionary<string, decimal>();
            for (int j = 0; j < sortedList.Count; j++) { 
                var(paymentDate, studentName, payment) = sortedList[j];
                // Get student
                decimal finalPrice = 0; // not used, but needed to get student
                var student = GetStudent(studentName, ref finalPrice);

                // Get lessons to pay
                var lessons = Lessons
                    .Where(l => l.StudentId == student.Id &&  !l.IsPaid)
                    .OrderBy(l => l.Date).ToList();

                if (!lessons.Any()) 
                    continue;

                // Load leftover payment
                if (leftoverPayments.ContainsKey(student.FirstName))
                    payment += leftoverPayments[student.FirstName];

                // Create invoice
                var invoice = new BillingDocument(paymentDate.ToDateTime(TimeOnly.MinValue))
                {
                    SequenceNumber = invoiceCounter,
                    Type = DocumentType.Ticket,
                    PayerId = student.PayerId,
                };
                Invoices.Add(invoice);
                invoiceCounter++;

                // Mark lessons as paid until payment is consumed
                for (int i = 0; i < lessons.Count && payment > 0; i++)
                {
                    var lesson = lessons[i];
                    payment -= lesson.FinalPrice;
                    lesson.IsPaid = true;
                    lesson.BillingDocumentId = invoice.Id;
                }

                // Save leftovers
                if (payment != 0)
                    leftoverPayments[student.FirstName] = payment;
            }
        }
    }
}
