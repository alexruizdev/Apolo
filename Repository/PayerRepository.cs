using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Models;

namespace Repository
{
    public sealed class PayerRepository
    {
        private readonly ApoloContext _db;

        public PayerRepository(ApoloContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<PayerSummary>> GetPayersAsync()
        {
            var unpaid = await _db.Attendances
                .AsNoTracking()
                .Where(a => !a.IsPaid)
                .Select(a => new
                {
                    PayerId = a.Student.PayerId,
                    a.LessonId,
                    a.StudentId
                })
                .ToListAsync();

            if (unpaid.Count == 0)
            {
                var basicPayers = await _db.Payers
                    .AsNoTracking()
                    .Select(p => new PayerSummary(
                         p.Id,
                         p.FirstName,
                         p.LastName,
                         0m,
                         p.Address,
                         p.ZipCode,
                         p.City,
                         p.TaxId
                     ))
                     .ToListAsync();
                return basicPayers.OrderBy(x => x.FullName).ToList();
            }

            var lessonIds = unpaid.Select(u => u.LessonId).Distinct().ToList();

            var lessons = await _db.Lessons
                .AsNoTracking()
                .Where(l => lessonIds.Contains(l.Id))
                .ToDictionaryAsync(l => l.Id);

            var totalsByPayer = unpaid
                .GroupBy(u => u.PayerId)
                .ToDictionary(g => g.Key, g => g.Sum(u =>
                {
                    return lessons[u.LessonId].GetFinalPricePerStudent();
                }));

            var result = await _db.Payers
                 .Select(p => new PayerSummary(
                     p.Id,
                     p.FirstName,
                     p.LastName,
                     totalsByPayer.ContainsKey(p.Id) ? totalsByPayer[p.Id] : 0m,
                     p.Address,
                     p.ZipCode,
                     p.City,
                     p.TaxId
                 ))
                 .AsNoTracking()
                 .ToListAsync();
            return result.OrderBy(x => x.FullName).ToList();
        }

        public async Task UpsertAsync(Payer payer)
        {
            var existingPayer = await _db.Payers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == payer.Id);

            if (existingPayer == null)
            {
                _db.Payers.Add(payer);
            }
            else
            {
                _db.Payers.Update(payer);
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _db.Payers
                .Include(p => p.Students)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (entity is null)
            {
                throw new ArgumentNullException("Payer not found.");
            }

            if (entity.Students.Any())
            {
                throw new InvalidOperationException("Cannot delete this payer: it has associated students. Reassign or remove students first.");
            }

            _db.Payers.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Guid payerId, string firstName, string lastName, string address, string zipCode, string city, string taxId)
        {
            var entity = await _db.Payers.FirstOrDefaultAsync(p => p.Id == payerId);

            if (entity is null)
            {
                throw new ArgumentNullException("Payer not found.");
            }

            entity.FirstName = firstName;
            entity.LastName = lastName;
            entity.Address = address;
            entity.ZipCode = zipCode;
            entity.City = city;
            entity.TaxId = taxId;

            await _db.SaveChangesAsync();
        }
    }
}
