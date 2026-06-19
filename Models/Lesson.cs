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

    public sealed record LessonSummary (
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
        bool IsWeekendOrHoliday,
        decimal WeekendFee,
        decimal Tip,
        string? Notes) : ISummary
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
        public bool IsWeekendOrHoliday{ get; private set; }
        public decimal WeekendFee { get; private set; }
        public decimal Tip { get; set; }
        public string? Notes { get; set; }

        public Lesson(DateOnly date, string name, bool isPaid, Guid studentId, Guid? billingDocumentId, 
            bool isPricePerHour, int? durationMinutes, decimal basePrice, 
            bool isOnline, decimal travelAllowance, bool isWeekendOrHoliday, decimal weekendFee, decimal tip, string? notes)
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
            IsWeekendOrHoliday = isWeekendOrHoliday;
            WeekendFee = weekendFee;
            Tip = tip;
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
            IsWeekendOrHoliday = weekend;
            WeekendFee = fee;
            FinalPrice = GetPrice();
            return true;
        }

        private decimal GetPrice() => GetPrice(IsOnline, TravelAllowance, IsWeekendOrHoliday, WeekendFee,
            BasePrice, IsPricePerHour, DurationMinutes);

        public static decimal GetPrice(bool isOnline, decimal travelAllowance,
            bool isWeekend, decimal weekendFee, 
            decimal basePrice, bool isPricePerHour, int? duration)
        {
            decimal travel = isOnline ? 0 : travelAllowance;
            decimal price = isWeekend ? weekendFee + basePrice : basePrice;
            if (isPricePerHour)
            {
                if (duration is null)
                    throw new ArgumentException("Duration is required when price is per hour.");
                if (duration <= 0)
                    throw new ArgumentException("Duration must be a positive value.");
                price *= duration.Value / 60m;
            }
            decimal total = travel + price;
            return Math.Round(total * 2m, MidpointRounding.AwayFromZero) / 2m;
        }

        public static string GetRate(bool isPricePerHour, int duration)
            => isPricePerHour ? $"{Math.Round(duration / 60m, 2)}" : "1";  

        public static string Truncate(string? input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            return input.Length <= maxLength
                ? input
                : $"{input[..maxLength]}...";
        }
    }
}
