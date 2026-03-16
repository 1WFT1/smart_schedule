using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TopAcademyAPI.Journal;
using TopAcademyAPI.Journal.Endpoints.Auth.Login;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "student")]
    public class ScheduleController : ControllerBase
    {
        private readonly JournalApi _journalApi;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ScheduleController> _logger;
        private readonly JournalScheduleService _journalScheduleService;

        public ScheduleController(
            JournalApi journalApi,
            ApplicationDbContext dbContext,
            ILogger<ScheduleController> logger,
            JournalScheduleService journalScheduleService)
        {
            _journalApi = journalApi;
            _dbContext = dbContext;
            _logger = logger;
             _journalScheduleService = journalScheduleService;
        }

        [HttpGet("current-lesson")]
        public async Task<ActionResult<EventDto>> GetCurrentLesson()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                if (user == null || string.IsNullOrEmpty(user.JournalLogin))
                    return BadRequest(new { Message = "Студент не привязан к журналу" });

                var password = DecryptPassword(user.EncryptedJournalPassword ?? "");

                // Получаем расписание на сегодня из реального API
                var lessons = await _journalScheduleService.GetDayScheduleAsync(
                    user.JournalLogin,
                    password,
                    DateTime.Today);

                if (lessons == null || !lessons.Any())
                {
                    return Ok(new { Message = "Нет пар на сегодня" });
                }

                // Находим текущее занятие
                var now = DateTime.Now.TimeOfDay;
                var currentLesson = lessons.FirstOrDefault(l =>
                {
                    var start = TimeSpan.Parse(l.StartedAt);
                    var end = TimeSpan.Parse(l.FinishedAt);
                    return now >= start && now <= end;
                });

                if (currentLesson == null)
                {
                    return Ok(new { Message = "Сейчас нет пары", IsBreak = true });
                }

                var eventDto = _journalScheduleService.ConvertToEventDto(currentLesson, DateTime.Today);

                return Ok(eventDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении текущего занятия");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }


        // ПОЛУЧИТЬ ВСЕ ПАРЫ НА СЕГОДНЯ - то, что нужно вашему фронтенду!
        [HttpGet("today")]
        [Authorize(Roles = "student")]
        public async Task<ActionResult<List<EventDto>>> GetTodaySchedule()
        {
            try
            {
                _logger.LogInformation("=== ЗАПРОС РАСПИСАНИЯ ===");

                // Логируем все claims из токена
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
                }

                var userId = GetCurrentUserId();
                _logger.LogInformation($"User ID из токена: {userId}");

                var user = await _dbContext.Users.FindAsync(userId);

                if (user == null)
                {
                    _logger.LogWarning($"Пользователь {userId} не найден в БД");
                    return Unauthorized(new { Message = "Пользователь не найден" });
                }

                if (string.IsNullOrEmpty(user.JournalLogin))
                {
                    _logger.LogWarning($"У пользователя {userId} нет JournalLogin");
                    return BadRequest(new { Message = "Студент не привязан к журналу" });
                }

                _logger.LogInformation($"Пользователь: {user.JournalLogin}, роль: {user.Role}");

                var password = DecryptPassword(user.EncryptedJournalPassword ?? "");

                var lessons = await _journalScheduleService.GetDayScheduleAsync(
                    user.JournalLogin,
                    password,
                    DateTime.Today);

                if (lessons == null || !lessons.Any())
                {
                    return Ok(new List<EventDto>());
                }

                var eventDtos = lessons
                    .Select(l => _journalScheduleService.ConvertToEventDto(l, DateTime.Today))
                    .ToList();

                return Ok(eventDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении расписания");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }


        /// <summary>
        /// ПОЛУЧИТЬ ВСЕ ПАРЫ НА КОНКРЕТНУЮ ДАТУ
        /// </summary>
        [HttpGet("day")]
        public async Task<ActionResult<List<EventDto>>> GetDaySchedule([FromQuery] DateTime? date)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;
                var userId = GetCurrentUserId();

                _logger.LogInformation($"Получение расписания на {targetDate:yyyy-MM-dd} для пользователя {userId}");

                var user = await _dbContext.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { Message = "Пользователь не найден" });
                }

                if (string.IsNullOrEmpty(user.JournalLogin))
                {
                    return BadRequest(new { Message = "Студент не привязан к журналу" });
                }

                var password = DecryptPassword(user.EncryptedJournalPassword ?? "");

                var lessons = await _journalScheduleService.GetDayScheduleAsync(
                    user.JournalLogin,
                    password,
                    targetDate);

                if (lessons == null || !lessons.Any())
                {
                    return Ok(new List<EventDto>()); // Пустой массив
                }

                var eventDtos = lessons
                    .Select(l => _journalScheduleService.ConvertToEventDto(l, targetDate))
                    .ToList();

                return Ok(eventDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении расписания на день");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }



        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            _logger.LogInformation($"ClaimTypes.NameIdentifier: {claim?.Value}");

            if (claim == null)
            {
                // Пробуем другие варианты
                claim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                _logger.LogInformation($"XML claim: {claim?.Value}");
            }

            return claim != null ? int.Parse(claim.Value) : 0;
        }

        private string DecryptPassword(string encryptedPassword)
        {
            try
            {
                var base64EncodedBytes = Convert.FromBase64String(encryptedPassword);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch
            {
                return encryptedPassword; // Если не получается расшифровать, возвращаем как есть
            }
        }
    }
}