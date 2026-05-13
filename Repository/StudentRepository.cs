using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class StudentRepository : IStudentRepository
    {
        private readonly ApoloContext _db;

        public StudentRepository(ApoloContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<StudentSummary>> GetSudentsAsync()
        {
            return await _db.Students
                .AsNoTracking()
                .OrderBy(s => s.FirstName)
                .ThenBy(s => s.LastName)
                .Select(s => new StudentSummary(
                    s.Id,
                    s.FirstName,
                    s.LastName,
                    s.PayerId,
                    s.Payer != null ? s.Payer.FullName : "No Payer Assigned")) // Null safety
                .ToListAsync();
        }

        public async Task AddAsync(Student student)
        {
            // A student must have a valid Payer
            var payerExists = await _db.Payers.AnyAsync(p => p.Id == student.PayerId);
            if (!payerExists)
            {
                throw new InvalidOperationException($"Cannot save student: Payer with ID {student.PayerId} does not exist.");
            }

            try
            {
                _db.Students.Add(student);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // SQLite Error 19 is "Constraint Violation"
                throw new InvalidDataException($"This student already exists: {student.FullName}.", ex);
            }
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
    }
}
