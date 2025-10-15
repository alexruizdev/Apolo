namespace Models
{
    public sealed record InvoiceAttendanceSummary(
        Guid AttendanceId,
        Guid LessonId,
        DateOnly Date,
        string LessonName,
        Guid StudentId,
        string StudentName,
        decimal Price);
    public sealed class InvoiceAttendance
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int InvoiceId { get; set; }
        public Invoice Invoice { get; set; } = null!;

        public Guid AttendanceId { get; set; }
        public Attendance Attendance { get; set; } = null!;

    }
    public sealed class Invoice
    {
        public int Id { get; set; }
        public string Name {get; set; } = string.Empty;
        public DateTime CreatedUTC { get; set; }
        public Guid PayerId { get; set; }
        public Payer Payer { get; set; } = null!;

        public ICollection<InvoiceAttendance> Lines { get; set; } = new List<InvoiceAttendance>();
    }
}
