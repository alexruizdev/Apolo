using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CSV
{
    public interface IWriter
    {
        void WriteExcel(in string templatePath, in string folder, in (List<Service> services, List<Payer> payers,
            List<Student> students, List<Specification> specifications, List<Lesson> lessons, 
            List<BillingDocument> bills) data, bool archive = false);
    }

    public class Writer : IWriter
    {
        // Notice we still implement WriteExcel to fulfill IWriter and keep the same method signature for now, 
        // though we ignore templatePath and write CSV files to a new folder instead.
        public void WriteExcel(in string templatePath, in string folder, in (List<Service> services, 
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
                WriteSpecifications(destinationFolder, data.specifications, data.students, data.services);
                WriteLessons(destinationFolder, data.lessons, data.students, data.bills);
                WriteInvoices(destinationFolder, data.bills, data.payers, data.lessons);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error writing CSV files: {ex.Message}");
            }
        }

        private string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Contains(",") || text.Contains("\"") || text.Contains("\n"))
            {
                return "\"" + text.Replace("\"", "\"\"") + "\"";
            }
            return text;
        }

        private void WriteServices(string folder, List<Service> services)
        {
            using var sw = new StreamWriter(Path.Combine(folder, "Services.csv"));
            sw.WriteLine("Name,Price,IsPricePerHour,Id");
            foreach (var service in services)
            {
                sw.WriteLine($"{Escape(service.Name)},{service.Price},{service.IsPricePerHour},{service.Id}");
            }
        }

        private void WritePayers(string folder, List<Payer> payers)
        {
            using var sw = new StreamWriter(Path.Combine(folder, "Payers.csv"));
            sw.WriteLine("FirstName,LastName,Address,ZipCode,City,TaxId,Id");
            foreach (var payer in payers)
            {
                sw.WriteLine($"{Escape(payer.FirstName)},{Escape(payer.LastName)},{Escape(payer.Address)},{Escape(payer.ZipCode)},{Escape(payer.City)},{Escape(payer.TaxId)},{payer.Id}");
            }
        }

        private void WriteStudents(string folder, List<Student> students, List<Payer> payers)
        {
            using var sw = new StreamWriter(Path.Combine(folder, "Students.csv"));
            sw.WriteLine("FirstName,LastName,PayerName,Id,PayerId");
            var payerLookup = payers.ToDictionary(p => p.Id, p => p);
            foreach (var student in students)
            {
                var payerName = payerLookup.ContainsKey(student.PayerId) ? payerLookup[student.PayerId].FullName : "";
                sw.WriteLine($"{Escape(student.FirstName)},{Escape(student.LastName)},{Escape(payerName)},{student.Id},{student.PayerId}");
            }
        }

        private void WriteSpecifications(string folder, List<Specification> specifications, List<Student> students, List<Service> services)
        {
            using var sw = new StreamWriter(Path.Combine(folder, "Specifications.csv"));
            sw.WriteLine("Name,StudentName,ServiceName,DurationMinutes,Price,IsOnline,IsWeekendOrHoliday,UsageCount,Id,StudentId,ServiceId");
            var serviceLookup = services.ToDictionary(s => s.Id, s => s);
            var studentLookup = students.ToDictionary(s => s.Id, s => s);
            foreach (var spec in specifications)
            {
                var studentName = studentLookup.ContainsKey(spec.StudentId) ? studentLookup[spec.StudentId].FullName : "";
                var serviceName = serviceLookup.ContainsKey(spec.ServiceId) ? serviceLookup[spec.ServiceId].Name : "";
                sw.WriteLine($"{Escape(spec.Name)},{Escape(studentName)},{Escape(serviceName)},{spec.DurationMinutes},{spec.Price},{spec.IsOnline},{spec.IsWeekendOrHoliday},{spec.UsageCount},{spec.Id},{spec.StudentId},{spec.ServiceId}");
            }
        }

        private void WriteLessons(string folder, List<Lesson> lessons, List<Student> students, List<BillingDocument> bills)
        {
            using var sw = new StreamWriter(Path.Combine(folder, "Lessons.csv"));
            sw.WriteLine("Date,Name,StudentName,FinalPrice,IsPaid,Notes,BillName,IsPricePerHour,DurationMinutes,BasePrice,IsOnline,TravelAllowance,IsWeekendOrHoliday,WeekendFee,Tip,Id,StudentId,BillId");
            var studentLookup = students.ToDictionary(s => s.Id, s => s);
            var billLookup = bills.ToDictionary(s => s.Id, b => b);
            foreach (var lesson in lessons)
            {
                var billName = "";
                var billId = "";
                if (lesson.BillingDocumentId is Guid id && billLookup.ContainsKey(id))
                {
                    billName = billLookup[id].DocumentNumber;
                    billId = id.ToString();
                }
                var studentName = studentLookup.ContainsKey(lesson.StudentId) ? studentLookup[lesson.StudentId].FullName : "";
                var dateStr = lesson.Date.ToString("dd-MM-yyyy", System.Globalization.CultureInfo.InvariantCulture);
                sw.WriteLine($"{dateStr},{Escape(lesson.Name)},{Escape(studentName)},{lesson.FinalPrice},{lesson.IsPaid},{Escape(lesson.Notes)},{Escape(billName)},{lesson.IsPricePerHour},{lesson.DurationMinutes},{lesson.BasePrice},{lesson.IsOnline},{lesson.TravelAllowance},{lesson.IsWeekendOrHoliday},{lesson.WeekendFee},{lesson.Tip},{lesson.Id},{lesson.StudentId},{billId}");
            }
        }

        private void WriteInvoices(string folder, List<BillingDocument> bills, List<Payer> payers, List<Lesson> lessons)
        {
            using var sw = new StreamWriter(Path.Combine(folder, "Bills.csv"));
            sw.WriteLine("DocumentNumber,Type,CreatedUTC,PayerName,Total,PayerId,Id,SequenceNumber");
            var billLookup = lessons
                .Where(l => l.BillingDocumentId.HasValue)
                .GroupBy(l => l.BillingDocumentId!.Value)
                .ToDictionary(group => group.Key, group => group.Sum(l => l.FinalPrice));
            var payerLookup = payers.ToDictionary(p => p.Id, p => p.FullName);

            foreach (var bill in bills)
            {
                var total = billLookup.ContainsKey(bill.Id) ? billLookup[bill.Id] : 0m;
                var payerName = payerLookup.ContainsKey(bill.PayerId) ? payerLookup[bill.PayerId] : "";
                var dateStr = bill.CreatedUTC.ToString("dd-MM-yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
                sw.WriteLine($"{Escape(bill.DocumentNumber)},{bill.Type},{dateStr},{Escape(payerName)},{total},{bill.PayerId},{bill.Id},{bill.SequenceNumber}");
            }
        }
    }
}
