using TopAcademyAPI.Journal;
using TopAcademyAPI.Journal.Endpoints.Auth.Login;
using TopAcademyAPI.Journal.Endpoints.Auth.Refresh;
using Backend.API.Models;

namespace Backend.API.Services
{
    public class TokenService
    {
        private readonly JournalApi _journalApi;
        private readonly ILogger<TokenService> _logger;

        public TokenService(JournalApi journalApi, ILogger<TokenService> logger)
        {
            _journalApi = journalApi;
            _logger = logger;
        }

        public async Task<TokenInfo> LoginAsync(string login, string password)
        {
            try
            {
                _logger.LogInformation($"Попытка входа для {login}");

                var request = new LoginRequest(login, password);
                var response = await _journalApi.LoginAsync(request);

                if (response == null)
                {
                    throw new Exception("Не удалось получить токены");
                }

                _logger.LogInformation($"Успешный вход для {login}");

                return new TokenInfo
                {
                    AccessToken = response.AccessToken,
                    RefreshToken = response.RefreshToken,
                    ExpiresIn = response.ExpiresInAccess,
                    UserType = response.UserType,
                    UserRole = response.UserRole
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка входа для {login}");
                throw;
            }
        }

        public async Task<TokenInfo> RefreshAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("Попытка обновления токена");

                var request = new RefreshRequest(refreshToken);
                var response = await _journalApi.RefreshAsync(request);

                if (response == null)
                {
                    throw new Exception("Не удалось обновить токен");
                }

                _logger.LogInformation("Токен успешно обновлен");

                // ВАЖНО: Refresh возвращает только новый refresh_token!
                // Для получения нового access_token нужно:
                // 1. Либо использовать старый access_token пока не истек
                // 2. Либо сделать новый Login с сохраненными данными

                return new TokenInfo
                {
                    AccessToken = null, // Не приходит в ответе!
                    RefreshToken = response.RefreshToken,
                    ExpiresIn = 0 // Не приходит в ответе!
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления токена");
                throw;
            }
        }

        public bool IsTokenValid(DateTime? expiresAt)
        {
            return expiresAt.HasValue && expiresAt.Value > DateTime.UtcNow.AddMinutes(5);
        }

        public async Task<TokenInfo?> TryRefreshIfNeededAsync(User user)
        {
            if (user == null || string.IsNullOrEmpty(user.RefreshToken))
                return null;

            // Если токен скоро истечет (меньше часа)
            if (user.TokenExpiresAt < DateTime.UtcNow.AddHours(1))
            {
                try
                {
                    return await RefreshAsync(user.RefreshToken);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }

    public class TokenInfo
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public int UserType { get; set; }
        public string UserRole { get; set; } = string.Empty;
    }
}