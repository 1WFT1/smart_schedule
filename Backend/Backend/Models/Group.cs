using System.ComponentModel.DataAnnotations;

namespace Backend.API.Models
{
    public class Group
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "9/4-РПО-22/2-39"

        [MaxLength(100)]
        public string? DisplayName { get; set; } // Для отображения

        public int? StudentCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastActive { get; set; }

        [MaxLength(20)]
        public string Source { get; set; } = "manual"; // "manual" или "journal"

        public bool IsActive { get; set; } = true;

        // КУРАТОР ГРУППЫ
        public int? CuratorId { get; set; }
        public User? Curator { get; set; }

        // СТУДЕНТЫ ГРУППЫ
        public ICollection<User>? Students { get; set; }
    }
}