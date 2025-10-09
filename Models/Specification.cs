namespace Models
{
    public sealed record SpecificationOption(
        Guid Id,
        string Display,
        Guid ServiceId,
        string ServiceName,
        decimal PricePerHour,
        int DurationMinutes,
        bool IsOnline);
    public sealed record SpecificationSummary(
        Guid Id,
        string SpecificationName,
        Guid studentId,
        string StudentName,
        Guid ServiceId,
        string ServiceName,
        int DurationMinutes,
        bool IsOnline
        );
    public sealed class Specification
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty; // human-friendly description

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;

        public int DurationMinutes { get; set; }
        public bool IsOnline { get; set; }
    }
}
