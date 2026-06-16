using System.Globalization;

namespace Models
{
    public sealed record PayerSummary(Guid Id, 
        string FirstName, 
        string LastName, 
        decimal Outstanding,
        string? Address,
        string? Zip, 
        string? City, 
        string? TaxId) : ISummary
    {
        public string Name => Helper.GetFullName(FirstName, LastName);
    }

    public class PayerActivityInfo
    {
        public Guid PayerId { get; set; }
        public string PayerName { get; set; } = string.Empty;
        public DateOnly? LastLessonDate { get; set; }

        // Helper to show a friendly string in the UI
        public string Display => PayerName + (LastLessonDate.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $" - Last activity: {LastLessonDate.Value:dd/MM/yyyy}")
            : " - No recorded activity");
    }

    public sealed record PayerOption(Guid Id, string FullName);

    public sealed class Payer
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public string FullName
        {
            get => Helper.GetFullName(FirstName, LastName);
        }

        public override string ToString() => FullName;

        public string? Address { get; set; }
        public string? ZipCode { get; set; }
        public string? City { get; set; }
        public string? TaxId { get; set; }

        public ICollection<Student> Students { get; set; } = new List<Student>();
    }
}
