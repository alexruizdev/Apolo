namespace Models
{
    public enum DocumentType
    {
        Invoice,
        Ticket
    }

    public sealed class BillingDocument
    {
        public BillingDocument(DateTime createdUTC)
        {
            CreatedUTC = createdUTC;
            Year = createdUTC.Year;
        }
        private BillingDocument() { }

        public Guid Id { get; set; } = Guid.NewGuid();
        public DocumentType Type { get; set; }
        public int SequenceNumber { get; set; }
        public DateTime CreatedUTC { get; private set; }
        public int Year { get; private set; }

        // Computed property for display and UI (e.g., "2024-03-E-0013" or "TCK-2024-0015")
        public string DocumentNumber => Type == DocumentType.Invoice
        ? $"{CreatedUTC:yyyy-MM}-E-{SequenceNumber:D4}"
        : $"TCK-{CreatedUTC:yyyy-MM}-{SequenceNumber:D4}";

        public Guid PayerId { get; set; }
        public Payer Payer { get; set; } = null!;

        public ICollection<Lesson> Lines { get; set; } = new List<Lesson>();
    }
}
