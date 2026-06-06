namespace Models
{
    public sealed record SpecificationOption(
        Guid Id,
        string Display,
        Guid ServiceId,
        double? Price,
        int DurationMinutes,
        bool IsOnline,
        bool IsWeekend);
    public sealed record SpecificationSummary(
        Guid Id,
        string SpecificationName,
        Guid StudentId,
        string StudentName,
        Guid ServiceId,
        string ServiceName,
        int DurationMinutes,
        double? Price,
        bool IsOnline,
        bool IsWeekendOrHoliday,
        int UsageCount
        );
    public sealed class Specification
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty; // human-friendly description

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;

        public int DurationMinutes { get; set; } // TODO: null
        public decimal? Price { get; set; } // when null, we use service price
        public bool IsOnline { get; set; }
        public bool IsWeekendOrHoliday { get; set; }
        public int UsageCount { get; set; } = 0; // Track frequency manually
    }
}
