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
            return await _db.Students
                 .AsNoTracking()
                 .OrderBy(s => s.FirstName)
                 .ThenBy(s => s.LastName)
                 .Select(p => new StudentOption(
                     p.Id,
                     p.FullName))
                 .ToListAsync();
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
                    s.IsPricePerHour,
                    (double)s.Price))
                .ToListAsync();
            return result.OrderBy(x => x.Name).ToList();
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

        public async Task AddLessonFromSpecificationAsync(Lesson lesson)
        {
            if (!lesson.Attendaces.Any())
            {
                throw new InvalidOperationException("Cannot create a lesson without any attendances.");
            }

            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();
        }
    }
}
