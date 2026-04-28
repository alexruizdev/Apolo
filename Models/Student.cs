using System.ComponentModel.DataAnnotations;

namespace Models
{
    public sealed record StudentOption(Guid Id, string FullName);

    public sealed record StudentSummary(Guid Id, string FirstName, string LastName,
        Guid PayerId, string PayerName)
    {
        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    public sealed class Student
    {
        public Guid Id { get; set; }  = Guid.NewGuid();

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}".Trim();

        public Guid PayerId { get; set; }
        public Payer Payer { get; set; } = null!;

        public ICollection<Specification> Specifications { get; set; } = new List<Specification>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
