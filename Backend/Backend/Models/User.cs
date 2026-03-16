using System.ComponentModel.DataAnnotations;

namespace Backend.API.Models
{
    // Модель пользователя в системе
    // Для студентов - только данные из журнала
    // Для админов - создаются вручную
    public class User
    {
        public int Id { get; set; }

        // Для студентов
        [MaxLength(100)]
        public string? JournalLogin { get; set; }

        public string? EncryptedJournalPassword { get; set; }

        // Для админов (используем общие поля)
        [MaxLength(100)]
        public string? Username { get; set; }  // Вместо AdminUsername

        public string? PasswordHash { get; set; }  // Вместо AdminPasswordHash

        [MaxLength(200)]
        public string? FullName { get; set; }

        [MaxLength(50)]
        public string? Group { get; set; }

        public UserRole Role { get; set; } = UserRole.student;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }

    public enum UserRole
    {
        student,
        teacher,
        admin
    }
}
