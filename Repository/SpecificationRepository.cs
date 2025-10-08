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

        public async Task<IEnumerable<StudentOption>> GetStudentOptionsAsync()
        {
            var result = await _db.Students
                 .AsNoTracking()
                 .Select(p => new StudentOption(
                     p.Id,
                     p.FullName))
                 .ToListAsync();
            return result.OrderBy(x => x.FullName).ToList();
        }

        public async Task AddSpecificationAsync(Specification specification)
        {
            _db.Specifications.Add(specification);
            await _db.SaveChangesAsync();
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

        public async Task<IEnumerable<SpecificationSummary>> GetSpecificationsAsync()
        {
            var result = await _db.Specifications
                .AsNoTracking()
                .Select(sp => new SpecificationSummary(
                    sp.Id,
                    sp.Name,
                    sp.StudentId,
                    sp.Student.FullName,
                    sp.ServiceId,
                    sp.Service.Name,
                    sp.DurationMinutes,
                    sp.IsOnline
                ))
                .ToListAsync();
            return result.OrderBy(x => x.StudentName).ThenBy(x => x.SpecificationName).ToList();
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

        public async Task UpdateAsync(Guid id, Guid serviceId, string name, int duration, bool isOnline)
        {
            var entity = await _db.Specifications.FirstOrDefaultAsync(sp => sp.Id == id);

            if (entity is null)
            {
                throw new ArgumentNullException("Specification not found.");
            }

            entity.ServiceId = serviceId;   
            entity.Name = name;
            entity.DurationMinutes = duration;
            entity.IsOnline = isOnline;

            await _db.SaveChangesAsync();
        }

        public async Task AddLessonFromSpecificationAsync(Lesson lesson)
        {
            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();
        }
    }
}
