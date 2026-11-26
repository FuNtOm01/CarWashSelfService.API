using System.ComponentModel.DataAnnotations;

namespace CarWashSelfService.API.Entities
{
    public class Tariff
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }   // длительность, включённая в тариф
        public bool IsActive { get; set; } = true;

        public ICollection<WashSession> WashSessions { get; set; } = new List<WashSession>();
    }
}
