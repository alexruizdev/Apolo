using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Models;

namespace Excel
{
    public interface IWriter
    {
        void WriteExcel(in string templatePath, in string folder, in (List<Service> services, List<Payer> payers,
            List<Student> students, List<Specification> specifications, List<Lesson> lessons, 
            List<BillingDocument> bills) data, bool archive = false);
    }
    public class Writer : IWriter
    {
        public void WriteExcel(in string templatePath, in string folder, in (List<Service> services, 
            List<Payer> payers, List<Student> students, List<Specification> specifications,
            List<Lesson> lessons, List<BillingDocument> bills) data, bool archive = false)
        {
            string destinationPath = Path.Combine(folder, $"Apolo_{(archive ? "Archive" : "Export")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            try
            {

                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException("Could not find the template file.", templatePath);
                }

                using (var workbook = new XLWorkbook(templatePath))
                {
                    WriteServices(workbook, in data.services);
                    WritePayers(workbook, in data.payers);
                    WriteStudents(workbook, in data.students, in data.payers);
                    WriteSpecifications(workbook, in data.specifications, in data.students, in data.services);
                    WriteLessons(workbook, in data.lessons, in data.students, in data.bills);
                    WriteInvoices(workbook, in data.bills, in data.payers, in data.lessons);

                    workbook.SaveAs(destinationPath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error writing Excel file: {ex.Message}");
            }
        }

        private IXLTable GetTable(XLWorkbook workbook, string name)
        {
            var worksheet = workbook.Worksheet(name);
            var table = worksheet.Table(name);
            if (table == null)
            {
                throw new Exception($"Table '{name}' not found in the workbook.");
            }
            table.DataRange.Clear(XLClearOptions.Contents); // Just to make sure we start with an empty table
            return table;
        }

        private void ResizeTable(IXLTable table, int rowCount)
        {
            table.Resize(table.Worksheet.Range(
                table.FirstCell().Address,
                table.DataRange.Cell(rowCount, table.ColumnCount()).Address));
        }

        private void WriteServices(XLWorkbook workbook, in List<Service> services)
        {
            var table = GetTable(workbook, "Services");
            int row = 1;

            foreach (var service in services)
            {
                table.DataRange.Cell(row, 1).Value = service.Name;
                table.DataRange.Cell(row, 2).Value = service.Price;
                table.DataRange.Cell(row, 3).Value = service.IsPricePerHour;
                table.DataRange.Cell(row, 4).Value = service.Id.ToString();

                row++;
            }
            if (services.Count > 0) 
                ResizeTable(table, services.Count());
        }

        private void WritePayers(XLWorkbook workbook, in List<Payer> payers)
        {
            var table = GetTable(workbook, "Payers");
            int row = 1;

            foreach (var payer in payers)
            {
                table.DataRange.Cell(row, 1).Value = payer.FirstName;
                table.DataRange.Cell(row, 2).Value = payer.LastName;
                table.DataRange.Cell(row, 3).Value = payer.Address;
                table.DataRange.Cell(row, 4).Value = payer.ZipCode;
                table.DataRange.Cell(row, 5).Value = payer.City;
                table.DataRange.Cell(row, 6).Value = payer.TaxId;
                table.DataRange.Cell(row, 7).Value = payer.Id.ToString();

                row++;
            }
            if (payers.Count > 0)
                ResizeTable(table, payers.Count());
        }

        private void WriteStudents(XLWorkbook workbook, in List<Student> students, in List<Payer> payers)
        {
            var table = GetTable(workbook, "Students");
            int row = 1;

            var payerLookup = payers.ToDictionary(p => p.Id, p => p);

            foreach (var student in students)
            {
                table.DataRange.Cell(row, 1).Value = student.FirstName;
                table.DataRange.Cell(row, 2).Value = student.LastName;
                table.DataRange.Cell(row, 3).Value = payerLookup[student.PayerId].FullName;
                table.DataRange.Cell(row, 4).Value = student.Id.ToString();
                table.DataRange.Cell(row, 5).Value = student.PayerId.ToString();

                row++;
            }
            if (students.Count > 0)
                ResizeTable(table, students.Count());
        }

        private void WriteSpecifications(XLWorkbook workbook, in List<Specification> specifications,
            in List<Student> students, in List<Service> services)
        {
            var table = GetTable(workbook, "Specifications");
            int row = 1;

            var serviceLookup = services.ToDictionary(s => s.Id, s => s);
            var studentLookup = students.ToDictionary(s => s.Id, s => s);

            //for (int i = 0; i < specifications.Count(); i++)
            foreach (var spec in specifications)
            {
                table.DataRange.Cell(row, 1).Value = spec.Name;
                table.DataRange.Cell(row, 2).Value = studentLookup[spec.StudentId].FullName;
                table.DataRange.Cell(row, 3).Value = serviceLookup[spec.ServiceId].Name;
                table.DataRange.Cell(row, 4).Value = spec.DurationMinutes;
                table.DataRange.Cell(row, 5).Value = spec.Price; 
                table.DataRange.Cell(row, 6).Value = spec.IsOnline;
                table.DataRange.Cell(row, 7).Value = spec.IsWeekendOrHoliday;
                table.DataRange.Cell(row, 8).Value = spec.UsageCount;
                table.DataRange.Cell(row, 9).Value = spec.Id.ToString();
                table.DataRange.Cell(row, 10).Value = spec.StudentId.ToString();
                table.DataRange.Cell(row, 11).Value = spec.ServiceId.ToString();

                row++;
            }

            if (specifications.Count > 0)
                ResizeTable(table, specifications.Count());
        }

        private void WriteLessons(XLWorkbook workbook, in List<Lesson> lessons, in List<Student> students,
            in List<BillingDocument> bills)
        {
            var table = GetTable(workbook, "Lessons");
            int row = 1;

            var studentLookup = students.ToDictionary(s => s.Id, s => s);
            var billLookup = bills.ToDictionary(s => s.Id, b => b);

            foreach (var lesson in lessons)
            {
                var billName = string.Empty;
                var billId = string.Empty;
                if (lesson.BillingDocumentId is Guid id)
                {
                    billName = billLookup[id].DocumentNumber;
                    billId = id.ToString();
                }
                table.DataRange.Cell(row, 1).Value = lesson.Date.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture); ;
                table.DataRange.Cell(row, 2).Value = lesson.Name;
                table.DataRange.Cell(row, 3).Value = studentLookup[lesson.StudentId].FullName;
                table.DataRange.Cell(row, 4).Value = lesson.FinalPrice;
                table.DataRange.Cell(row, 5).Value = lesson.IsPaid;
                table.DataRange.Cell(row, 6).Value = lesson.Notes;
                table.DataRange.Cell(row, 7).Value = billName;
                table.DataRange.Cell(row, 8).Value = lesson.IsPricePerHour;
                table.DataRange.Cell(row, 9).Value = lesson.DurationMinutes;
                table.DataRange.Cell(row, 10).Value = lesson.BasePrice;
                table.DataRange.Cell(row, 11).Value = lesson.IsOnline;
                table.DataRange.Cell(row, 12).Value = lesson.TravelAllowance;
                table.DataRange.Cell(row, 13).Value = lesson.IsWeekendOrHoliday;
                table.DataRange.Cell(row, 14).Value = lesson.WeekendFee;
                table.DataRange.Cell(row, 15).Value = lesson.Tip;
                table.DataRange.Cell(row, 16).Value = lesson.Id.ToString();
                table.DataRange.Cell(row, 17).Value = lesson.StudentId.ToString();
                table.DataRange.Cell(row, 18).Value = billId;

                row++;
            }

            if (lessons.Count > 0)
                ResizeTable(table, lessons.Count());
        }

        private void WriteInvoices(XLWorkbook workbook, in List<BillingDocument> bills,
            in List<Payer> payers, in List<Lesson> lessons)
        {
            var table = GetTable(workbook, "Bills");
            int row = 1;

            var billLookup = lessons
                .Where(l => l.BillingDocumentId.HasValue) // 1. Ignore lessons without a bill
                .GroupBy(l => l.BillingDocumentId!.Value)  // 2. Group them by the actual Guid
                .ToDictionary(
                    group => group.Key,                   // 3. The Dictionary Key (BillingDocumentId)
                    group => group.Sum(l => l.FinalPrice) // 4. The Dictionary Value (Sum of prices)
                );
            var payerLookup = payers.ToDictionary(p => p.Id, p => p.FullName);

            foreach (var bill in bills)
            {
                table.DataRange.Cell(row, 1).Value = bill.DocumentNumber;
                table.DataRange.Cell(row, 2).Value = bill.Type.ToString();
                table.DataRange.Cell(row, 3).Value = bill.CreatedUTC.ToString("dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                table.DataRange.Cell(row, 4).Value = payerLookup[bill.PayerId];
                table.DataRange.Cell(row, 5).Value = billLookup[bill.Id];
                table.DataRange.Cell(row, 6).Value = bill.PayerId.ToString();
                table.DataRange.Cell(row, 7).Value = bill.Id.ToString();
                table.DataRange.Cell(row, 8).Value = bill.SequenceNumber;

                row++;
            }

            if (bills.Count > 0)
                ResizeTable(table, bills.Count());
        }
    }
}
