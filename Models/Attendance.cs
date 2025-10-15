using Microsoft.EntityFrameworkCore;

namespace Models
{
    public sealed record AttendanceSummary(Guid Id, Guid StudentId, string StudentName, bool IsPaid);

    public sealed class Attendance
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid LessonId { get; set; }
        public Lesson Lesson { get; set; } = null!;

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        [Precision(18, 2)]
        public bool IsPaid { get; set; }
    }
}
