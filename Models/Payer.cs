using System.ComponentModel.DataAnnotations;

namespace Models
{
    public sealed record PayerSummary(Guid Id, string FirstName, string LastName, decimal Outstanding)
    {
        public string FullName => $"{FirstName} {LastName}";
    }
    public sealed class Payer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [MaxLength(100)]
        public string FirstName { get; set; } = string.Empty;
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        public string FullName
        {
            get => $"{FirstName} {LastName}";
        }

        public override string ToString() => FullName;

        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
