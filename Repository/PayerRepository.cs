using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Models;

namespace Repository
{
    public sealed class PayerRepository : IPayerRepository
    {
        private readonly ApoloContext _db;

        public PayerRepository(ApoloContext db)
        {
            _db = db;
        }

        public async Task<PayerSummary> GetPayerSummaryNoOutstandingAsync(Guid payerId)
        {
            var payer = await _db.Payers
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == payerId)
                ?? throw new InvalidDataException("Payer not found.");

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

        public async Task<IEnumerable<PayerSummary>> GetPayersAsync()
        {
            // 1. Fetch only the 'ingredients' needed for the calculation
            var unpaidAttendances = await _db.Attendances
                .AsNoTracking()
                .Where(a => !a.IsPaid)
                .Select(a => new
                {
                    a.Student.PayerId,
                    // We pull the Lesson properties needed for the static GetPrice method
                    a.Lesson.IsPricePerHour,
                    a.Lesson.DurationMinutes,
                    a.Lesson.PricePerAttendance,
                    a.Lesson.IsOnline,
                    a.Lesson.TravelAllowance,
                    a.Lesson.IsWeekenOrHoliday,
                    a.Lesson.WeekendFee
                })
                .ToListAsync();

            // 2. Group by Payer and calculate using the Model's logic
            var debtMap = unpaidAttendances
                .GroupBy(a => a.PayerId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(a => Lesson.GetPrice(
                        1, // Calculating price per student
                        a.IsPricePerHour,
                        a.DurationMinutes,
                        a.PricePerAttendance,
                        a.IsOnline,
                        a.TravelAllowance,
                        a.IsWeekenOrHoliday,
                        a.WeekendFee))
                );

            // 3. Get the Payer list and inject the calculated debt
            var payers = await _db.Payers
                .AsNoTracking()
                .Select(p => new PayerSummary(
                    p.Id,
                    p.FirstName,
                    p.LastName,
                    debtMap.ContainsKey(p.Id) ? debtMap[p.Id] : 0m,
                    p.Address,
                    p.ZipCode,
                    p.City,
                    p.TaxId
                ))
                .ToListAsync();

            return payers.OrderBy(x => x.FullName).ToList();
        }

        public async Task<IEnumerable<PayerOption>> GetPayerOptionsAsync()
        {
            return await _db.Payers
                 .AsNoTracking()
                 .OrderBy(s => s.FirstName)
                 .ThenBy(s => s.LastName)
                 .Select(p => new PayerOption(
                     p.Id,
                     p.FullName))
                 .ToListAsync();
        }

        public async Task UpsertAsync(Payer payer)
        {
            var existingPayer = await _db.Payers.FindAsync(payer.Id);

            if (existingPayer == null)
            {
                _db.Payers.Add(payer);
            }
            else
            {
                // Update only the values, keeping the entity tracked
                _db.Entry(existingPayer).CurrentValues.SetValues(payer);
            }

            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            // Check for existence and relations without loading all student objects
            var hasStudents = await _db.Students.AnyAsync(s => s.PayerId == id);

            if (hasStudents)
            {
                throw new InvalidOperationException("Cannot delete: Payer has associated students.");
            }

            var entity = await _db.Payers.FindAsync(id)
                         ?? throw new ArgumentNullException("Payer not found.");

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
