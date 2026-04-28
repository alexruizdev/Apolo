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
                    s.IsPricePerHour,
                    (double)s.Price))
                .ToListAsync();
            return result.OrderBy(x => x.Name).ToList();
        }

        // TODO: add threshold as input
        public async Task<IEnumerable<LessonSummary>> GetLessonsAsync(bool showOnlyUnpaid, int? months)
        {
            // 2. Start the query with AsNoTracking (crucial for read-only performance)
            var query = _db.Lessons.AsNoTracking();

            // 3. Apply Filters
            if (showOnlyUnpaid)
            {
                query = query.Where(l => l.Attendaces.Any(a => !a.IsPaid));
            }

            if (months is not null)
            {
                var dateThreshold = DateOnly.FromDateTime(DateTime.Now.AddMonths(-months.Value));
                query = query.Where(l => l.Date >= dateThreshold);
            }

            return await query
                .OrderByDescending(l => l.Date)
                .Select(l => new LessonSummary(
                    l.Id,
                    l.Name,
                    l.Date,
                    l.IsPricePerHour,
                    l.DurationMinutes,
                    l.PricePerAttendance,
                    l.IsOnline,
                    l.TravelAllowance,
                    l.IsWeekenOrHoliday,
                    l.WeekendFee,
                    l.Notes,
                    l.Attendaces.Select(a => new AttendanceSummary(
                        a.Id,
                        a.StudentId,
                        a.Student.FullName, // EF handles the join automatically here
                        a.IsPaid
                    )).ToList()
                ))
                .ToListAsync();
        }

        public async Task<Lesson> UpsertAsync(Lesson lesson)
        {
            _db.Lessons.Add(lesson);
            await _db.SaveChangesAsync();
            return lesson;
        }

        public async Task<Lesson> CreateLesson(DateOnly date, string name, ServiceSummary service, int? duration, 
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee,
            bool isTotalPrice, decimal pricePerStudent, string? notes, IReadOnlyList<Guid> studentIds)
        {
            var lesson = new Lesson
            {
                Date = date,
                Name = name,
                IsPricePerHour = service.IsPricePerHour,
                DurationMinutes = duration, 
                PricePerAttendance = (decimal)service.Price,
                IsOnline = isOnline,
                TravelAllowance = travelAllowance,
                IsWeekenOrHoliday = isWeekendOrHoliday,
                WeekendFee = weekendFee,
                Notes = notes
            };

            _db.Lessons.Add(lesson);

            for (int i = 0; i < studentIds.Count; i++)
            {
                lesson.Attendaces.Add(new Attendance
                {
                    LessonId = lesson.Id,
                    StudentId = studentIds[i],
                    IsPaid = false,
                    Price = lesson.GetFinalPricePerStudent()
                });
            }

            await _db.SaveChangesAsync();

            return lesson;
        }

        public async Task<Lesson> UpdateLessonNoteAsync(Guid id, string? note)
        {
            var entity = await _db.Lessons.FirstOrDefaultAsync(i => i.Id == id);
            if (entity is null)
                throw new InvalidDataException("Lesson not found.");
            entity.Notes = note;
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task<Lesson> UpdateLesson(Guid id, DateOnly date, string name, 
            bool isPricePerHour, int? duration, decimal pricePerStudent,
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, string? note)
        {
            var entity = await _db.Lessons.Include(l => l.Attendaces).FirstOrDefaultAsync(i => i.Id == id);

            if (entity is null)
                throw new InvalidDataException("Lesson not found.");

            entity.Date = date;
            entity.Name = name;
            entity.IsPricePerHour = isPricePerHour;
            entity.DurationMinutes = duration;
            entity.PricePerAttendance = pricePerStudent;
            entity.IsOnline = isOnline;
            entity.TravelAllowance = travelAllowance;
            entity.IsWeekenOrHoliday = isWeekendOrHoliday;
            entity.WeekendFee = weekendFee;
            entity.Notes = note;

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
                    IsPaid = false,
                    Price = lesson.GetFinalPricePerStudent()
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
                    (double?)sp.Price,
                    sp.DurationMinutes,
                    sp.IsOnline,
                    sp.IsWeekenOrHoliday))
                .ToListAsync();

        }

    }

}
