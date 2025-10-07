using Microsoft.EntityFrameworkCore;

namespace Models
{
    public sealed class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateOnly Date { get; set; } // day/month/year

        public int DurationMinutes { get; set; }
        public bool IsOnline { get; set; }

        public Guid ServiceId { get; set; }
        public Service Service { get; set; } = null!;

        public Guid? SpecificationId { get; set; }
        public Specification? Specification { get; set; }

        [Precision(18, 2)]
        public decimal GrandTotal { get; set; }
        
        public ICollection<Attendance> Attendaces { get; set; } = new List<Attendance>();   
    }
}
