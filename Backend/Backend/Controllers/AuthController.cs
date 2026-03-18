using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Interfaces;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TopAcademyAPI.Journal;
using TopAcademyAPI.Journal.Endpoints.Auth.Login;
using TopAcademyAPI.Journal.Endpoints.Settings.UserInfo;
using TopAcademyAPI.Journal.Exceptions;

namespace Backend.API.Controllers
{
    // Контроллер для аутентификации пользователей
    // Студенты входят через журнал, админы по логину/паролю

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JournalApi _journalApi;
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthController> _logger;
        private readonly TokenService _tokenService;

        public AuthController(
            JournalApi journalApi,
            ApplicationDbContext dbContext,
            IConfiguration configuration,
            IPasswordHasher passwordHasher,
            ILogger<AuthController> logger,
            TokenService tokenService)
        {
            _journalApi = journalApi;
            _dbContext = dbContext;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _tokenService = tokenService;
        }


        //Вход для студентов через журнал
        [HttpPost("student-login")]
        public async Task<ActionResult<LoginResponseDto>> StudentLogin(StudentLoginDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Попытка входа студента: {loginDto.Username}");

                // 1. Получаем токены через TokenService (пароль НЕ сохраняем!)
                var tokenInfo = await _tokenService.LoginAsync(loginDto.Username, loginDto.Password);

                // 2. Устанавливаем токен для получения информации о пользователе
                _journalApi.AccessToken = tokenInfo.AccessToken;
                var userInfo = await _journalApi.UserInfoAsync();

                // 3. Ищем существующего студента
                var student = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.JournalLogin == loginDto.Username);

                if (student == null)
                {
                    // Создаем нового студента (без пароля!)
                    student = new User
                    {
                        JournalLogin = loginDto.Username,
                        AccessToken = tokenInfo.AccessToken,
                        RefreshToken = tokenInfo.RefreshToken,
                        TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenInfo.ExpiresIn),
                        FullName = userInfo?.FullName,
                        Group = userInfo?.GroupName,
                        Role = UserRole.student,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.Users.Add(student);
                }
                else
                {
                    // Обновляем токены (пароль не трогаем!)
                    student.AccessToken = tokenInfo.AccessToken;
                    student.RefreshToken = tokenInfo.RefreshToken;
                    student.TokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenInfo.ExpiresIn);
                    student.FullName = userInfo?.FullName;
                    student.Group = userInfo?.GroupName;
                    student.LastLoginAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();

                // 4. Привязка к группе (с автоматическим созданием)
                if (!string.IsNullOrEmpty(userInfo?.GroupName))
                {
                    // Ищем существующую группу
                    var group = await _dbContext.Groups
                        .FirstOrDefaultAsync(g => g.Name == userInfo.GroupName);

                    // Если группы нет - создаём!
                    if (group == null)
                    {
                        _logger.LogInformation($"Группа {userInfo.GroupName} не найдена, создаём новую...");

                        group = new Group
                        {
                            Name = userInfo.GroupName,
                            DisplayName = userInfo.GroupName,
                            Source = "journal",
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            StudentCount = 0
                        };
                        _dbContext.Groups.Add(group);
                        await _dbContext.SaveChangesAsync();

                        _logger.LogInformation($"Группа {userInfo.GroupName} успешно создана");
                    }

                    // Привязываем студента к группе
                    student.StudentGroupId = group.Id;

                    // Обновляем счётчик студентов в группе
                    group.StudentCount = await _dbContext.Users
                        .CountAsync(u => u.StudentGroupId == group.Id);

                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Студент {student.FullName} привязан к группе {group.Name}");
                }

                // 5. Генерируем JWT для нашего API
                var token = GenerateJwtToken(student);

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = student.Id,
                        Username = student.JournalLogin ?? "",
                        FullName = student.FullName,
                        Role = student.Role.ToString(),
                        Group = student.Group
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе студента");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера", Error = ex.Message });
            }
        }


        // Вход для администраторов (созданных вручную)
        [HttpPost("admin-login")]
        public async Task<ActionResult<LoginResponseDto>> AdminLogin(AdminLoginDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Попытка входа админа: {loginDto.Username}");

                var admin = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username &&
                                             (u.Role == UserRole.admin || u.Role == UserRole.teacher));

                if (admin == null)
                    return Unauthorized(new { Message = "Неверный логин или пароль" });

                if (!_passwordHasher.Verify(loginDto.Password, admin.PasswordHash ?? ""))
                    return Unauthorized(new { Message = "Неверный логин или пароль" });

                admin.LastLoginAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                var token = GenerateJwtToken(admin);

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = admin.Id,
                        Username = admin.Username ?? "",
                        FullName = admin.FullName ?? "Администратор",
                        Role = admin.Role.ToString(),
                        Group = null
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе админа");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username ?? user.JournalLogin ?? ""),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "my-super-secret-key-12345!!!-change-this-in-production"));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddDays(7);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "ScheduleAPI",
                audience: _configuration["Jwt:Audience"] ?? "ScheduleClient",
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }

    // DTO для входа студента
    public class StudentLoginDto
    {
        public string Username { get; set; } = string.Empty;  // Логин от журнала
        public string Password { get; set; } = string.Empty;  // Пароль от журнала
    }

    // DTO для входа админа
    public class AdminLoginDto
    {
        public string Username { get; set; } = string.Empty;  // Логин админа
        public string Password { get; set; } = string.Empty;  // Пароль админа
    }
}
