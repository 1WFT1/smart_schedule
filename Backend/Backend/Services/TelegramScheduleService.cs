using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;
using TopAcademyAPI.Journal;
using TopAcademyAPI.Journal.Endpoints.Schedule;

namespace Backend.API.Services
{
    public class TelegramScheduleService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly JournalApi _journalApi;
        private readonly ILogger<TelegramScheduleService> _logger;

        public TelegramScheduleService(
            ApplicationDbContext dbContext,
            JournalApi journalApi,
            ILogger<TelegramScheduleService> logger)
        {
            _dbContext = dbContext;
            _journalApi = journalApi;
            _logger = logger;
        }

        public async Task<string> GetTodayScheduleForUser(long telegramId)
        {
            try
            {
                // Ищем пользователя по Telegram ID
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.TelegramId == telegramId);

                if (user == null || string.IsNullOrEmpty(user.JournalLogin))
                {
                    return "❌ Ваш аккаунт не привязан к журналу. Используйте /link для привязки.";
                }

                // Проверяем токен
                if (user.TokenExpiresAt < DateTime.UtcNow.AddMinutes(5))
                {
                    return "⏰ Сессия истекла. Используйте /link для обновления связи с аккаунтом.";
                }

                _journalApi.AccessToken = user.AccessToken;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                var lessons = await _journalApi.GetScheduleByDateAsync(DateTime.Today);

                if (lessons == null || !lessons.Any())
                {
                    return "📭 На сегодня пар нет. Отдыхайте!";
                }

                // Формируем красивое сообщение
                var message = $"📅 Расписание на {DateTime.Today:dd.MM.yyyy}\n\n";
                var lessonNumber = 1;

                foreach (var lesson in lessons.OrderBy(l => TimeSpan.Parse(l.StartedAt)))
                {
                    message += $"<b>{lessonNumber}.</b> {lesson.StartedAt} – {lesson.FinishedAt}\n";
                    message += $"📚 <b>{lesson.SubjectName}</b>\n";
                    message += $"👨‍🏫 {lesson.TeacherName}\n";
                    message += $"📍 {lesson.RoomName ?? "ауд. не указана"}\n\n";
                    lessonNumber++;
                }

                // Добавляем информацию о следующей паре
                var now = DateTime.Now.TimeOfDay;
                var nextLesson = lessons
                    .Select(l => new { Lesson = l, Start = TimeSpan.Parse(l.StartedAt) })
                    .FirstOrDefault(l => l.Start > now);

                if (nextLesson != null)
                {
                    var timeUntil = nextLesson.Start - now;
                    message += $"⏳ Следующая пара через {timeUntil.Hours}ч {timeUntil.Minutes}м";
                }

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка получения расписания для Telegram ID {telegramId}");
                return "❌ Произошла ошибка при получении расписания. Попробуйте позже.";
            }
        }

        public async Task<string> GetWeekScheduleForUser(long telegramId)
        {
            try
            {
                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.TelegramId == telegramId);

                if (user == null || string.IsNullOrEmpty(user.JournalLogin))
                {
                    return "❌ Аккаунт не привязан к журналу.";
                }

                _journalApi.AccessToken = user.AccessToken;

                var startDate = DateTime.Today;
                var endDate = startDate.AddDays(6);

                var message = $"📅 Расписание на неделю {startDate:dd.MM} – {endDate:dd.MM}\n\n";

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var lessons = await _journalApi.GetScheduleByDateAsync(date);

                    message += $"<b>{date:dd.MM.yyyy} ({GetDayName(date.DayOfWeek)})</b>\n";

                    if (lessons != null && lessons.Any())
                    {
                        foreach (var lesson in lessons)
                        {
                            message += $"  {lesson.StartedAt} – {lesson.SubjectName}\n";
                        }
                    }
                    else
                    {
                        message += $"  🟢 Нет пар\n";
                    }
                    message += "\n";
                }

                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения недельного расписания");
                return "❌ Ошибка при получении расписания.";
            }
        }

        private string GetDayName(DayOfWeek day)
        {
            var days = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "ПН" },
                { DayOfWeek.Tuesday, "ВТ" },
                { DayOfWeek.Wednesday, "СР" },
                { DayOfWeek.Thursday, "ЧТ" },
                { DayOfWeek.Friday, "ПТ" },
                { DayOfWeek.Saturday, "СБ" },
                { DayOfWeek.Sunday, "ВС" }
            };
            return days[day];
        }
    }
}