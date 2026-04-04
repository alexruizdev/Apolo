using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public sealed record ServiceSummary (Guid Id, string Name, decimal PricePerHour);
    public sealed class Service
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;

        [Precision(18, 2)]
        public decimal PricePerHour { get; set; }

        public ICollection<Specification> Specifications { get; set; } = new List<Specification>();
    }
}
