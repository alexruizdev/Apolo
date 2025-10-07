using Microsoft.EntityFrameworkCore;

namespace Models
{
    public sealed class Service
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public int ServiceTypeId { get; set; }
        public ServiceType ServiceType { get; set; } = null!;

        [Precision(18, 2)]
        public decimal PricePerHour { get; set; }

        public ICollection<Specification> Specifications { get; set; } = new List<Specification>();
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
