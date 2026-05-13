using ClosedXML.Excel;
using Models;

namespace Excel
{
    public interface IWriter
    {
        void WriteExcel(in string templatePath, in string folder, in (List<Service> services, List<Payer> payers,
            List<Student> students, List<Specification> specifications, List<Lesson> lessons, 
            List<Invoice> invoices) data);
    }
    public class Writer : IWriter
    {
        public void WriteExcel(in string templatePath, in string folder, in (List<Service> services, 
            List<Payer> payers, List<Student> students, List<Specification> specifications,
            List<Lesson> lessons, List<Invoice> invoices) data)
        {
            string destinationPath = Path.Combine(folder, $"Apolo_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
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
                    WriteStudents(workbook, in data.students);
                    WriteSpecifications(workbook, in data.specifications);
                    WriteLessons(workbook, in data.lessons);
                    WriteInvoices(workbook, in data.invoices);

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
                table.DataRange.Cell(row, 3).Value = service.Id.ToString();

                row++;
            }

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

            ResizeTable(table, payers.Count());
        }

        private void WriteStudents(XLWorkbook workbook, in List<Student> students)
        {
            var table = GetTable(workbook, "Students");
            int row = 1;

            foreach (var student in students)
            {
                table.DataRange.Cell(row, 1).Value = student.FirstName;
                table.DataRange.Cell(row, 2).Value = student.LastName;
                //table.DataRange.Cell(row, 3).Value = student.PayerName;
                table.DataRange.Cell(row, 4).Value = student.Id.ToString();
                table.DataRange.Cell(row, 5).Value = student.PayerId.ToString();

                row++;
            }

            ResizeTable(table, students.Count());
        }

        private void WriteSpecifications(XLWorkbook workbook, in List<Specification> specifications)
        {
            var table = GetTable(workbook, "Specifications");

            for (int i = 0; i < specifications.Count(); i++)
            {
                var specification = specifications.ElementAt(i);
                int row = i + 1;

                table.DataRange.Cell(row, 1).Value = specification.Name;
                //table.DataRange.Cell(row, 2).Value = specification.StudentName;
                //table.DataRange.Cell(row, 3).Value = specification.ServiceName;
                table.DataRange.Cell(row, 4).Value = specification.DurationMinutes;
                table.DataRange.Cell(row, 5).Value = specification.Price; 
                table.DataRange.Cell(row, 5).Value = specification.IsOnline;
            }

            ResizeTable(table, specifications.Count());
        }

        private void WriteLessons(XLWorkbook workbook, in List<Lesson> lessons)
        {
            var table = GetTable(workbook, "Lessons");
            int row = 1;

            foreach (var lesson in lessons)
            {
                foreach (var attendance in lesson.Attendances)
                {
                    table.DataRange.Cell(row, 1).Value = lesson.Date.ToString("yyyy-MM-dd");
                    //table.DataRange.Cell(row, 2).Value = attendance.StudentName;
                    table.DataRange.Cell(row, 3).Value = lesson.Name;
                    table.DataRange.Cell(row, 4).Value = lesson.DurationMinutes;
                    table.DataRange.Cell(row, 5).Value = lesson.IsOnline;
                    table.DataRange.Cell(row, 6).Value = lesson.PricePerAttendance;
                    table.DataRange.Cell(row, 7).Value = lesson.GetFinalPricePerStudent();
                    table.DataRange.Cell(row, 8).Value = attendance.IsPaid;
                    table.DataRange.Cell(row, 9).Value = lesson.Id.ToString();
                    table.DataRange.Cell(row, 10).Value = attendance.Id.ToString();
                    table.DataRange.Cell(row, 11).Value = attendance.StudentId.ToString();
                    //table.DataRange.Cell(row, 12).Value = lesson.id.ToString(); &&

                    row++;
                }
            }

            ResizeTable(table, lessons.Count());
        }

        private void WriteInvoices(XLWorkbook workbook, in List<Invoice> invoices)
        {
            var table = GetTable(workbook, "Invoices");

            int row = 1;

            foreach (var invoice in invoices)
            {
                foreach  (var line in invoice.Lines)
                {
                    table.DataRange.Cell(row, 1).Value = invoice.Id;
                    table.DataRange.Cell(row, 2).Value = invoice.Name;
                    table.DataRange.Cell(row, 3).Value = invoice.CreatedUTC.ToString();
                    table.DataRange.Cell(row, 4).Value = invoice.Payer.FullName;
                    table.DataRange.Cell(row, 5).Value = line.Attendance.Student.FullName;
                    // TODO: Total
                    // TODO: Paid
                    table.DataRange.Cell(row, 8).Value = invoice.PayerId.ToString();
                    table.DataRange.Cell(row, 9).Value = line.Attendance.StudentId.ToString();
                }
                row++;
            }

            ResizeTable(table, invoices.Count());
        }
    }
}
