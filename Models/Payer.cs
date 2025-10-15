using System.ComponentModel.DataAnnotations;

namespace Models
{
    public sealed record PayerSummary(Guid Id, 
        string FirstName, 
        string LastName, 
        decimal Outstanding,
        string? Address,
        string? Zip, 
        string? City, 
        string? TaxId)
    {
        public string FullName => $"{FirstName} {LastName}";
    }

    public sealed record PayerOption(Guid Id, string FullName);

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

        [MaxLength(200)] public string? Address { get; set; }
        [MaxLength(5)] public string? ZipCode { get; set; }
        [MaxLength(100)] public string? City { get; set; }
        [MaxLength(100)] public string? TaxId { get; set; }

        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
