using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class PayerRepository(ApoloContext context) : IPayerRepository
    {
        private readonly ApoloContext _context = context;

        public async Task<PayerSummary> GetPayerSummaryNoOutstandingAsync(Guid payerId)
        {
            var payer = await _context.Payers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == payerId)
                ?? throw new InvalidDataException("Payer not found.");

            return Helper.ConvertToPayerSummary(payer, 0);
        }

        public async Task<IEnumerable<PayerSummary>> GetPayersAsync()
        {
            var rawData = await _context.Payers
                .AsNoTracking()
                .Select(p => new
                {
                    p.Id,
                    p.FirstName,
                    p.LastName,
                    Outstanding = p.Students
                        .SelectMany(s => s.Lessons)
                        .Where(l => !l.IsPaid)
                        .Sum(l => (decimal?)l.FinalPrice) ?? 0m,
                    p.Address,
                    p.ZipCode,
                    p.City,
                    p.TaxId
                })
                .ToListAsync(); 

            return rawData
                .OrderByDescending(x => x.Outstanding) 
                .Select(x => new PayerSummary(
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.Outstanding,
                    x.Address,
                    x.ZipCode,
                    x.City,
                    x.TaxId
                ));
        }

        public async Task<IEnumerable<PayerOption>> GetPayerOptionsAsync()
        {
            return await _context.Payers
                 .AsNoTracking()
                 .OrderBy(s => s.FirstName)
                 .ThenBy(s => s.LastName)
                 .Select(p => Helper.ConvertToPayerOption(p))
                 .ToListAsync();
        }

        public async Task<IEnumerable<PayerOption>> GetPayerOptionsByUnbilledLessons()
        {
            return await _context.Payers
                 .AsNoTracking()
                 .Select(p => new
                 {
                     Payer = p,
                     UnbilledCount = p.Students
                         .SelectMany(s => s.Lessons)
                         .Count(l => l.BillingDocumentId == null && !l.IsPaid)
                 })
                 .OrderByDescending(x => x.UnbilledCount)
                 .ThenBy(x => x.Payer.FirstName)
                 .ThenBy(x => x.Payer.LastName)
                 .Select(x => new PayerOption
                 (
                     x.Payer.Id,
                     x.UnbilledCount > 0
                         ? $"{x.Payer.FirstName} {x.Payer.LastName} - {x.UnbilledCount} lesson{(x.UnbilledCount == 1 ? "" : "s")}"
                         : $"{x.Payer.FirstName} {x.Payer.LastName}"
                 ))
                 .ToListAsync();
        }

        public async Task AddAsync(Payer payer)
        {
            try
            {
                _context.Payers.Add(payer);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidDataException($"This payer already exists: {payer.FullName}.", ex);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            // Check for existence and relations without loading all student objects
            var hasStudents = await _context.Students.AnyAsync(s => s.PayerId == id);

            if (hasStudents)
            {
                throw new InvalidOperationException("Cannot delete: Payer has associated students.");
            }

            var entity = await _context.Payers.FindAsync(id)
                         ?? throw new ArgumentNullException("Payer not found.");

            _context.Payers.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Guid payerId, string firstName, string lastName, string address, string zipCode, string city, string taxId)
        {
            var entity = await _context.Payers.FirstOrDefaultAsync(p => p.Id == payerId) ?? 
                throw new ArgumentNullException("Payer not found.");

            entity.FirstName = firstName;
            entity.LastName = lastName;
            entity.Address = address;
            entity.ZipCode = zipCode;
            entity.City = city;
            entity.TaxId = taxId;

            await _context.SaveChangesAsync();
        }
    }
}
