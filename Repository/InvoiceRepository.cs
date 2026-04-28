using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Query;
using Models;
using System.Data;

namespace Repository
{
    public sealed class InvoiceRepository
    {
        private readonly ApoloContext _db;

        public InvoiceRepository(ApoloContext db)
        {
            _db = db;
        }

        public async Task<PayerSummary> GetPayerSummaryAsync(Guid payerId)
        {
            var payer = await _db.Payers
                .AsNoTracking()
                .FirstAsync();

            return new PayerSummary(
                payerId,
                payer.FirstName,
                payer.LastName,
                0m,
                payer.Address,
                payer.ZipCode,
                payer.City,
                payer.TaxId
            );
        }

        public async Task<IEnumerable<PayerOption>> GetPayerOptionsAsync()
        {
            var result = await _db.Payers
                 .AsNoTracking()
                 .Select(p => new PayerOption(
                     p.Id,
                     p.FullName))
                 .ToListAsync();
            return result.OrderBy(x => x.FullName).ToList();
        }

        public async Task<IEnumerable<InvoiceAttendanceSummary>> GetInvoiceAttendancesAsync(Guid payerId)
        {
            var result = await _db.Attendances
                .AsNoTracking()
                .Where(a => !a.IsPaid && a.Student.PayerId == payerId)
                .Select(a => new InvoiceAttendanceSummary
                (
                    a.Id,
                    a.LessonId,
                    a.Lesson.Date,
                    a.Lesson.Name,
                    a.StudentId,
                    a.Student.FullName,
                    a.Lesson.GetFinalPricePerStudent()
                ))
                .ToListAsync();
            return result.OrderBy(x => x.Date);
        }

        public async Task<IEnumerable<InvoiceAttendanceSummary>> GetInvoiceAttendancesAsync(string invoiceName)
        {
            var result = await _db.InvoiceAttendances
                .AsNoTracking()
                .Where(x => x.Invoice.Name == invoiceName.Trim())
                .Select(x => x.Attendance)
                .Where(a => !a.IsPaid)
                .Select(a => new InvoiceAttendanceSummary
                (
                    a.Id,
                    a.LessonId,
                    a.Lesson.Date,
                    a.Lesson.Name,
                    a.StudentId,
                    a.Student.FullName,
                    a.Lesson.GetFinalPricePerStudent() // TODO: add price to invoice
                ))
                .ToListAsync();
            return result.OrderBy(x => x.Date);
        }


        public async Task UpdateAttendancesAsync(IEnumerable<Guid> attendancesIds)
        {
            var toUpdate = await _db.Attendances
                .Where(a => attendancesIds.Contains(a.Id))
                .ToListAsync();

            foreach (var a in toUpdate) a.IsPaid = true;

            await _db.SaveChangesAsync();

        }

        public async Task UpsertAsync(Invoice invoice)
        {
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
        }

        public async Task<(int invoiceId, string InvoiceName)> CreateInvoiceAsync(Guid payerId, 
            IEnumerable<Guid> attendanceIds, string? requestedName)
        {
            var now = DateTime.UtcNow;
            var invoice = new Invoice
            {
                PayerId = payerId,
                CreatedUTC = now,
                Name = string.IsNullOrWhiteSpace(requestedName)
                    ? string.Empty : requestedName.Trim()
            };
            foreach (var attendance in attendanceIds)
            {
                invoice.Lines.Add(new InvoiceAttendance
                {
                    AttendanceId = attendance
                });
            }
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();

            // name rule: user input OR fallback "yyyy-MM-E-{Id}
            if (string.IsNullOrWhiteSpace(invoice.Name))
            {
                invoice.Name = $"{DateTime.Now:yyyy-MM}-E-{invoice.Id}";
                await _db.SaveChangesAsync();
            }

            return (invoice.Id, invoice.Name);
        }

        public async Task DeleteInvoiceAsync(int invoiceId)
        {
            var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null) return;

            _db.Invoices.Remove(invoice);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesAsync()
        {
            var result = await _db.Invoices
                .AsNoTracking()
                .Include(x => x.Payer)
                .Include(x => x.Lines)
                    .ThenInclude(x => x.Attendance)
                        .ThenInclude(x => x.Student)
                .ToListAsync();
            return result.OrderBy(x => x.CreatedUTC);
        }
    }
}
