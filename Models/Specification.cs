namespace Models
{
    public sealed class Specification
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = string.Empty; // human-friendly description

        public Guid StudentId { get; set; }
        public Student Student { get; set; } = null!;

        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;

        public int DurationMinutes { get; set; }
        public bool IsOnline {get; set; }   
    }
}
