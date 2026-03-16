using Backend.API.Models.JournalApi;
using System.Text.Json.Serialization;

namespace Backend.API.DTOs
{
    //DTO для отправки данных на фронтенд
    public class EventDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;        // lecture, practice, extra, activity
        public string Category { get; set; } = string.Empty;    // study, extra
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public string? Teacher { get; set; }
        public string? Room { get; set; }
        public string? Group { get; set; }
        public bool IsCurrent { get; set; }
        public string? TimeRemaining { get; set; }              // "до конца 25 мин"
        public string? StartTime { get; set; }                  // ISO строка
        public string? EndTime { get; set; }                    // ISO строка
    }


    // DTO для создания нового события (с фронтенда)
    public class CreateEventDto
    {
        public string Type { get; set; } = "extra";              // extra, activity, lecture, practice
        public string Category { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Teacher { get; set; }
        public string? Room { get; set; }
        public string? Group { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<string> Tags { get; set; } = new();
        public List<string>? TargetGroups { get; set; }
    }
}
