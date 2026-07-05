using System.Reflection.Metadata;
using System.Xml.Linq;

namespace Models
{
    public enum DocumentType
    {
        Invoice,
        Ticket
    }

    public sealed record BillSummary(Guid? Id, Guid PayerId, DocumentType Type, int SequenceNumber, string Name, DateTime CreatedUTC)
    { 
        public string Date => CreatedUTC.ToString("dd/MM/yyyy");
    };

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

        public void EditDate(DateTime newDate)
        {
            CreatedUTC = newDate;
            Year = newDate.Year;
        }

        // Computed property for display and UI (e.g., "03-2024-E-0013" or "TCK-03-2024-0015")
        public string DocumentNumber => Type == DocumentType.Invoice
        ? $"{CreatedUTC:MM-yyyy}-E-{SequenceNumber:D4}"
        : $"TCK-{CreatedUTC:MM-yyyy}-{SequenceNumber:D4}";
    
        public Guid PayerId { get; set; }
        public Payer Payer { get; set; } = null!;

        public ICollection<Lesson> Lines { get; set; } = [];
    }
}
