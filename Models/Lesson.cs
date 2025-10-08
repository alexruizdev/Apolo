using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public sealed class Lesson
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateOnly Date { get; set; } // day/month/year

        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }
        public bool IsOnline { get; set; }


        [Precision(18, 2)]
        public decimal GrandTotal { get; set; }
        
        public ICollection<Attendance> Attendaces { get; set; } = new List<Attendance>();   
    }
}
