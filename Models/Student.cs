using System.ComponentModel.DataAnnotations;

namespace Models
{

    public sealed record StudentSummary(Guid Id, string FirstName, string LastName,
        Guid PayerId, string PayerName, int? CommuteMinutes)
    {
        public string FullName => $"{FirstName} {LastName}";
    }

    public sealed class Student
    {
        public Guid Id { get; set; }  = Guid.NewGuid();

        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public int CommuteMinutes { get; set; } // commute to service location

        public Guid PayerId { get; set; }
        public Payer Payer { get; set; } = null!;

        public ICollection<Specification> Specifications { get; set; } = new List<Specification>();
        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    }
}
