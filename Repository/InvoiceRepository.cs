using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class InvoiceRepository
    {
        private readonly ApoloContext _db;

        public InvoiceRepository(ApoloContext db)
        {
            _db = db;
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

        public async Task<IEnumerable<InvoiceAttendance>> GetInvoiceAttendancesAsync(Guid payerId)
        {
            var result = await _db.Attendances
                .AsNoTracking()
                .Where(a => !a.IsPaid && a.Student.PayerId == payerId)
                .Select(a => new InvoiceAttendance
                (
                    a.Id,
                    a.LessonId,
                    a.Lesson.Date,
                    a.Lesson.Name,
                    a.StudentId,
                    a.Student.FullName,
                    a.Lesson.PricePerStudent
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

    }
}
