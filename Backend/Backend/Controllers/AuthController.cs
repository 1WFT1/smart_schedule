using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Interfaces;
using Backend.API.Models;
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

        public AuthController(
            JournalApi journalApi,
            ApplicationDbContext dbContext,
            IConfiguration configuration,
            IPasswordHasher passwordHasher,
            ILogger<AuthController> logger)
        {
            _journalApi = journalApi;
            _dbContext = dbContext;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }


        //Вход для студентов через журнал
        [HttpPost("student-login")]
        public async Task<ActionResult<LoginResponseDto>> StudentLogin(StudentLoginDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Попытка входа студента: {loginDto.Username}");

                // 1. Вход в журнал
                var loginRequest = new LoginRequest(loginDto.Username, loginDto.Password);
                var loginResponse = await _journalApi.LoginAsync(loginRequest);

                if (loginResponse == null)
                    return Unauthorized(new { Message = "Неверный логин или пароль" });

                // 2. Получаем информацию о студенте
                var userInfo = await _journalApi.UserInfoAsync();

                // 3. Ищем или создаем студента в БД
                var student = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.JournalLogin == loginDto.Username && u.Role == UserRole.student);

                if (student == null)
                {
                    student = new User
                    {
                        JournalLogin = loginDto.Username,
                        EncryptedJournalPassword = EncryptPassword(loginDto.Password),
                        FullName = userInfo?.FullName,
                        Group = userInfo?.GroupName,
                        Role = UserRole.student,
                        CreatedAt = DateTime.UtcNow
                    };
                    _dbContext.Users.Add(student);
                }
                else
                {
                    student.EncryptedJournalPassword = EncryptPassword(loginDto.Password);
                    student.FullName = userInfo?.FullName;
                    student.Group = userInfo?.GroupName;
                    student.LastLoginAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();

                // 4. ПРИВЯЗКА К ГРУППЕ - ДОБАВЛЯЕМ ЭТОТ КОД
                if (!string.IsNullOrEmpty(userInfo?.GroupName))
                {
                    var group = await _dbContext.Groups
                        .FirstOrDefaultAsync(g => g.Name == userInfo.GroupName);

                    if (group != null)
                    {
                        student.StudentGroupId = group.Id;
                        group.StudentCount = (group.StudentCount ?? 0) + 1;
                        await _dbContext.SaveChangesAsync();

                        _logger.LogInformation($"Студент {student.FullName} привязан к группе {group.Name}");
                    }
                    else
                    {
                        _logger.LogWarning($"Группа {userInfo.GroupName} не найдена в БД");
                    }
                }

                // 5. Генерируем JWT токен
                var token = GenerateJwtToken(student, isAdmin: false);

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = student.Id,
                        Username = student.JournalLogin ?? "",
                        FullName = student.FullName,
                        Role = "student",
                        Group = student.Group
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе студента");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }


        // Вход для администраторов (созданных вручную)
        [HttpPost("admin-login")]
        public async Task<ActionResult<LoginResponseDto>> AdminLogin(AdminLoginDto loginDto)
        {
            try
            {
                _logger.LogInformation($"Попытка входа админа: {loginDto.Username}");

                // Ищем админа по Username (а не AdminUsername)
                var admin = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.Role == UserRole.admin);

                if (admin == null)
                    return Unauthorized(new { Message = "Неверный логин или пароль" });

                // Проверяем пароль
                if (!_passwordHasher.Verify(loginDto.Password, admin.PasswordHash ?? ""))
                    return Unauthorized(new { Message = "Неверный логин или пароль" });

                admin.LastLoginAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                // Генерируем JWT токен
                var token = GenerateJwtToken(admin, isAdmin: true);

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = admin.Id,
                        Username = admin.Username ?? "",
                        FullName = admin.FullName ?? "Администратор",
                        Role = "admin",
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

        //Парсит группу из UserInfoResponse
        private string? ParseGroup(UserInfoResponse? userInfo)
        {
            if (userInfo?.Groups != null && userInfo.Groups.Any())
            {
                return string.Join(", ", userInfo.Groups.Select(g => g.Name));
            }
            return null;
        }


        private string GenerateJwtToken(User user, bool isAdmin)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, isAdmin ? "admin" : "student")
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Name, user.Username ?? ""));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Name, user.JournalLogin ?? ""));
                claims.Add(new Claim("group", user.Group ?? ""));
                claims.Add(new Claim("fullName", user.FullName ?? ""));
            }

            var expires = DateTime.UtcNow.AddDays(7);
            claims.Add(new Claim(JwtRegisteredClaimNames.Exp, new DateTimeOffset(expires).ToUnixTimeSeconds().ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "my-super-secret-key-12345!!!-change-this-in-production"));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "ScheduleAPI",
                audience: _configuration["Jwt:Audience"] ?? "ScheduleClient",
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            _logger.LogInformation($"Сгенерирован токен для пользователя {user.Id} с ролью {(isAdmin ? "admin" : "student")}");

            return tokenString;
        }

        private string EncryptPassword(string password)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(password);
            return Convert.ToBase64String(plainTextBytes);
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
