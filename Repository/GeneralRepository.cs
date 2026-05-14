using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class GeneralRepository : IGeneralRepository
    {
        private readonly ApoloContext _db;
        private readonly ApoloArchiveContext _archiveDb;

        public GeneralRepository(ApoloContext db, ApoloArchiveContext archiveDb)
        {
            _db = db;
            _archiveDb = archiveDb;
        }

        public async Task ImportAllDataAsync(
            List<Service> services,
            List<Payer> payers,
            List<Student> students,
            List<Specification> specifications,
            List<Lesson> lessons,
            List<Invoice> invoices)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Services.AddRange(services);
                _db.Payers.AddRange(payers);
                _db.Students.AddRange(students);
                _db.Specifications.AddRange(specifications);
                _db.Lessons.AddRange(lessons);
                _db.Invoices.AddRange(invoices);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<(List<Service> Services, List<Payer> Payers, List<Student> Students,
            List<Specification> Specifications, List<Lesson> Lessons, List<Invoice> Invoices)>
            GetAllDataAsync()
        {
            return (
                await _db.Services.AsNoTracking().ToListAsync(),
                await _db.Payers.AsNoTracking().ToListAsync(),
                await _db.Students.AsNoTracking().ToListAsync(),
                await _db.Specifications.AsNoTracking().ToListAsync(),
                await _db.Lessons.Include(l => l.Attendances).AsNoTracking().ToListAsync(),
                await _db.Invoices.Include(i => i.Lines).AsNoTracking().ToListAsync()
            );
        }

        public async Task ClearDatabaseAsync()
        {
            await _db.Database.EnsureDeletedAsync();
            await _db.Database.EnsureCreatedAsync();
        }

        public async Task ClearArchiveAsync()
        {
            await _archiveDb.Database.EnsureDeletedAsync();
            await _archiveDb.Database.EnsureCreatedAsync();
        }

        public async Task<List<PayerActivityInfo>> GetPayersWithActivityAsync()
        {
            return await _db.Payers
                .Select(p => new PayerActivityInfo
                {
                    PayerId = p.Id,
                    PayerName = p.FullName,
                    LastLessonDate = p.Students
                        .SelectMany(s => s.Attendances)
                        .Select(a => (DateOnly?)a.Lesson.Date)
                        .Max()
                })
                .AsNoTracking()
                .OrderBy(p => p.LastLessonDate) // Show oldest/inactive first
                .ToListAsync();
        }

        public async Task ArchiveOldDataAsync(List<Guid> payerIds)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            using var archiveTransaction = await _archiveDb.Database.BeginTransactionAsync();
            try
            {
                // 1. Identify Payers and Students
                var payersToMove = await _db.Payers
                    .AsNoTracking()
                    .Where(p => payerIds.Contains(p.Id))
                    .ToListAsync();

                var studentsToMove = await _db.Students
                    .AsNoTracking()
                    .Where(s => payerIds.Contains(s.PayerId))
                    .ToListAsync();

                var studentIds = studentsToMove.Select(s => s.Id).ToList();

                // 2. Identify Attendances and Invoices linked to these students
                var attendancesToMove = await _db.Attendances
                    .AsNoTracking()
                    .Where(a => studentIds.Contains(a.StudentId))
                    .ToListAsync();

                var invoicesToMove = await _db.Invoices
                    .AsNoTracking()
                    .Include(i => i.Lines)
                    .Where(i => payerIds.Contains(i.PayerId))
                    .ToListAsync();

                var specsToRemove = await _db.Specifications
                    .AsNoTracking()
                    .Where(s => studentIds.Contains(s.StudentId))
                    .ToListAsync();

                // 3. ARCHIVE SIDE: Insert data
                var lessonIds = attendancesToMove.Select(s => s.LessonId).ToHashSet();
                var lessonsToCopy = await _db.Lessons
                    .AsNoTracking()
                    .Where(l => lessonIds.Contains(l.Id))
                    .ToListAsync();

                foreach (var lesson in lessonsToCopy)
                {
                    if (!await _archiveDb.Lessons.AnyAsync(l => l.Id == lesson.Id))
                        _archiveDb.Lessons.Add(lesson);
                }

                _archiveDb.Payers.AddRange(payersToMove);
                _archiveDb.Students.AddRange(studentsToMove);
                _archiveDb.Attendances.AddRange(attendancesToMove);
                _archiveDb.Invoices.AddRange(invoicesToMove);

                await _archiveDb.SaveChangesAsync();

                // 4. MAIN SIDE: Clean up
                await _db.Invoices.Where(i => payerIds.Contains(i.PayerId)).ExecuteDeleteAsync();
                await _db.Attendances.Where(a => studentIds.Contains(a.StudentId)).ExecuteDeleteAsync();
                await _db.Lessons.Where(l => !l.Attendances.Any()).ExecuteDeleteAsync();
                await _db.Students.Where(s => payerIds.Contains(s.PayerId)).ExecuteDeleteAsync();
                await _db.Payers.Where(p => payerIds.Contains(p.Id)).ExecuteDeleteAsync();

                await transaction.CommitAsync();
                await archiveTransaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                await archiveTransaction.RollbackAsync();
                throw;
            }
        }
    }
}
