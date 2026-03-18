using Backend.API.Data;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TopAcademyAPI.Journal;
using TopAcademyAPI.Journal.Endpoints.Settings.UserInfo;
using TopAcademyAPI.Journal.Endpoints.Schedule;

namespace Backend.API.Services
{
    public class TelegramBotService : IHostedService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IServiceProvider _services;
        private readonly string _webAppUrl;
        private CancellationTokenSource _cts;

        public TelegramBotService(
            IOptions<TelegramBotConfiguration> config,
            ILogger<TelegramBotService> logger,
            IServiceProvider services,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _botClient = new TelegramBotClient(config.Value.BotToken);
            _webAppUrl = config.Value.WebAppUrl ?? "https://web.telegram.org/k/";
            _services = services;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                var me = await _botClient.GetMeAsync();
                _logger.LogInformation($"Бот @{me.Username} успешно запущен!");

                _botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    pollingErrorHandler: HandleErrorAsync,
                    receiverOptions: new ReceiverOptions
                    {
                        AllowedUpdates = new[]
                        {
                            UpdateType.Message,
                            UpdateType.CallbackQuery
                        },
                        ThrowPendingUpdates = true,
                    },
                    cancellationToken: _cts.Token
                );

                _logger.LogInformation("Бот начал получать обновления...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запуске бота");
                throw;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            _logger.LogInformation("Бот остановлен");
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(
            ITelegramBotClient botClient,
            Update update,
            CancellationToken cancellationToken)
        {
            try
            {
                // Обработка callback от кнопок
                if (update.CallbackQuery != null)
                {
                    await HandleCallbackQuery(botClient, update.CallbackQuery, cancellationToken);
                    return;
                }

                // Обработка текстовых сообщений
                if (update.Message is not { } message)
                    return;

                if (message.Text is not { } messageText)
                    return;

                var chatId = message.Chat.Id;
                var username = message.From?.Username ?? "пользователь";

                _logger.LogInformation($"Получено сообщение от {username}: {messageText}");

                if (messageText.StartsWith("/start"))
                {
                    await HandleStartCommand(botClient, chatId, cancellationToken);
                }
                else if (messageText.StartsWith("/link"))
                {
                    await HandleLinkCommand(botClient, chatId, messageText, cancellationToken);
                }
                else if (messageText.StartsWith("/today"))
                {
                    await HandleTodayCommand(botClient, chatId, cancellationToken);
                }
                else if (messageText.StartsWith("/week"))
                {
                    await HandleWeekCommand(botClient, chatId, cancellationToken);
                }
                else if (messageText.StartsWith("/help"))
                {
                    await HandleHelpCommand(botClient, chatId, cancellationToken);
                }
                else
                {
                    await HandleTextMessage(botClient, chatId, messageText, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке обновления");
            }
        }

        private async Task HandleCallbackQuery(
            ITelegramBotClient botClient,
            CallbackQuery callbackQuery,
            CancellationToken cancellationToken)
        {
            try
            {
                var chatId = callbackQuery.Message.Chat.Id;
                var data = callbackQuery.Data;
                var messageId = callbackQuery.Message.MessageId;

                // Убираем "часики" на кнопке
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    cancellationToken: cancellationToken);

                // Удаляем клавиатуру, чтобы избежать повторных нажатий
                await botClient.EditMessageReplyMarkupAsync(
                    chatId: chatId,
                    messageId: messageId,
                    replyMarkup: null,
                    cancellationToken: cancellationToken);

                // Обрабатываем команду
                switch (data)
                {
                    case "today":
                        await HandleTodayCommand(botClient, chatId, cancellationToken);
                        break;
                    case "week":
                        await HandleWeekCommand(botClient, chatId, cancellationToken);
                        break;
                    case "help":
                        await HandleHelpCommand(botClient, chatId, cancellationToken);
                        break;
                    case "start":
                        await HandleStartCommand(botClient, chatId, cancellationToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке callback");
            }
        }

        private async Task HandleStartCommand(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📅 Сегодня", "today"),
                    InlineKeyboardButton.WithCallbackData("📆 Неделя", "week"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("❓ Помощь", "help"),
                },
                new[]
                {
                    InlineKeyboardButton.WithWebApp(
                        "🌐 Открыть веб-версию",
                        new WebAppInfo { Url = _webAppUrl }
                    )
                }
            });

            var welcomeMessage =
                "👋 Привет! Я бот для расписания колледжа.\n\n" +
                "📌 <b>Доступные команды:</b>\n" +
                "/start - Показать это меню\n" +
                "/today - Расписание на сегодня\n" +
                "/week - Расписание на неделю\n" +
                "/link логин пароль - Привязать аккаунт\n" +
                "/help - Помощь\n\n" +
                "Для начала работы используйте /link логин пароль";

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: welcomeMessage,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task HandleLinkCommand(
            ITelegramBotClient botClient,
            long chatId,
            string messageText,
            CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();
            var journalApi = scope.ServiceProvider.GetRequiredService<JournalApi>();

            var parts = messageText.Split(' ');
            if (parts.Length < 3)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Неправильный формат. Используйте: /link логин пароль",
                    cancellationToken: cancellationToken);
                return;
            }

            var login = parts[1];
            var password = string.Join(" ", parts.Skip(2));

            try
            {
                // Отправляем сообщение о начале процесса
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "⏳ Выполняю привязку аккаунта...",
                    cancellationToken: cancellationToken);

                // Пробуем залогиниться
                var tokenInfo = await tokenService.LoginAsync(login, password);

                // Получаем информацию о пользователе
                journalApi.AccessToken = tokenInfo.AccessToken;
                var userInfo = await journalApi.UserInfoAsync();

                // Ищем или создаем пользователя
                var user = await dbContext.Users
                    .FirstOrDefaultAsync(u => u.JournalLogin == login);

                if (user == null)
                {
                    user = new Backend.API.Models.User
                    {
                        JournalLogin = login,
                        AccessToken = tokenInfo.AccessToken,
                        RefreshToken = tokenInfo.RefreshToken,
                        TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenInfo.ExpiresIn),
                        FullName = userInfo?.FullName,
                        Group = userInfo?.GroupName,
                        Role = UserRole.student,
                        CreatedAt = DateTime.UtcNow,
                        TelegramId = chatId,
                        IsTelegramLinked = true
                    };
                    dbContext.Users.Add(user);
                }
                else
                {
                    user.AccessToken = tokenInfo.AccessToken;
                    user.RefreshToken = tokenInfo.RefreshToken;
                    user.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenInfo.ExpiresIn);
                    user.LastLoginAt = DateTime.UtcNow;
                    user.TelegramId = chatId;
                    user.IsTelegramLinked = true;
                }

                // Получаем username если есть
                try
                {
                    var chat = await botClient.GetChatAsync(chatId, cancellationToken);
                    user.TelegramUsername = chat.Username;
                }
                catch { }

                await dbContext.SaveChangesAsync();

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"✅ Аккаунт успешно привязан!\n\n" +
                          $"👤 {user.FullName}\n" +
                          $"📚 Группа: {user.Group}\n\n" +
                          $"Теперь вы можете использовать /today и /week",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при привязке аккаунта");
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Ошибка при привязке аккаунта. Проверьте логин и пароль.",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandleTodayCommand(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var scheduleService = scope.ServiceProvider.GetRequiredService<TelegramScheduleService>();

            var schedule = await scheduleService.GetTodayScheduleForUser(chatId);

            // Просто отправляем текст, без клавиатуры
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: schedule,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        private async Task HandleWeekCommand(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var scheduleService = scope.ServiceProvider.GetRequiredService<TelegramScheduleService>();

            var schedule = await scheduleService.GetWeekScheduleForUser(chatId);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: schedule,
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        private async Task HandleHelpCommand(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            var helpMessage =
                "❓ <b>Помощь по боту</b>\n\n" +
                "<b>📱 Основные команды:</b>\n" +
                "/start - Главное меню\n" +
                "/today - Расписание на сегодня\n" +
                "/week - Расписание на неделю\n" +
                "/link логин пароль - Привязать аккаунт журнала\n" +
                "/help - Это сообщение\n\n" +
                "<b>🔐 Как привязать аккаунт:</b>\n" +
                "1. Отправьте /link ваш_логин ваш_пароль\n" +
                "2. Например: /link Homen_nw08 Q54hb6b7\n" +
                "3. После привязки можно смотреть расписание\n\n" +
                "<b>🌐 Веб-версия:</b>\n" +
                "Полное расписание доступно в веб-приложении";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📅 Сегодня", "today"),
                    InlineKeyboardButton.WithCallbackData("📆 Неделя", "week"),
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("◀️ Назад", "start")
                }
            });

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: helpMessage,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task HandleTextMessage(
            ITelegramBotClient botClient,
            long chatId,
            string messageText,
            CancellationToken cancellationToken)
        {
            var lowerText = messageText.ToLower().Trim();

            // Проверяем, привязан ли пользователь
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = await dbContext.Users
                .FirstOrDefaultAsync(u => u.TelegramId == chatId);

            // Если пользователь не привязан - предлагаем привязаться
            if (user == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Сначала привяжите аккаунт командой /link логин пароль",
                    cancellationToken: cancellationToken);
                return;
            }

            // Обработка запросов про расписание
            if (IsScheduleQuery(lowerText))
            {
                await HandleScheduleQuery(botClient, chatId, user, cancellationToken);
                return;
            }

            // Обработка запросов про следующую пару
            if (IsNextLessonQuery(lowerText))
            {
                await HandleNextLessonQuery(botClient, chatId, user, cancellationToken);
                return;
            }

            // Обработка запросов про конкретную дату
            if (IsDateQuery(lowerText))
            {
                await HandleDateQuery(botClient, chatId, user, messageText, cancellationToken);
                return;
            }

            // Ответ по умолчанию
            var responses = new[]
            {
                    "❓ Я не совсем понял. Попробуйте:\n/today - расписание на сегодня\n/week - на неделю\n/link - привязать аккаунт",
                    "🤔 Не могли бы уточнить? Например: 'какие пары сегодня?' или 'что завтра?'",
                    "📅 Я могу показать расписание. Спросите 'что сегодня?' или 'пары на завтра'",
                    "👨‍🎓 Если нужна помощь, используйте /help"
                };

            var random = new Random();
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: responses[random.Next(responses.Length)],
                cancellationToken: cancellationToken);
        }

        private bool IsScheduleQuery(string text)
        {
            var keywords = new[] {
            "пары", "расписание", "что сегодня", "какие пары",
            "что сейчас", "пары сегодня", "занятия", "уроки",
            "чё сегодня", "чё по парам", "что по парам"
        };

            return keywords.Any(keyword => text.Contains(keyword));
        }

        private bool IsNextLessonQuery(string text)
        {
            var keywords = new[] {
            "следующая пара", "что дальше", "когда следующая",
            "что потом", "дальше что", "следующее занятие"
        };

            return keywords.Any(keyword => text.Contains(keyword));
        }

        private bool IsDateQuery(string text)
        {
            var dateKeywords = new[] {
            "завтра", "послезавтра", "вчера", "понедельник", "вторник",
            "среда", "четверг", "пятница", "суббота", "воскресенье"
        };

            return dateKeywords.Any(keyword => text.Contains(keyword));
        }

        private async Task HandleScheduleQuery(
            ITelegramBotClient botClient,
            long chatId,
            Backend.API.Models.User user,
            CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _services.CreateScope();
                var journalApi = scope.ServiceProvider.GetRequiredService<JournalApi>();

                journalApi.AccessToken = user.AccessToken;
                var lessons = await journalApi.GetScheduleByDateAsync(DateTime.Today);

                if (lessons == null || !lessons.Any())
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "📭 На сегодня пар нет. Отдыхайте!",
                        cancellationToken: cancellationToken);
                    return;
                }

                var message = $"📅 <b>Расписание на {DateTime.Today:dd.MM.yyyy}</b>\n\n";
                var lessonNumber = 1;

                foreach (var lesson in lessons.OrderBy(l => TimeSpan.Parse(l.StartedAt)))
                {
                    message += $"<b>{lessonNumber}.</b> {lesson.StartedAt} – {lesson.FinishedAt}\n";
                    message += $"📚 {lesson.SubjectName}\n";
                    message += $"👨‍🏫 {lesson.TeacherName}\n";
                    message += $"📍 {lesson.RoomName ?? "ауд. не указана"}\n\n";
                    lessonNumber++;
                }

                // Добавляем следующую пару
                var now = DateTime.Now.TimeOfDay;
                var nextLesson = lessons
                    .Select(l => new { Lesson = l, Start = TimeSpan.Parse(l.StartedAt) })
                    .FirstOrDefault(l => l.Start > now);

                if (nextLesson != null)
                {
                    var timeUntil = nextLesson.Start - now;
                    message += $"⏳ Следующая через {timeUntil.Hours}ч {timeUntil.Minutes}м";
                }

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке запроса расписания");
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Не удалось получить расписание. Попробуйте позже.",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandleNextLessonQuery(
            ITelegramBotClient botClient,
            long chatId,
            Backend.API.Models.User user,
            CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _services.CreateScope();
                var journalApi = scope.ServiceProvider.GetRequiredService<JournalApi>();

                journalApi.AccessToken = user.AccessToken;
                var lessons = await journalApi.GetScheduleByDateAsync(DateTime.Today);

                if (lessons == null || !lessons.Any())
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "📭 На сегодня пар нет.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var now = DateTime.Now.TimeOfDay;
                var nextLesson = lessons
                    .Select(l => new { Lesson = l, Start = TimeSpan.Parse(l.StartedAt) })
                    .FirstOrDefault(l => l.Start > now);

                if (nextLesson == null)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "✅ На сегодня все пары закончились!",
                        cancellationToken: cancellationToken);
                    return;
                }

                var timeUntil = nextLesson.Start - now;
                var message = $"⏰ <b>Следующая пара</b>\n\n" +
                             $"📚 <b>{nextLesson.Lesson.SubjectName}</b>\n" +
                             $"👨‍🏫 {nextLesson.Lesson.TeacherName}\n" +
                             $"📍 {nextLesson.Lesson.RoomName ?? "ауд. не указана"}\n" +
                             $"⏱️ Начало в {nextLesson.Lesson.StartedAt}\n" +
                             $"🕒 Осталось {timeUntil.Hours}ч {timeUntil.Minutes}м";

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении следующей пары");
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Не удалось получить информацию.",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandleDateQuery(
    ITelegramBotClient botClient,
    long chatId,
    Backend.API.Models.User user,
    string messageText,
    CancellationToken cancellationToken)
        {
            try
            {
                var targetDate = DateTime.Today;
                var lowerText = messageText.ToLower();

                // Определяем дату
                if (lowerText.Contains("завтра"))
                    targetDate = DateTime.Today.AddDays(1);
                else if (lowerText.Contains("послезавтра"))
                    targetDate = DateTime.Today.AddDays(2);
                else if (lowerText.Contains("вчера"))
                    targetDate = DateTime.Today.AddDays(-1);
                else if (lowerText.Contains("понедельник"))
                    targetDate = GetNextWeekday(DayOfWeek.Monday);
                else if (lowerText.Contains("вторник"))
                    targetDate = GetNextWeekday(DayOfWeek.Tuesday);
                else if (lowerText.Contains("среда"))
                    targetDate = GetNextWeekday(DayOfWeek.Wednesday);
                else if (lowerText.Contains("четверг"))
                    targetDate = GetNextWeekday(DayOfWeek.Thursday);
                else if (lowerText.Contains("пятница"))
                    targetDate = GetNextWeekday(DayOfWeek.Friday);

                using var scope = _services.CreateScope();
                var journalApi = scope.ServiceProvider.GetRequiredService<JournalApi>();

                journalApi.AccessToken = user.AccessToken;
                var lessons = await journalApi.GetScheduleByDateAsync(targetDate);

                if (lessons == null || !lessons.Any())
                {
                    await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"📭 На {targetDate:dd.MM.yyyy} пар нет.",
                        cancellationToken: cancellationToken);
                    return;
                }

                var dayName = GetDayName(targetDate.DayOfWeek);
                var message = $"📅 <b>{dayName}, {targetDate:dd.MM.yyyy}</b>\n\n";
                var lessonNumber = 1;

                foreach (var lesson in lessons.OrderBy(l => TimeSpan.Parse(l.StartedAt)))
                {
                    message += $"<b>{lessonNumber}.</b> {lesson.StartedAt} – {lesson.FinishedAt}\n";
                    message += $"📚 {lesson.SubjectName}\n";
                    message += $"👨‍🏫 {lesson.TeacherName}\n";
                    message += $"📍 {lesson.RoomName ?? "ауд. не указана"}\n\n";
                    lessonNumber++;
                }

                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: message,
                    parseMode: ParseMode.Html,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении расписания по дате");
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Не удалось получить расписание.",
                    cancellationToken: cancellationToken);
            }
        }

        private DateTime GetNextWeekday(DayOfWeek day)
        {
            var date = DateTime.Today;
            while (date.DayOfWeek != day)
                date = date.AddDays(1);
            return date;
        }

        private string GetDayName(DayOfWeek day)
        {
            var days = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "Понедельник" },
                { DayOfWeek.Tuesday, "Вторник" },
                { DayOfWeek.Wednesday, "Среда" },
                { DayOfWeek.Thursday, "Четверг" },
                { DayOfWeek.Friday, "Пятница" },
                { DayOfWeek.Saturday, "Суббота" },
                { DayOfWeek.Sunday, "Воскресенье" }
            };
            return days[day];
        }

        private Task HandleErrorAsync(
            ITelegramBotClient botClient,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(errorMessage);
            return Task.CompletedTask;
        }
    }
}