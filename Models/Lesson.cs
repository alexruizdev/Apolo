using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public sealed record LessonLine(
        Guid Id,
        Guid StudentId,
        DateOnly Date,
        string Name,
        string StudentName,
        decimal FinalPrice,
        bool IsPaid)
    { }

    public sealed record LessonSummary(
        Guid Id,
        DateOnly Date,
        string Name,
        decimal FinalPrice,
        bool IsPaid,
        Guid StudentId,
        string StudentName,
        Guid? BillingDocumentId,
        string BillingName,
        bool IsPricePerHour,
        int? DurationMinutes,
        decimal BasePrice,
        bool IsOnline,
        decimal TravelAllowance,
        bool IsWeekenOrHoliday,
        decimal WeekendFee,
        string? Notes)
    {
        public string ShortNote => Lesson.Truncate(Notes, 70);
    }
    public sealed class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateOnly Date { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public decimal FinalPrice { get; private set; }
        public bool IsPaid { get; set; }
        [Precision(18, 2)]
        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;
        public Guid? BillingDocumentId { get; set; }
        public BillingDocument? BillingDocument { get; set; }
        public bool IsPricePerHour { get; private set; } // When true, pricePerHour is the total price for the lesson, regardless of duration
        public int? DurationMinutes { get; private set; }
        [Precision(18, 2)]
        public decimal BasePrice { get; private set; }
        public bool IsOnline { get; private set; }
        public decimal TravelAllowance { get; private set; }
        public bool IsWeekenOrHoliday{ get; private set; }
        public decimal WeekendFee { get; private set; }
        public string? Notes { get; set; }

        public Lesson(DateOnly date, string name, bool isPaid, Guid studentId, Guid? billingDocumentId, 
            bool isPricePerHour, int? durationMinutes, decimal basePrice, 
            bool isOnline, decimal travelAllowance, bool isWeekenOrHoliday, decimal weekendFee, string? notes)
        {
            Date = date;
            Name = name;
            IsPaid = isPaid;
            StudentId = studentId;
            BillingDocumentId = billingDocumentId;
            IsPricePerHour = isPricePerHour;
            DurationMinutes = durationMinutes;
            BasePrice = basePrice;
            IsOnline = isOnline;
            TravelAllowance = travelAllowance;
            IsWeekenOrHoliday = isWeekenOrHoliday;
            WeekendFee = weekendFee;
            Notes = notes;
            FinalPrice = GetPrice();
        }

        // We allow to update lesson core information when is not paid
        public bool Set(bool isPricePerHour, int? duration, decimal price, bool online, decimal travel, bool weekend, decimal fee)
        {
            if (IsPaid || BillingDocumentId is not null)
                return false;
            IsPricePerHour = isPricePerHour;
            DurationMinutes = duration;
            BasePrice = price;
            IsOnline = online;
            TravelAllowance = travel;
            IsWeekenOrHoliday = weekend;
            WeekendFee = fee;
            FinalPrice = GetPrice();
            return true;
        }

        private decimal GetPrice()
        {
            decimal travel = IsOnline ? 0 : TravelAllowance;
            decimal price = IsWeekenOrHoliday ? WeekendFee + BasePrice : BasePrice;
            if (IsPricePerHour)
            {
                if (DurationMinutes is null) 
                    throw new ArgumentException("Duration is required when price is per hour.");
                price = price * (DurationMinutes.Value / 60m);
            }
            decimal total = travel + price;
            return Math.Round(total * 2m, MidpointRounding.AwayFromZero) / 2m;
        }

        public static string Truncate(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            return input.Length <= maxLength
                ? input
                : $"{input[..maxLength]}...";
        }
    }
}
