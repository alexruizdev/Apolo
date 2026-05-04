using Microsoft.EntityFrameworkCore;
using Models;
using System.Data;

namespace Repository
{
    public sealed class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApoloContext _db;

        public InvoiceRepository(ApoloContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<InvoiceAttendanceSummary>> GetInvoiceAttendancesAsync(Guid payerId)
        {
            // 1. Fetch only the raw 'ingredients' from SQL
            var rawData = await _db.Attendances
                .AsNoTracking()
                .Where(a => !a.IsPaid && a.Student.PayerId == payerId)
                .OrderBy(a => a.Lesson.Date)
                .Select(a => new
                {
                    a.Id,
                    a.LessonId,
                    a.Lesson.Date,
                    a.Lesson.Name,
                    a.StudentId,
                    // Access properties directly so EF can build the JOINs
                    StudentFirstName = a.Student.FirstName,
                    StudentLastName = a.Student.LastName,
                    // Pricing ingredients
                    a.Lesson.IsPricePerHour,
                    a.Lesson.DurationMinutes,
                    a.Lesson.PricePerAttendance,
                    a.Lesson.IsOnline,
                    a.Lesson.TravelAllowance,
                    a.Lesson.IsWeekenOrHoliday,
                    a.Lesson.WeekendFee
                })
                .ToListAsync();

            // 2. Perform the complex C# math in memory (fast and safe)
            return rawData.Select(r => new InvoiceAttendanceSummary(
                r.Id,
                r.LessonId,
                r.Date,
                r.Name,
                r.StudentId,
                Helper.GetFullName(r.StudentFirstName, r.StudentLastName),
                Lesson.GetPrice( 
                    1,
                    r.IsPricePerHour,
                    r.DurationMinutes,
                    r.PricePerAttendance,
                    r.IsOnline,
                    r.TravelAllowance,
                    r.IsWeekenOrHoliday,
                    r.WeekendFee)
            ));
        }

        public async Task<IEnumerable<InvoiceAttendanceSummary>> GetInvoiceAttendancesAsync(string invoiceName)
        {
            var cleanName = invoiceName?.Trim() ?? string.Empty;

            var rawData = await _db.InvoiceAttendances
                .AsNoTracking()
                .Where(x => x.Invoice.Name == cleanName)
                .Select(x => new
                {
                    x.Attendance.Id,
                    x.Attendance.LessonId,
                    x.Attendance.Lesson.Date,
                    x.Attendance.Lesson.Name,
                    x.Attendance.StudentId,
                    StudentFirstName = x.Attendance.Student.FirstName,
                    StudentLastName = x.Attendance.Student.LastName,
                    // Pricing ingredients
                    x.Attendance.Lesson.IsPricePerHour,
                    x.Attendance.Lesson.DurationMinutes,
                    x.Attendance.Lesson.PricePerAttendance,
                    x.Attendance.Lesson.IsOnline,
                    x.Attendance.Lesson.TravelAllowance,
                    x.Attendance.Lesson.IsWeekenOrHoliday,
                    x.Attendance.Lesson.WeekendFee
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return rawData.Select(r => new InvoiceAttendanceSummary(
                r.Id,
                r.LessonId,
                r.Date,
                r.Name,
                r.StudentId,
                $"{r.StudentFirstName} {r.StudentLastName}",
                Lesson.GetPrice(
                    1,
                    r.IsPricePerHour,
                    r.DurationMinutes,
                    r.PricePerAttendance,
                    r.IsOnline,
                    r.TravelAllowance,
                    r.IsWeekenOrHoliday,
                    r.WeekendFee)
            ));
        }

        public async Task UpdateAttendancesAsync(IEnumerable<Guid> attendancesIds)
        {
            if (!attendancesIds.Any()) return;

            await _db.Attendances
                .Where(a => attendancesIds.Contains(a.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsPaid, true));

        }

        public async Task AddAsync(Invoice invoice)
        {
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync();
        }

        //public async Task<IEnumerable<int>> AddInvoicesAsync(IEnumerable<Invoice> invoices)
        //{
        //    if (invoices == null || !invoices.Any())
        //        return Enumerable.Empty<int>();

        //    // 1. Add the entire collection to the change tracker in one go
        //    _db.Invoices.AddRange(invoices);

        //    // 2. Save everything in a single database transaction
        //    await _db.SaveChangesAsync();

        //    // 3. Return the newly generated IDs
        //    return invoices.Select(i => i.Id);
        //}

        public async Task<(int invoiceId, string InvoiceName)> CreateInvoiceAsync(Guid payerId,
    IEnumerable<Guid> attendanceIds, string? requestedName)
        {
            // 1. Transaction ensures "All or Nothing"
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var now = DateTime.UtcNow;
                var invoice = new Invoice
                {
                    PayerId = payerId,
                    CreatedUTC = now,
                    // Use a temporary placeholder if name is missing
                    Name = string.IsNullOrWhiteSpace(requestedName) ? "PENDING" : requestedName.Trim()
                };

                if (!attendanceIds.Any()) 
                    throw new ArgumentException("Cannot create an invoice with no attendances.");

                foreach (var attendanceId in attendanceIds)
                {
                    invoice.Lines.Add(new InvoiceAttendance { AttendanceId = attendanceId });
                }

                _db.Invoices.Add(invoice);
                await _db.SaveChangesAsync(); // First save to get the ID

                // 2. Apply Fallback Name Rule
                if (invoice.Name == "PENDING")
                {
                    // Consistent use of 'now' for the name
                    invoice.Name = $"{now:yyyy-MM}-E-{invoice.Id}";
                    await _db.SaveChangesAsync(); // Second save to persist the name
                }

                await transaction.CommitAsync();
                return (invoice.Id, invoice.Name);
            }
            catch
            {
                // If anything fails, the invoice is never created
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteInvoiceAsync(int invoiceId)
        {
            int deletedRows = await _db.Invoices
                .Where(i => i.Id == invoiceId)
                .ExecuteDeleteAsync();

            if (deletedRows == 0)
            {
                throw new InvalidDataException($"Invoice {invoiceId} not found.");
            }
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesAsync()
        {
            return await _db.Invoices
                .AsNoTracking()
                .OrderBy(x => x.CreatedUTC)
                .AsSplitQuery()
                .Include(x => x.Payer)
                .Include(x => x.Lines)
                    .ThenInclude(x => x.Attendance)
                        .ThenInclude(x => x.Student)
                .ToListAsync();
        }
    }
}
