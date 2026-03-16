using Backend.API.DTOs;
using Backend.API.Models.JournalApi;
using TopAcademyAPI.Journal;
using TopAcademyAPI.Journal.Endpoints;
using TopAcademyAPI.Journal.Endpoints.Auth.Login;
using TopAcademyAPI.Journal.Endpoints.Schedule;

namespace Backend.API.Services
{
    public class JournalScheduleService
    {
        private readonly ILogger<JournalScheduleService> _logger;
        private readonly JournalApi _journalApi;

        public JournalScheduleService(
            ILogger<JournalScheduleService> logger,
            JournalApi journalApi)
        {
            _logger = logger;
            _journalApi = journalApi;
        }

        // Получить расписание на день из API журнала
        public async Task<List<JournalLessonDto>?> GetDayScheduleAsync(
            string login,
            string password,
            DateTime date)
        {
            try
            {
                _logger.LogInformation($"Получение расписания на {date:yyyy-MM-dd}");

                await EnsureAuthorizedAsync(login, password);

                var apiLessons = await _journalApi.GetScheduleByDateAsync(date);

                if (apiLessons != null && apiLessons.Any())
                {
                    _logger.LogInformation($"Получено {apiLessons.Count} занятий");

                    var result = apiLessons
                        .Select(JournalLessonMapper.MapFromApi)
                        .ToList();

                    return result;
                }

                _logger.LogInformation("Нет занятий на этот день");
                return new List<JournalLessonDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения расписания");
                return new List<JournalLessonDto>();
            }
        }

        // Конвертировать занятие из журнала в EventDto для фронтенда
        public EventDto ConvertToEventDto(JournalLessonDto lesson, DateTime date)
        {
            var startTime = ParseTime(lesson.StartedAt, date);
            var endTime = ParseTime(lesson.FinishedAt, date);
            var now = DateTime.Now;
            var isCurrent = now >= startTime && now <= endTime;

            return new EventDto
            {
                Id = lesson.LessonNumber,
                Type = "lecture",
                Category = "study",
                Name = lesson.SubjectName,
                Teacher = lesson.TeacherName,
                Room = lesson.RoomName,
                Group = lesson.Group,
                Tags = new List<string> { "Занятие" },
                IsCurrent = isCurrent,
                TimeRemaining = isCurrent ? GetTimeRemaining(endTime) : null,
                StartTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                EndTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }

        private async Task EnsureAuthorizedAsync(string login, string password)
        {
            if (string.IsNullOrEmpty(_journalApi.AccessToken))
            {
                var loginRequest = new LoginRequest(login, password);
                await _journalApi.LoginAsync(loginRequest);
            }
        }

        private DateTime ParseTime(string timeString, DateTime date)
        {
            var parts = timeString.Split(':');
            return date.Date.AddHours(int.Parse(parts[0]))
                         .AddMinutes(int.Parse(parts[1]));
        }

        private string? GetTimeRemaining(DateTime endTime)
        {
            var remaining = endTime - DateTime.Now;
            if (remaining.TotalMinutes <= 0) return null;

            if (remaining.TotalHours >= 1)
                return $"до конца {Math.Ceiling(remaining.TotalHours)} ч {remaining.Minutes} мин";
            else
                return $"до конца {remaining.Minutes} мин";
        }
    }
}