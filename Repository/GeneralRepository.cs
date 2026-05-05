using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class GeneralRepository : IGeneralRepository
    {
        private readonly ApoloContext _db;

        public GeneralRepository(ApoloContext db)
        {
            _db = db;
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
    }
}
