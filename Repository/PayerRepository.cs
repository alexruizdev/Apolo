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

            var result = await _db.Payers
                 .Select(p => new PayerSummary(
                     p.Id,
                     p.FirstName,
                     p.LastName,
                     p.Students
                              .SelectMany(s => s.Attendances)
                              .Where(a => !a.IsPaid)
                              .Sum(a => (decimal?)a.Price) ?? 0m
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

        public async Task UpdateAsync(Guid payerId, string firstName, string lastName)
        {
            var entity = await _db.Payers.FirstOrDefaultAsync(p => p.Id == payerId);
            
            if (entity is null)
            {
                throw new ArgumentNullException("Payer not found.");
            }

            entity.FirstName = firstName;
            entity.LastName = lastName;

            await _db.SaveChangesAsync();
        }
    }
}
