using System.ComponentModel.DataAnnotations;

namespace Backend.API.Models
{
    //Это МОДЕЛЬ БАЗЫ ДАННЫХ(Entity)
    // Используется Entity Framework для создания таблиц
    public class Event
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "extra";  // extra, activity, lecture, practice

        [Required]
        [MaxLength(20)]
        public string Category { get; set; } = string.Empty;  // study, extra

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Teacher { get; set; }

        [MaxLength(50)]
        public string? Room { get; set; }

        [MaxLength(50)]
        public string? Group { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public List<string> Tags { get; set; } = new();

        public List<string>? TargetGroups { get; set; }

        public int? CreatedByUserId { get; set; }

        public User? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }


    }
}
