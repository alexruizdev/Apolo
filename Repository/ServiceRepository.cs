using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class ServiceRepository(ApoloContext context) : IServiceRepository
    {
        private readonly ApoloContext _context = context;

        public async Task<IEnumerable<ServiceSummary>> GetServicesAsync()
        {
            return await _context.Services
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(Helper.AsServiceSummary)
                .ToListAsync();
        }

        public async Task AddAsync(Service service)
        {
            try
            {
                _context.Services.Add(service);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // SQLite Error 19 is "Constraint Violation"
                throw new InvalidDataException($"A service with this name already exists: {service.Name}.", ex);
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Services.FirstOrDefaultAsync(s => s.Id == id) ??
                throw new KeyNotFoundException($"Service with ID {id} was not found.");

            _context.Services.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync (Guid id, string name, bool isPricePerHour, decimal price)
        {
            var entity = await _context.Services.FirstOrDefaultAsync(s => s.Id == id)
                 ?? throw new KeyNotFoundException($"Service with ID {id} was not found.");

            // Only check uniqueness if the name is DIFFERENT from the current one
            if (!string.Equals(entity.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                var nameTaken = await _context.Services.AnyAsync(s => EF.Functions.Like(s.Name, name));
                if (nameTaken) throw new InvalidDataException($"Another service already uses: {name}.");
            }

            entity.Name = name;
            entity.IsPricePerHour = isPricePerHour;
            entity.Price = price;

            await _context.SaveChangesAsync();
        }
    }
}
