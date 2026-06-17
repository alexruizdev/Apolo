namespace Models
{
    public sealed record StudentOption(Guid Id, string FullName);

    public sealed record StudentSummary(Guid Id, string FirstName, string LastName,
        Guid PayerId, string PayerName) : ISummary
    {
        public string Name => Helper.GetFullName(FirstName, LastName);
    }

    public sealed class Student
    {
        public Guid Id { get; set; }  = Guid.NewGuid();

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string FullName => Helper.GetFullName(FirstName, LastName);

        public Guid PayerId { get; set; }
        public Payer Payer { get; set; } = null!;

        public ICollection<Specification> Specifications { get; set; } = [];
        public ICollection<Lesson> Lessons { get; set; } = [];

        
    }
}
