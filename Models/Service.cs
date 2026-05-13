using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public sealed record ServiceSummary (Guid Id, string Name, bool IsPricePerHour, double Price);
    public sealed class Service
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public string Name { get; set; } = string.Empty;
        public bool IsPricePerHour { get; set; } // When true, pricePerHour is the total price for the service, regardless of duration

        [Precision(18, 2)]
        public decimal Price { get; set; }

        public ICollection<Specification> Specifications { get; set; } = new List<Specification>();
    }
}
