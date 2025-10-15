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
        bool IsTotalPrice,
        decimal PricePerHour,
        IReadOnlyList<AttendanceSummary> Attendances)
    {
        public decimal GrandTotal => Lesson.GetPrice(PricePerHour, DurationMinutes, Attendances.Count(), IsTotalPrice, IsOnline);
    }
    public sealed class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateOnly Date { get; set; }

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }
        public bool IsOnline { get; set; }
        public bool IsTotalPrice { get; set; }


        [Precision(18, 2)]
        public decimal PricePerStudent { get; set; }
        
        public ICollection<Attendance> Attendaces { get; set; } = new List<Attendance>();

        public decimal GetFinalPricePerStudent() => GetPrice(PricePerStudent, DurationMinutes, 1, IsTotalPrice, IsOnline);

        public static decimal GetPrice(decimal pricePerHour, int durationMinutes, int attendants, bool isTotalPrice, bool isOnline)
            => (isOnline ? 0 : 5) + (isTotalPrice ? pricePerHour : Math.Round(pricePerHour * (durationMinutes / 60m), 2, MidpointRounding.AwayFromZero)) * attendants;
    }
}
