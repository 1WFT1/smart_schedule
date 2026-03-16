using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;


namespace Backend.API.Services


{
    public class TelegramBotService : IHostedService
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<TelegramBotService> _logger;
        private readonly string _webAppUrl;
        private CancellationTokenSource _cts;

        public TelegramBotService(
            IOptions<TelegramBotConfiguration> config,
            ILogger<TelegramBotService> logger)
        {
            _logger = logger;
            _botClient = new TelegramBotClient(config.Value.Token);
            _webAppUrl = config.Value.WebAppUrl ?? "https://web.telegram.org/k/";
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Инициализируем CancellationTokenSource
                _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                _logger.LogInformation("Получаем информацию о боте...");

                // Получаем информацию о боте
                var me = await _botClient.GetMeAsync();
                _logger.LogInformation($"Бот @{me.Username} успешно запущен!");

                // Начинаем получать обновления
                _botClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    pollingErrorHandler: HandleErrorAsync,
                    receiverOptions: new ReceiverOptions
                    {
                        AllowedUpdates = Array.Empty<UpdateType>(),
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
                // Обрабатываем только сообщения
                if (update.Message is not { } message)
                    return;

                // Обрабатываем только текстовые сообщения
                if (message.Text is not { } messageText)
                    return;

                var chatId = message.Chat.Id;
                var username = message.From?.Username ?? "пользователь";

                _logger.LogInformation($"Получено сообщение от {username}: {messageText}");

                // Обработка команды /start
                if (messageText.StartsWith("/start"))
                {
                    await HandleStartCommand(botClient, chatId, cancellationToken);
                    return;
                }

                // Обработка команды /schedule
                if (messageText.StartsWith("/schedule"))
                {
                    await HandleScheduleCommand(botClient, chatId, cancellationToken);
                    return;
                }

                // Обработка обычных текстовых сообщений
                await HandleTextMessage(botClient, chatId, messageText, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке обновления");
            }
        }

        private async Task HandleStartCommand(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            // Создаем кнопку для открытия Mini App
            var keyboard = new InlineKeyboardMarkup(new[]
            {
            InlineKeyboardButton.WithWebApp(
                "📅 Открыть расписание",
                new WebAppInfo { Url = _webAppUrl }
            )
        });

            var welcomeMessage =
                "👋 Привет! Я бот для расписания колледжа.\n\n" +
                "📌 Доступные команды:\n" +
                "/start - Показать это сообщение\n" +
                "/schedule - Получить расписание\n\n" +
                "📱 Нажмите кнопку ниже, чтобы открыть веб-приложение с полным расписанием";

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: welcomeMessage,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task HandleScheduleCommand(
            ITelegramBotClient botClient,
            long chatId,
            CancellationToken cancellationToken)
        {
            // Заглушка - позже здесь будет реальное расписание из БД
            var scheduleMessage =
                "📚 Расписание на сегодня:\n\n" +
                "1. 09:00 - Математика (ауд. 301)\n" +
                "2. 10:45 - Физика (ауд. 415)\n" +
                "3. 13:00 - Программирование (ауд. 202)\n\n" +
                "Используйте кнопку ниже для детального просмотра";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
            InlineKeyboardButton.WithWebApp(
                "📅 Открыть полное расписание",
                new WebAppInfo { Url = _webAppUrl }
            )
        });

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: scheduleMessage,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        private async Task HandleTextMessage(
            ITelegramBotClient botClient,
            long chatId,
            string messageText,
            CancellationToken cancellationToken)
        {
            var response = messageText.ToLower() switch
            {
                var text when text.Contains("привет") => "Привет! Чем могу помочь?",
                var text when text.Contains("расписание") =>
                    "Используйте команду /schedule или откройте веб-приложение",
                var text when text.Contains("пара") || text.Contains("аудитория") =>
                    "Эта функция скоро будет доступна! Сейчас используйте /schedule",
                _ => $"Я пока учусь понимать такие запросы. Попробуйте:\n" +
                     "/start - основные команды\n" +
                     "/schedule - получить расписание\n" +
                     "Или откройте веб-приложение для полного функционала"
            };

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: response,
                cancellationToken: cancellationToken);
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

    public class TelegramBotConfiguration
    {
        public string Token { get; set; } = string.Empty;
        public string WebAppUrl { get; set; } = string.Empty;
    }
}