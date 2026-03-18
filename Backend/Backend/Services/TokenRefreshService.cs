using Backend.API.Data;
using Backend.API.Models;
using Microsoft.EntityFrameworkCore;
using TopAcademyAPI.Journal;

namespace Backend.API.Services
{
    public class TokenRefreshService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<TokenRefreshService> _logger;

        public TokenRefreshService(
            IServiceProvider services,
            ILogger<TokenRefreshService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Refresh Service запущен");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Запускаем обновление токенов
                    await RefreshExpiredTokens();

                    // Ждем 30 минут до следующей проверки
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в Token Refresh Service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }

        private async Task RefreshExpiredTokens()
        {
            using var scope = _services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tokenService = scope.ServiceProvider.GetRequiredService<TokenService>();

            // Ищем студентов, у которых токен скоро истечет (меньше часа)
            var soonExpiring = await dbContext.Users
                .Where(u => u.Role == UserRole.student &&
                            u.RefreshToken != null &&
                            u.TokenExpiresAt < DateTime.UtcNow.AddHours(1))
                .ToListAsync();

            _logger.LogInformation($"Найдено {soonExpiring.Count} студентов с истекающими токенами");

            foreach (var student in soonExpiring)
            {
                try
                {
                    _logger.LogInformation($"Обновление токена для {student.JournalLogin}");

                    // Обновляем токен через RefreshToken
                    var newTokens = await tokenService.RefreshAsync(student.RefreshToken!);

                    // Обновляем в БД
                    student.RefreshToken = newTokens.RefreshToken;
                    // AccessToken не обновляем - он пока действителен
                    // TokenExpiresAt тоже не меняем - access_token живет столько же

                    _logger.LogInformation($"Refresh токен для {student.JournalLogin} успешно обновлен");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Не удалось обновить токен для {student.JournalLogin}");

                    // Если не удалось обновить - очищаем токены
                    student.AccessToken = null;
                    student.RefreshToken = null;
                    student.TokenExpiresAt = null;
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}