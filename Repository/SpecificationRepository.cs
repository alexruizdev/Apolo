using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class SpecificationRepository : ISpecificationRepository
    {
        private readonly ApoloContext _context;

        public SpecificationRepository(ApoloContext context)
        {
            _context = context;
        }

        public async Task AddSpecificationAsync(Specification specification)
        {
            _context.Specifications.Add(specification);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SpecificationSummary>> GetSpecificationsAsync()
        {
            return await _context.Specifications
                .AsNoTracking()
                .OrderByDescending(sp => sp.UsageCount)
                .Select(sp => new SpecificationSummary(
                    sp.Id,
                    sp.Name,
                    sp.StudentId,
                    sp.Student.FullName,
                    sp.ServiceId,
                    sp.Service.Name,
                    sp.DurationMinutes,
                    (double?)sp.Price,
                    sp.IsOnline,
                    sp.IsWeekendOrHoliday,
                    sp.UsageCount
                ))
                .ToListAsync();
        }

        public async Task IncrementUsageAsync(Guid id)
        {
            var entity = await _context.Specifications.FirstOrDefaultAsync(s => s.Id == id);
            if (entity is null)
                throw new ArgumentNullException("Specification not found.");
            
            entity.UsageCount++;
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<SpecificationOption>> GetSpecificationsForStudentAsync(IEnumerable<Guid> studentsIds)
        {
            var ids = studentsIds.Distinct().ToList();
            if (ids.Count == 0) return new List<SpecificationOption>();

            return await _context.Specifications
                .AsNoTracking()
                .Where(sp => ids.Contains(sp.StudentId))
                .Select(sp => new SpecificationOption(
                    sp.Id,
                    $"{sp.Name} - {sp.Student.FullName}".Trim(),
                    sp.ServiceId,
                    (double?)sp.Price,
                    sp.DurationMinutes,
                    sp.IsOnline,
                    sp.IsWeekendOrHoliday))
                .ToListAsync();

        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _context.Specifications.FirstOrDefaultAsync(s => s.Id == id);

            if (entity is null)
            {
                throw new ArgumentNullException("Specification not found.");
            }

            _context.Specifications.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Guid id, Guid serviceId, string name, int duration, decimal? price, bool isOnline, bool isWeekend)
        {
            var entity = await _context.Specifications.FirstOrDefaultAsync(sp => sp.Id == id);

            if (entity is null)
            {
                throw new ArgumentNullException("Specification not found.");
            }

            entity.ServiceId = serviceId;   
            entity.Name = name;
            entity.DurationMinutes = duration;
            entity.Price = price;
            entity.IsOnline = isOnline;
            entity.IsWeekendOrHoliday = isWeekend;

            await _context.SaveChangesAsync();
        }
    }
}
