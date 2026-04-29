using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class ServiceRepository
    {
        private readonly ApoloContext _db;

        public ServiceRepository(ApoloContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<ServiceSummary>> GetServicesAsync()
        {
            return await _db.Services
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new ServiceSummary(
                    s.Id,
                    s.Name,
                    s.IsPricePerHour,
                    (double)s.Price))
                .ToListAsync();
        }

        public async Task AddAsync(Service service)
        {
            try
            {
                _db.Services.Add(service);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // SQLite Error 19 is "Constraint Violation"
                throw new InvalidDataException($"A service with this name already exists: {service.Name}.", ex);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _db.Services.FirstOrDefaultAsync(s => s.Id == id);

            if (entity is null)
            {
                throw new ArgumentNullException("Service not found.");
            }

            _db.Services.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync (Guid id, string name, bool isPricePerHour, decimal price)
        {
            var entity = await _db.Services.FirstOrDefaultAsync(s => s.Id == id)
                 ?? throw new ArgumentNullException("Service not found.");

            // Only check uniqueness if the name is DIFFERENT from the current one
            if (!string.Equals(entity.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                var nameTaken = await _db.Services.AnyAsync(s => s.Name.ToLower() == name.ToLower());
                if (nameTaken) throw new InvalidDataException($"Another service already uses: {name}.");
            }

            entity.Name = name;
            entity.IsPricePerHour = isPricePerHour;
            entity.Price = price;

            await _db.SaveChangesAsync();
        }
    }
}
