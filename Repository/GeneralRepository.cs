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
            List<BillingDocument> invoices)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                _db.Services.AddRange(services);
                _db.Payers.AddRange(payers);
                _db.Students.AddRange(students);
                _db.Specifications.AddRange(specifications);
                _db.BillingDocuments.AddRange(invoices);
                _db.Lessons.AddRange(lessons);
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
            List<Specification> Specifications, List<Lesson> Lessons, List<BillingDocument> Invoices)>
            GetAllDataAsync()
        {
            return (
                await _db.Services.AsNoTracking().ToListAsync(),
                await _db.Payers.AsNoTracking().ToListAsync(),
                await _db.Students.AsNoTracking().ToListAsync(),
                await _db.Specifications.AsNoTracking().ToListAsync(),
                await _db.Lessons.AsNoTracking().ToListAsync(),
                await _db.BillingDocuments.AsNoTracking().ToListAsync()
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
                        .SelectMany(s => s.Lessons)
                        .Select(l => (DateOnly?)l.Date)
                        .Max()
                })
                .AsNoTracking()
                .OrderBy(p => p.LastLessonDate) // Show oldest/inactive first
                .ToListAsync();
        }

        public async Task<List<PayerOption>> GetPayersFromArchiveAsync()
        {
            return await _archiveDb.Payers
                .AsNoTracking()
                 .OrderBy(s => s.FirstName)
                 .ThenBy(s => s.LastName)
                 .Select(p => new PayerOption(
                     p.Id,
                     p.FullName))
                 .ToListAsync();
        }

        public async Task RetrieveDataFromArchiveAsync(List<Guid> payerIds)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            using var archiveTransaction = await _archiveDb.Database.BeginTransactionAsync();
            try
            {
                _db.ChangeTracker.Clear();
                _archiveDb.ChangeTracker.Clear();

                // 1. Identify Payers and Students
                var payersToMove = await _archiveDb.Payers
                    .AsNoTracking()
                    .Where(p => payerIds.Contains(p.Id))
                    .ToListAsync();

                var studentsToMove = await _archiveDb.Students
                    .AsNoTracking()
                    .Where(s => payerIds.Contains(s.PayerId))
                    .ToListAsync();

                var studentIds = studentsToMove.Select(s => s.Id).ToList();

                // 2. Identify Lessons and Invoices linked to these students
                var lessonsToMove = await _archiveDb.Lessons
                    .AsNoTracking()
                    .Where(l => studentIds.Contains(l.StudentId))
                    .ToListAsync();

                var billsToMove = await _archiveDb.BillingDocuments
                    .AsNoTracking()
                    .Where(b => payerIds.Contains(b.PayerId))
                    .ToListAsync();

                _db.Payers.AddRange(payersToMove);
                _db.Students.AddRange(studentsToMove);
                _db.Lessons.AddRange(lessonsToMove);
                _db.BillingDocuments.AddRange(billsToMove);

                await _db.SaveChangesAsync();

                // 4. MAIN SIDE: Clean up
                await _archiveDb.BillingDocuments.Where(i => payerIds.Contains(i.PayerId)).ExecuteDeleteAsync();
                await _archiveDb.Lessons.Where(l => studentIds.Contains(l.StudentId)).ExecuteDeleteAsync();
                await _archiveDb.Students.Where(s => payerIds.Contains(s.PayerId)).ExecuteDeleteAsync();
                await _archiveDb.Payers.Where(p => payerIds.Contains(p.Id)).ExecuteDeleteAsync();

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

        public async Task ArchiveOldDataAsync(List<Guid> payerIds)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();
            using var archiveTransaction = await _archiveDb.Database.BeginTransactionAsync();
            try
            {
                _db.ChangeTracker.Clear();
                _archiveDb.ChangeTracker.Clear();

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

                // 2. Identify Lessons and Invoices linked to these students
                var lessonsToMove = await _db.Lessons
                    .AsNoTracking()
                    .Where(l => studentIds.Contains(l.StudentId))
                    .ToListAsync();

                var billsToMove = await _db.BillingDocuments
                    .AsNoTracking()
                    .Where(b => payerIds.Contains(b.PayerId))
                    .ToListAsync();

                var specsToRemove = await _db.Specifications
                    .AsNoTracking()
                    .Where(s => studentIds.Contains(s.StudentId))
                    .ToListAsync();

                _archiveDb.Payers.AddRange(payersToMove);
                _archiveDb.Students.AddRange(studentsToMove);
                _archiveDb.Lessons.AddRange(lessonsToMove);
                _archiveDb.BillingDocuments.AddRange(billsToMove);

                await _archiveDb.SaveChangesAsync();

                // 4. MAIN SIDE: Clean up
                await _db.BillingDocuments.Where(i => payerIds.Contains(i.PayerId)).ExecuteDeleteAsync();
                await _db.Lessons.Where(l => studentIds.Contains(l.StudentId)).ExecuteDeleteAsync();
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
