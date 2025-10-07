using System.ComponentModel.DataAnnotations;

namespace Models
{
    public sealed class ServiceType
    {
        public int Id { get; set; } // simple int key
        [Required, MaxLength(120)]
        public string Name { get; set; } = string.Empty; // unique

        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}
