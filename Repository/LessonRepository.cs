using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class LessonRepository : ILessonRepository
    {
        private readonly ApoloContext _db;

        public LessonRepository(ApoloContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<LessonSummary>> GetLessonsAsync(bool showOnlyUnpaid, int? months)
        {
            // 2. Start the query with AsNoTracking (crucial for read-only performance)
            var query = _db.Lessons.AsNoTracking();

            // 3. Apply Filters
            if (showOnlyUnpaid)
                query = query.Where(l => !l.IsPaid);

            if (months is not null)
            {
                var dateThreshold = DateOnly.FromDateTime(DateTime.Now.AddMonths(-months.Value));
                query = query.Where(l => l.Date >= dateThreshold);
            }

            return await query
                .OrderByDescending(l => l.Date)
                .Select(l => new LessonSummary(
                    l.Id,
                    l.Date,
                    l.Name,
                    l.FinalPrice,
                    l.IsPaid,
                    l.StudentId,
                    l.Student.FullName,
                    l.BillingDocumentId,
                    l.BillingDocument == null ? string.Empty : l.BillingDocument.DocumentNumber,
                    l.IsPricePerHour,
                    l.DurationMinutes,
                    l.BasePrice,
                    l.IsOnline,
                    l.TravelAllowance,
                    l.IsWeekendOrHoliday,
                    l.WeekendFee,
                    l.Tip,
                    l.Notes))
                .ToListAsync();
        }

        public async Task AddLessonsAsync(List<Lesson> lessons)
        {
            _db.Lessons.AddRange(lessons);
            await _db.SaveChangesAsync();
        }

        public async Task<Lesson> AddLessonAsync(DateOnly date, string name, bool isPaid, Guid studentId,
            Guid? billingDocumentId, bool isPricePerHour, int? duration, decimal basePrice,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, decimal tip,
             string? notes)
        {
            var lesson = new Lesson(date, name, isPaid, studentId, billingDocumentId, isPricePerHour, duration, basePrice,
                isOnline, travelAllowance, isWeekendOrHoliday, weekendFee, tip, notes);

            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();

            return lesson;
        }

        public async Task<Lesson> UpdateLesson(Guid id, DateOnly date, string name, 
            bool isPricePerHour, int? duration, decimal pricePerStudent,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, decimal tip, string? note)
        {
            var entity = await _db.Lessons.FirstOrDefaultAsync(i => i.Id == id);

            if (entity is null)
                throw new InvalidDataException("Lesson not found.");

            entity.Date = date;
            entity.Name = name;
            var canEdit = entity.Set(isPricePerHour, duration, pricePerStudent, isOnline, travelAllowance,
                isWeekendOrHoliday, weekendFee);
            if (!canEdit)
            {
                var reason = entity.IsPaid ? "is marked as paid." : "is assigned to a ticket/invoice.";
                throw new InvalidDataException($"Lesson {entity.Name} can't be edited because {reason}");
            }
            entity.Tip = tip;
            entity.Notes = note;

            await _db.SaveChangesAsync();

            return entity;
        }

        public async Task UpdateLessonsPayment(IEnumerable<Guid> lessonsIds, bool isPaid)
        {
            if (!lessonsIds.Any()) return;

            await _db.Lessons
                .Where(l => lessonsIds.Contains(l.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.IsPaid, isPaid));
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _db.Lessons.FindAsync(id)
                         ?? throw new ArgumentNullException("Lesson not found.");

            _db.Lessons.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UnassignBillToLessons(IEnumerable<Guid> lessonsIds)
        {
            if (!lessonsIds.Any()) return;

            var lesssons = await _db.Lessons
                .Where(l => lessonsIds.Contains(l.Id))
                .ToListAsync();
            
            foreach(var lesson in lesssons)
                lesson.BillingDocumentId = null;

            await _db.SaveChangesAsync();
        }
    }
}
