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
        public string FullName => Helper.GetFullName(FirstName, LastName);
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
