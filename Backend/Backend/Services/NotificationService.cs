using Backend.API.Data;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using TopAcademyAPI.Journal;
using TopAcademyAPI.Journal.Endpoints.Schedule;

namespace Backend.API.Services
{
    public class NotificationService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<NotificationService> _logger;
        private readonly ITelegramBotClient _botClient;

        public NotificationService(
            IServiceProvider services,
            ILogger<NotificationService> logger,
            ITelegramBotClient botClient)
        {
            _services = services;
            _logger = logger;
            _botClient = botClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Service запущен");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendNotifications();

                    // Проверяем каждые 30 секунд
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в Notification Service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task CheckAndSendNotifications()
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var journalApi = scope.ServiceProvider.GetRequiredService<JournalApi>();

            // Находим всех пользователей с включенными уведомлениями
            var users = await dbContext.Users
                .Where(u => u.NotificationsEnabled &&
                           u.TelegramId != null &&
                           u.AccessToken != null)
                .ToListAsync();

            var now = DateTime.Now;

            foreach (var user in users)
            {
                try
                {
                    journalApi.AccessToken = user.AccessToken;
                    var lessons = await journalApi.GetScheduleByDateAsync(now.Date);

                    if (lessons == null || !lessons.Any())
                        continue;

                    // Ищем ближайшую пару
                    foreach (var lesson in lessons.OrderBy(l => TimeSpan.Parse(l.StartedAt)))
                    {
                        var lessonStart = now.Date.Add(TimeSpan.Parse(lesson.StartedAt));
                        var minutesUntil = (lessonStart - now).TotalMinutes;

                        // Если до пары осталось нужное количество минут
                        if (minutesUntil > 0 &&
                            minutesUntil <= user.NotificationMinutesBefore &&
                            minutesUntil > user.NotificationMinutesBefore - 1)
                        {
                            await SendNotification(user, lesson);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Ошибка при проверке уведомлений для пользователя {user.Id}");
                }
            }
        }

        private async Task SendNotification(User user, JournalApiLessonDto lesson)
        {
            var message = $"🔔 <b>Скоро пара!</b>\n\n" +
                         $"📚 <b>{lesson.SubjectName}</b>\n" +
                         $"👨‍🏫 {lesson.TeacherName}\n" +
                         $"📍 {lesson.RoomName ?? "ауд. не указана"}\n" +
                         $"⏰ Начало в {lesson.StartedAt}\n\n" +
                         $"Осталось {user.NotificationMinutesBefore} минут";

            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: user.TelegramId,
                    text: message,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);

                _logger.LogInformation($"Уведомление отправлено пользователю {user.TelegramUsername}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка отправки уведомления пользователю {user.TelegramId}");
            }
        }
    }
}