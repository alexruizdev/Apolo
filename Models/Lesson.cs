using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public sealed record LessonSummary(
        Guid Id,
        string Name,
        DateOnly Date,
        int DurationMinutes,
        bool IsOnline,
        decimal PricePerStudent,
        IReadOnlyList<AttendanceSummary> Attendances)
    {
        public decimal GrandTotal => PricePerStudent * Attendances.Count();
    }
    public sealed class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateOnly Date { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }
        public bool IsOnline { get; set; }


        [Precision(18, 2)]
        public decimal PricePerStudent { get; set; }
        
        public ICollection<Attendance> Attendaces { get; set; } = new List<Attendance>();   
    }
}
