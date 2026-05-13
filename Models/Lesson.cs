using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public sealed record LessonSummary(
        Guid Id,
        string Name,
        DateOnly Date,
        bool IsPricePerHour,
        int? DurationMinutes,
        decimal PricePerAttendance,
        bool IsOnline,
        decimal TravelAllowance,
        bool IsWeekenOrHoliday,
        decimal WeekendFee,
        string? Notes,
        IReadOnlyList<AttendanceSummary> Attendances)
    {
        public decimal GrandTotal => Lesson.GetPrice(Attendances.Count(), IsPricePerHour, DurationMinutes, PricePerAttendance, IsOnline, TravelAllowance, IsWeekenOrHoliday, WeekendFee);
        public string ShortNote => Lesson.Truncate(Notes, 70);
    }
    public sealed class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateOnly Date { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public bool IsPricePerHour { get; set; } // When true, pricePerHour is the total price for the lesson, regardless of duration
        public int? DurationMinutes { get; set; }
        [Precision(18, 2)]
        public decimal PricePerAttendance { get; set; }
        public bool IsOnline { get; set; }
        public decimal TravelAllowance { get; set; }
        public bool IsWeekenOrHoliday{ get; set; }
        public decimal WeekendFee { get; set; }

        public string? Notes { get; set; }
        
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

        public decimal GetFinalPricePerStudent() => GetPrice(1, IsPricePerHour, DurationMinutes, PricePerAttendance, IsOnline, TravelAllowance, IsWeekenOrHoliday, WeekendFee);

        public static decimal GetPrice(int attendants, bool isPricePerHour, int? duration, decimal price, bool isOnline, decimal travelAllowance, bool isWeekenOrHoliday, decimal weekendFee)
        {
            if (attendants <= 0)
                throw new ArgumentException("Attendants must be greater than zero.");
            decimal travel = isOnline ? 0 : travelAllowance;
            price = isWeekenOrHoliday ? weekendFee + price : price;
            decimal pricePerStudent = price;
            if (isPricePerHour)
            {
                if (duration is null) 
                    throw new ArgumentException("Duration is required when price is per hour.");
                pricePerStudent = Math.Round(price * (duration.Value / 60m), 2, MidpointRounding.AwayFromZero);
            }
            return travel + (pricePerStudent * attendants);
        }

        public static string Truncate(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            return input.Length <= maxLength
                ? input
                : $"{input[..maxLength]}...";
        }

        public List<AttendanceSummary> AttendancesSummary(ICollection<StudentOption> students)
        {
            return Attendances.Select(a =>
            {
                var student = students.First(s => s.Id == a.StudentId);

                return new AttendanceSummary(
                    a.Id,
                    a.StudentId,
                    student.FullName,
                    a.IsPaid
                );
            })
            .OrderBy(summary => summary.StudentName)
            .ToList();
        }
    }
}
