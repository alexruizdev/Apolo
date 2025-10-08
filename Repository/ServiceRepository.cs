using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
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
            var result = await _db.Services
                .AsNoTracking()
                .Select(s => new ServiceSummary(
                    s.Id,
                    s.Name,
                    s.PricePerHour))
                .ToListAsync();
            return result.OrderBy(x => x.Name).ToList();
        }

        public async Task AddAsync(Service service)
        {
            var exists = await _db.Services.AnyAsync(s => s.Name.ToLower() == service.Name.ToLower());
            if (exists)
            {
                throw new InvalidDataException("A service with this name already exists.");
            }
            _db.Services.Add(service);
            await _db.SaveChangesAsync();
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

        public async Task UpdateAsync (Guid id, string name, decimal price)
        {
            var entity = await _db.Services.FirstOrDefaultAsync(s => s.Id == id);

            if (entity is null)
            {
                throw new ArgumentNullException("Service not found.");
            }

            // Uniqueness pre-check (ignore self)
            var nameTaken = await _db.Services.AnyAsync( s => s.Id != id && s.Name.ToLower() == name.ToLower());
            if (nameTaken)
            {
                throw new InvalidDataException("Another service already uses that name.");
            }

            entity.Name = name;
            entity.PricePerHour = price;

            await _db.SaveChangesAsync();
        }
    }
}
