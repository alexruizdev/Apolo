using ClosedXML.Excel;
using Models;

namespace Excel
{
    public class Writer
    {
        public List<ServiceSummary> Services { get; set; } = new List<ServiceSummary>();
        public List<PayerSummary> Payers { get; set; } = new List<PayerSummary>();
        public List<StudentSummary> Students { get; set; } = new List<StudentSummary>();
        public List<SpecificationSummary> Specifications { get; set; } = new List<SpecificationSummary>();
        public List<LessonSummary> Lessons { get; set; } = new List<LessonSummary>();
        public List<Invoice> Invoices { get; set; } = new List<Invoice>();

        public void WriteExcel(in string templatePath, in string folder)
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
                    WriteServices(workbook);
                    WritePayers(workbook);
                    WriteStudents(workbook);
                    WriteSpecifications(workbook);
                    WriteLessons(workbook);
                    WriteInvoices(workbook);

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



        private void WriteServices(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Services");

            int nameCol = table.Field("Name").Index + 1;
            int priceCol = table.Field("Price / h").Index + 1;

            for (int i = 0; i < Services.Count; i++)
            {
                var service = Services[i];
                int row = i + 1;

                table.DataRange.Cell(row, nameCol).Value = service.Name;
                table.DataRange.Cell(row, priceCol).Value = service.PricePerHour;
            }

            ResizeTable(table, Services.Count);
        }

        private void WritePayers(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Payers");

            int firstNameCol = table.Field("First name").Index + 1;
            int lastNameCol = table.Field("Last name").Index + 1;
            int addressCol = table.Field("Address").Index + 1;
            int zipCodeCol = table.Field("Zip code").Index + 1;
            int cityCol = table.Field("City").Index + 1;
            int nifCifCol = table.Field("NIF/CIF").Index + 1;
            int idCol = table.Field("ID").Index + 1;

            for (int i = 0; i < Payers.Count; i++)
            {
                var payer = Payers[i];
                int row = i + 1;

                table.DataRange.Cell(row, firstNameCol).Value = payer.FirstName;
                table.DataRange.Cell(row, lastNameCol).Value = payer.LastName;
                table.DataRange.Cell(row, addressCol).Value = payer.Address;
                table.DataRange.Cell(row, zipCodeCol).Value = payer.Zip;
                table.DataRange.Cell(row, cityCol).Value = payer.City;
                table.DataRange.Cell(row, nifCifCol).Value = payer.TaxId;
                table.DataRange.Cell(row, idCol).Value = payer.Id.ToString();
            }

            ResizeTable(table, Payers.Count);
        }

        private void WriteStudents(XLWorkbook workbook)
        {
            var worksheet = workbook.Worksheet("Students");
            var table = workbook.Table("Students");
            table.DataRange.Clear(XLClearOptions.Contents); // Just to make sure we start with an empty table

            int firstNameCol = table.Field("First name").Index + 1;
            int lastNameCol = table.Field("Last name").Index + 1;
            int addressCol = table.Field("Payer name").Index + 1;
            int zipCodeCol = table.Field("Commute").Index + 1;
            int idCol = table.Field("ID").Index + 1;
            int payerIdCol = table.Field("PayerId").Index + 1;

            for (int i = 0; i < Students.Count; i++)
            {
                var student = Students[i];
                int row = i + 1;

                table.DataRange.Cell(row, firstNameCol).Value = student.FirstName;
                table.DataRange.Cell(row, lastNameCol).Value = student.LastName;
                table.DataRange.Cell(row, addressCol).Value = student.PayerName;
                table.DataRange.Cell(row, zipCodeCol).Value = student.CommuteMinutes;
                table.DataRange.Cell(row, idCol).Value = student.Id.ToString();
                table.DataRange.Cell(row, idCol).Value = student.PayerId.ToString();
            }

            table.Resize(worksheet.Range(
                table.FirstCell().Address,
                table.DataRange.Cell(Students.Count, table.ColumnCount()).Address));
        }

        private void WriteSpecifications(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Specifications");

            int nameCol = table.Field("Name").Index + 1;
            int studentNameCol = table.Field("Student name").Index + 1;
            int serviceCol = table.Field("Service").Index + 1;
            int durationCol = table.Field("Duration").Index + 1;
            int priceCol = table.Field("Price").Index + 1;
            int onlineCol = table.Field("Online").Index + 1;

            for (int i = 0; i < Specifications.Count; i++)
            {
                var specification = Specifications[i];
                int row = i + 1;

                table.DataRange.Cell(row, nameCol).Value = specification.SpecificationName;
                table.DataRange.Cell(row, studentNameCol).Value = specification.StudentName;
                table.DataRange.Cell(row, serviceCol).Value = specification.ServiceName;
                table.DataRange.Cell(row, durationCol).Value = specification.DurationMinutes;
                table.DataRange.Cell(row, onlineCol).Value = specification.IsOnline;
            }

            ResizeTable(table, Specifications.Count);
        }

        private void WriteLessons(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Lessons");

            int dateCol = table.Field("Date").Index + 1;
            int studentCol = table.Field("Student").Index + 1;
            int serviceCol = table.Field("Service").Index + 1;
            int durationCol = table.Field("Duration (min)").Index + 1;
            int onlineCol = table.Field("Online").Index + 1;
            int isTotalPriceCol = table.Field("Is total price").Index + 1;
            int pricePerStudentCol = table.Field("Price per student").Index + 1;
            int paidCol = table.Field("Paid").Index + 1;
            int lessonIdCol = table.Field("Lesson ID").Index + 1;
            int attendanceIdCol = table.Field("Attendance ID").Index + 1;
            int studentIdCol = table.Field("Student ID").Index + 1;

            int row = 1;
            foreach (var lesson in Lessons)
            {
                foreach (var attendance in lesson.Attendances)
                {
                    table.DataRange.Cell(row, dateCol).Value = lesson.Date.ToString("yyyy-MM-dd");
                    table.DataRange.Cell(row, studentCol).Value = attendance.StudentName;
                    table.DataRange.Cell(row, serviceCol).Value = lesson.Name;
                    table.DataRange.Cell(row, durationCol).Value = lesson.DurationMinutes;
                    table.DataRange.Cell(row, onlineCol).Value = lesson.IsOnline;
                    table.DataRange.Cell(row, isTotalPriceCol).Value = lesson.IsTotalPrice;
                    table.DataRange.Cell(row, pricePerStudentCol).Value = lesson.GrandTotal;
                    table.DataRange.Cell(row, paidCol).Value = attendance.IsPaid;
                    table.DataRange.Cell(row, lessonIdCol).Value = lesson.Id.ToString();
                    table.DataRange.Cell(row, attendanceIdCol).Value = attendance.Id.ToString();
                    table.DataRange.Cell(row, studentIdCol).Value = attendance.StudentId.ToString();
                    row++;
                }
            }

            ResizeTable(table, Lessons.Count);
        }

        private void WriteInvoices(XLWorkbook workbook)
        {
            var table = GetTable(workbook, "Invoices");

            int row = 1;

            foreach (var invoice in Invoices)
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

            ResizeTable(table, Invoices.Count);
        }
    }
}
