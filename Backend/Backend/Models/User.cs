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

        public string? AccessToken { get; set; }      // Текущий токен сессии
        public string? RefreshToken { get; set; }     // Для обновления
        public DateTime? TokenExpiresAt { get; set; } // Когда истекает

        // Для админов (используем общие поля)
        [MaxLength(100)]
        public string? Username { get; set; }  // Вместо AdminUsername

        public string? PasswordHash { get; set; }  // Вместо AdminPasswordHash

        [MaxLength(200)]
        public string? FullName { get; set; }

        [MaxLength(50)]
        public string? Group { get; set; }  // Для студентов

        public UserRole Role { get; set; } = UserRole.student;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }

        // Группы, которыми управляет куратор/админ
        public ICollection<Group>? CuratedGroups { get; set; }

        // Группа студента (связь с Group)
        public int? StudentGroupId { get; set; }
        public Group? StudentGroup { get; set; }

        // Telegram данные прямо здесь
        public long? TelegramId { get; set; }
        [MaxLength(100)]
        public string? TelegramUsername { get; set; }
        public bool IsTelegramLinked { get; set; }

        public bool NotificationsEnabled { get; set; } = false;
        public int NotificationMinutesBefore { get; set; } = 15;
    }

    public enum UserRole
    {
        student,    // Студент
        teacher,    // Куратор/Преподаватель
        admin       // Учебная часть
    }
}