using Microsoft.EntityFrameworkCore;
using Models;
using System.Data;

namespace Repository
{
    public sealed class BillingRepository(ApoloContext context) : IBillingRepository
    {
        private readonly ApoloContext _context = context;

        public async Task<IEnumerable<LessonLine>> GetUnbilledLessonsAsync(Guid payerId)
        {
            // 1. Fetch only the raw 'ingredients' from SQL
            return await _context.Lessons
                .AsNoTracking()
                .Where(l => l.Student.PayerId == payerId && l.BillingDocumentId == null && !l.IsPaid)
                .OrderBy(l => l.Date)
                .Select(l => new LessonLine(l.Id, l.StudentId, l.Date, l.Name, l.Student.FullName, l.FinalPrice, l.IsPaid))
                .ToListAsync();
        }

        public async Task<IEnumerable<LessonLine>> GetLessonsFromBillAsync(Guid billId)
        {
            // 1. Fetch only the raw 'ingredients' from SQL
            return await _context.Lessons
                .AsNoTracking()
                .Where(l => l.BillingDocumentId == billId)
                .OrderBy(l => l.Date)
                .Select(l => new LessonLine(l.Id, l.StudentId, l.Date, l.Name, l.Student.FullName, l.FinalPrice, l.IsPaid))
                .ToListAsync();
        }

        public async Task<BillingDocument> CreateBillAsync(Guid payerId, List<Guid> ids, DocumentType type)
        {
            if (ids.Count == 0)
                throw new ArgumentException($"Cannot create {type} without any lesson.");

            var lessonsToBill = await _context.Lessons
                .Where(l => ids.Contains(l.Id) && l.BillingDocumentId == null)
                .ToListAsync();

            if (lessonsToBill.Count != ids.Count)
                throw new ArgumentException($"Invalid lessons selected.");


            var now = DateTime.UtcNow;
            var maxSequence = await _context.BillingDocuments
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

            _context.BillingDocuments.Add(doc);
            await _context.SaveChangesAsync();

            return doc;
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.BillingDocuments.FindAsync(id)
                         ?? throw new KeyNotFoundException($"Billing document with ID {id} was not found.");

            _context.BillingDocuments.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<BillingDocument>> GetBillSuggestionsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return [];

            var normalizedTerm = searchTerm.Trim();

            // DocumentNumber is computed in C#, so we filter in-memory to match the exact UI format.
            var documents = await _context.BillingDocuments
                .AsNoTracking()
                .OrderByDescending(b => b.CreatedUTC)
                .ToListAsync();

            return documents
                .Where(b => b.DocumentNumber.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();
        }
    }
}
