using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository
{
    public sealed class LessonRepository
    {
        private readonly ApoloContext _db;

        public LessonRepository(ApoloContext db)
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

        // TODO: add threshold as input
        public async Task<IEnumerable<LessonSummary>> GetLessonsAsync(bool showOnlyUnpaid)
        {
            // 1. Calculate the date threshold once
            var dateThreshold = DateOnly.FromDateTime(DateTime.Now.AddMonths(-1));

            // 2. Start the query with AsNoTracking (crucial for read-only performance)
            var query = _db.Lessons.AsNoTracking();

            // 3. Apply Filters
            if (showOnlyUnpaid)
            {
                query = query.Where(l => l.Attendaces.Any(a => !a.IsPaid));
            }

            query = query.Where(l => l.Date >= dateThreshold);

            return await query
                .OrderByDescending(l => l.Date)
                .Select(l => new LessonSummary(
                    l.Id,
                    l.Name,
                    l.Date,
                    l.DurationMinutes,
                    l.IsOnline,
                    l.IsTotalPrice,
                    l.PricePerStudent,
                    l.Attendaces.Select(a => new AttendanceSummary(
                        a.Id,
                        a.StudentId,
                        a.Student.FullName, // EF handles the join automatically here
                        a.IsPaid
                    )).ToList()
                ))
                .ToListAsync();
        }

        private static List<decimal> EqualSplit(decimal total, int count)
        {
            if (count <= 0) return new List<decimal>();
            var cents = (int)Math.Round(total * 100m, 0, MidpointRounding.AwayFromZero);
            var baseShare = cents / count;
            var remainder = cents % count;

            var result = Enumerable.Repeat(baseShare / 100m, count).ToList();
            for (int i = 0; i < remainder; i++) result[i] += 0.01m;
            return result;
        }

        public async Task<Lesson> UpsertAsync(Lesson lesson)
        {
            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();
            return lesson;
        }

        public async Task<Lesson> CreateLesson(string name, DateOnly date, int duration, bool isOnline, bool isTotalPrice, decimal pricePerStudent, IReadOnlyList<Guid> studentIds)
        {
            var lesson = new Lesson
            {
                Name = name,
                Date = date,
                DurationMinutes = isTotalPrice ? 0 : duration, // TODO: enable duration for total price lessons
                IsOnline = isOnline,
                IsTotalPrice = isTotalPrice,
                PricePerStudent = pricePerStudent
            };

            _db.Lessons.Add(lesson);

            // split equally accross selected students
            for (int i = 0; i < studentIds.Count; i++)
            {
                lesson.Attendaces.Add(new Attendance
                {
                    StudentId = studentIds[i],
                    IsPaid = false
                });
            }

            await _db.SaveChangesAsync();

            return lesson;
        }

        public async Task<Lesson> UpdateLesson(Guid id, string name, DateOnly date, int duration, bool isOnline, bool isTotalPrice, decimal pricePerStudent)
        {
            var entity = await _db.Lessons.Include(l => l.Attendaces).FirstOrDefaultAsync(i => i.Id == id);

            if (entity is null)
                throw new InvalidDataException("Lesson not found.");

            entity.Name = name;
            entity.Date = date;
            entity.DurationMinutes = isTotalPrice ? 0 : duration;
            entity.IsOnline = isOnline;
            entity.IsTotalPrice = isTotalPrice;
            entity.PricePerStudent = pricePerStudent;

            await _db.SaveChangesAsync();

            return entity;
        }

        public async Task<Lesson> AddAttendanceAsync(Guid lessonId, IReadOnlyCollection<Guid> studentIds)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Attendaces)
                .FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson is null)
                throw new InvalidDataException("Lesson not found.");

            // Exclude students already present
            var existing = lesson.Attendaces.Select(a => a.StudentId).ToHashSet();
            var newIds = studentIds.Where(id => !existing.Contains(id)).ToList();
            if (newIds.Count == 0)
                throw new InvalidOperationException("No new students to be added.");

            var newAttendances = new List<Attendance>(newIds.Count);
            for (int i = 0; i < newIds.Count; i++)
            {
                newAttendances.Add(new Attendance
                {
                    LessonId = lessonId,
                    StudentId = newIds[i],
                    IsPaid = false
                });
            }

            _db.Attendances.AddRange(newAttendances);
            await _db.SaveChangesAsync();

            return lesson;
        }

        public async Task<Lesson> RemoveAttendanceAsync(Guid lessonId, Guid attendanceId)
        {
            var attendance = await _db.Attendances.FirstOrDefaultAsync(a => a.Id == attendanceId);
            if (attendance == null)
                throw new InvalidDataException("Attendance not found.");

            _db.Attendances.Remove(attendance);

            var lesson = await _db.Lessons.Include(l => l.Attendaces).FirstAsync(l => l.Id == lessonId);    
            await _db.SaveChangesAsync();

            if (lesson.Attendaces.Count == 0)
            {
                _db.Lessons.Remove(lesson);
            }
            await _db.SaveChangesAsync();

            return lesson;
        }

        public async Task<Lesson> UpdateAttendanceAsync(Guid lessonId, Guid attendanceId, bool isPaid)
        {
            var attendance = await _db.Attendances.FirstOrDefaultAsync(a => a.Id == attendanceId && a.LessonId == lessonId);
            if (attendance is null)
                throw new InvalidDataException("Attendance not found.");

            attendance.IsPaid = isPaid;

            var lesson = await _db.Lessons.Include(l => l.Attendaces).FirstAsync(l => l.Id == lessonId);

            await _db.SaveChangesAsync();

            return lesson;
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
                    sp.Service.Name,
                    sp.Service.PricePerHour,
                    sp.DurationMinutes,
                    sp.IsOnline))
                .ToListAsync();

        }

    }

}
