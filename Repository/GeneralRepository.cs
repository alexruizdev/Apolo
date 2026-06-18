using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class GeneralRepository(ApoloContext context, ApoloArchiveContext archiveDb) : IGeneralRepository
    {
        private readonly ApoloContext _context = context;
        private readonly ApoloArchiveContext _archiveDb = archiveDb;

        public async Task ImportAllDataAsync(
            List<Service> services,
            List<Payer> payers,
            List<Student> students,
            List<Specification> specifications,
            List<Lesson> lessons,
            List<BillingDocument> invoices)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Services.AddRange(services);
                _context.Payers.AddRange(payers);
                _context.Students.AddRange(students);
                _context.Specifications.AddRange(specifications);
                _context.BillingDocuments.AddRange(invoices);
                _context.Lessons.AddRange(lessons);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ImportArchiveAsync(
            List<Payer> payers,
            List<Student> students,
            List<Lesson> lessons,
            List<BillingDocument> invoices)
        {
            using var transaction = await _archiveDb.Database.BeginTransactionAsync();
            try
            {
                _archiveDb.Payers.AddRange(payers);
                _archiveDb.Students.AddRange(students);
                _archiveDb.BillingDocuments.AddRange(invoices);
                _archiveDb.Lessons.AddRange(lessons);
                await _archiveDb.SaveChangesAsync();
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
                await _context.Services.AsNoTracking().ToListAsync(),
                await _context.Payers.AsNoTracking().ToListAsync(),
                await _context.Students.AsNoTracking().ToListAsync(),
                await _context.Specifications.AsNoTracking().ToListAsync(),
                await _context.Lessons.AsNoTracking().ToListAsync(),
                await _context.BillingDocuments.AsNoTracking().ToListAsync()
            );
        }

        public async Task<(List<Service> Services, List<Payer> Payers, List<Student> Students,
            List<Specification> Specifications, List<Lesson> Lessons, List<BillingDocument> Invoices)>
            ExportArchiveAsync()
        {
            return (
                new List<Service>(),
                await _archiveDb.Payers.AsNoTracking().ToListAsync(),
                await _archiveDb.Students.AsNoTracking().ToListAsync(),
                new List<Specification>(),
                await _archiveDb.Lessons.AsNoTracking().ToListAsync(),
                await _archiveDb.BillingDocuments.AsNoTracking().ToListAsync()
            );
        }

        public async Task ClearDatabaseAsync()
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
        }

        public async Task ClearArchiveAsync()
        {
            await _archiveDb.Database.EnsureDeletedAsync();
            await _archiveDb.Database.EnsureCreatedAsync();
        }

        public async Task<List<PayerActivityInfo>> GetPayersWithActivityAsync()
        {
            return await _context.Payers
                .Select(p => new PayerActivityInfo
                {
                    PayerId = p.Id,
                    PayerName = p.FullName,
                    LastLessonDate = p.Students.SelectMany(s => s.Lessons).Max(l => (DateOnly?)l.Date)
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
                 .Select(p => Helper.ConvertToPayerOption(p))
                 .ToListAsync();
        }

        public async Task RetrieveDataFromArchiveAsync(List<Guid> payerIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            using var archiveTransaction = await _archiveDb.Database.BeginTransactionAsync();
            try
            {
                _context.ChangeTracker.Clear();
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

                _context.Payers.AddRange(payersToMove);
                _context.Students.AddRange(studentsToMove);
                _context.Lessons.AddRange(lessonsToMove);
                _context.BillingDocuments.AddRange(billsToMove);

                await _context.SaveChangesAsync();

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
            using var transaction = await _context.Database.BeginTransactionAsync();
            using var archiveTransaction = await _archiveDb.Database.BeginTransactionAsync();
            try
            {
                _context.ChangeTracker.Clear();
                _archiveDb.ChangeTracker.Clear();

                // 1. Identify Payers and Students
                var payersToMove = await _context.Payers
                    .AsNoTracking()
                    .Where(p => payerIds.Contains(p.Id))
                    .ToListAsync();

                var studentsToMove = await _context.Students
                    .AsNoTracking()
                    .Where(s => payerIds.Contains(s.PayerId))
                    .ToListAsync();

                var studentIds = studentsToMove.Select(s => s.Id).ToList();

                // 2. Identify Lessons and Invoices linked to these students
                var lessonsToMove = await _context.Lessons
                    .AsNoTracking()
                    .Where(l => studentIds.Contains(l.StudentId))
                    .ToListAsync();

                var billsToMove = await _context.BillingDocuments
                    .AsNoTracking()
                    .Where(b => payerIds.Contains(b.PayerId))
                    .ToListAsync();

                var specsToRemove = await _context.Specifications
                    .AsNoTracking()
                    .Where(s => studentIds.Contains(s.StudentId))
                    .ToListAsync();

                _archiveDb.Payers.AddRange(payersToMove);
                _archiveDb.Students.AddRange(studentsToMove);
                _archiveDb.Lessons.AddRange(lessonsToMove);
                _archiveDb.BillingDocuments.AddRange(billsToMove);

                await _archiveDb.SaveChangesAsync();

                // 4. MAIN SIDE: Clean up
                await _context.BillingDocuments.Where(i => payerIds.Contains(i.PayerId)).ExecuteDeleteAsync();
                await _context.Lessons.Where(l => studentIds.Contains(l.StudentId)).ExecuteDeleteAsync();
                await _context.Students.Where(s => payerIds.Contains(s.PayerId)).ExecuteDeleteAsync();
                await _context.Payers.Where(p => payerIds.Contains(p.Id)).ExecuteDeleteAsync();

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
