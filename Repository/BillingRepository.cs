using Microsoft.EntityFrameworkCore;
using Models;
using System.Data;

namespace Repository
{
    public sealed class BillingRepository : IBillingRepository
    {
        private readonly ApoloContext _db;

        public BillingRepository(ApoloContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<LessonLine>> GetUnbilledLessonsAsync(Guid payerId)
        {
            // 1. Fetch only the raw 'ingredients' from SQL
            return await _db.Lessons
                .AsNoTracking()
                .Where(l => l.Student.PayerId == payerId && l.BillingDocumentId == null && !l.IsPaid)
                .OrderBy(l => l.Date)
                .Select(l => new LessonLine(l.Id, l.StudentId, l.Date, l.Name, l.Student.FullName, l.FinalPrice, l.IsPaid))
                .ToListAsync();
        }

        public async Task UpdateLessonsAsync(IEnumerable<Guid> lessonsIds, bool isPaid)
        {
            if (!lessonsIds.Any()) return;

            await _db.Lessons
                .Where(l => lessonsIds.Contains(l.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsPaid, isPaid));
        }

        public async Task<string> CreateBillAsync(Guid payerId, List<Guid> ids, DocumentType type)
        {
            if (ids.Count == 0)
                throw new ArgumentException($"Cannot create {type.ToString()} without any lesson.");

            var lessonsToBill = await _db.Lessons
                .Where(l => ids.Contains(l.Id) && l.BillingDocumentId == null)
                .ToListAsync();

            if (lessonsToBill.Count != ids.Count)
                throw new ArgumentException($"Invalid lessons selected.");


            var now = DateTime.UtcNow;
            var maxSequence = await _db.BillingDocuments
                .Where(bd => bd.Type == type && bd.CreatedUTC.Year == now.Year)
                .MaxAsync(bd => (int?)bd.SequenceNumber) ?? 0;

            var doc = new BillingDocument (now)
            {
                PayerId = payerId,
                Type = type,
                SequenceNumber = maxSequence + 1
            };

            foreach ( var lesson in lessonsToBill )
                doc.Lines.Add( lesson );

            _db.BillingDocuments.Add(doc);
            await _db.SaveChangesAsync();

            return doc.DocumentNumber;
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _db.BillingDocuments.FindAsync(id)
                ?? throw new ArgumentNullException("Billing document not found");

            _db.BillingDocuments.Remove(entity);
            await _db.SaveChangesAsync();
        }
    }
}
