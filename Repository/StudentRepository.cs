using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class StudentRepository
    {
        private readonly ApoloContext _db;

        public StudentRepository(ApoloContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<PayerOption>> GetPayerOptionsAsync()
        {
            var result = await _db.Payers
                 .AsNoTracking()
                 .Select(p => new PayerOption(
                     p.Id,
                     p.FullName))
                 .ToListAsync();
            return result.OrderBy(x => x.FullName).ToList();
        }

        public async Task<IEnumerable<StudentSummary>> GetSudentsAsync()
        {
            var result = await _db.Students
                .AsNoTracking()
                .Select(s => new StudentSummary(
                    s.Id,
                    s.FirstName,
                    s.LastName,
                    s.PayerId,
                    s.Payer.FullName))
                .ToListAsync();
            return result.OrderBy(x => x.FirstName).ThenBy(x => x.LastName).ToList();
        }

        public async Task UpsertAsync(Student student)
        {
            _db.Students.Add(student);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _db.Students.FirstOrDefaultAsync(s => s.Id == id);

            if (entity is null)
            {
                throw new ArgumentNullException("Student not found.");
            }

            _db.Students.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Guid studentId, Guid payerId, string firstName, string lastName)
        {
            var entity = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId);

            if (entity is null)
            {
                throw new ArgumentNullException("Student not found.");
            }

            entity.FirstName = firstName;
            entity.LastName = lastName;
            entity.PayerId = payerId;

            await _db.SaveChangesAsync();
        }
    }
}
