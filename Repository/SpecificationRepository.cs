using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class SpecificationRepository
    {
        private readonly ApoloContext _db;

        public SpecificationRepository(ApoloContext db)
        {
            _db = db;
        }

        public async Task AddSpecificationAsync(Specification specification)
        {
            _db.Specifications.Add(specification);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<SpecificationSummary>> GetSpecificationsAsync()
        {
            return await _db.Specifications
                .AsNoTracking()
                .OrderBy(sp => sp.Student.FirstName)
                .ThenBy(sp => sp.Name)
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
                    sp.IsWeekenOrHoliday
                ))
                .ToListAsync();
        }

        public async Task<IEnumerable<SpecificationOption>> GetSpecificationsForStudentAsync(IEnumerable<Guid> studentsIds)
        {
            var ids = studentsIds.Distinct().ToList();
            if (ids.Count == 0) return new List<SpecificationOption>();

            return await _db.Specifications
                .AsNoTracking()
                .Where(sp => ids.Contains(sp.StudentId))
                .Select(sp => new SpecificationOption(
                    sp.Id,
                    $"{sp.Name} - {sp.Student.FullName}".Trim(),
                    sp.ServiceId,
                    (double?)sp.Price,
                    sp.DurationMinutes,
                    sp.IsOnline,
                    sp.IsWeekenOrHoliday))
                .ToListAsync();

        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _db.Specifications.FirstOrDefaultAsync(s => s.Id == id);

            if (entity is null)
            {
                throw new ArgumentNullException("Specification not found.");
            }

            _db.Specifications.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Guid id, Guid serviceId, string name, int duration, decimal? price, bool isOnline, bool isWeekend)
        {
            var entity = await _db.Specifications.FirstOrDefaultAsync(sp => sp.Id == id);

            if (entity is null)
            {
                throw new ArgumentNullException("Specification not found.");
            }

            entity.ServiceId = serviceId;   
            entity.Name = name;
            entity.DurationMinutes = duration;
            entity.Price = price;
            entity.IsOnline = isOnline;
            entity.IsWeekenOrHoliday = isWeekend;

            await _db.SaveChangesAsync();
        }
    }
}
