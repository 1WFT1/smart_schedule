using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static Backend.API.Models.Event;

namespace Backend.API.Controllers
{

    // Контроллер для работы с внеурочными мероприятиями (Events)
    // Студенты могут только просматривать
    // Админы могут создавать, редактировать и удалять

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            ApplicationDbContext dbContext,
            ILogger<EventsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET: api/events
        [HttpGet]
        public async Task<ActionResult<List<EventDto>>> GetEvents([FromQuery] DateTime? date)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                _logger.LogInformation($"Получение событий на {targetDate:yyyy-MM-dd} для пользователя {userId} с ролью {user?.Role}");

                if (user == null)
                {
                    return Unauthorized(new { Message = "Пользователь не найден" });
                }

                var startOfDay = targetDate.Date.ToUniversalTime();
                var endOfDay = targetDate.Date.AddDays(1).ToUniversalTime().AddTicks(-1);

                // 1. Сначала загружаем все события за день
                var events = await _dbContext.Events
                    .Where(e => e.StartTime >= startOfDay && e.StartTime <= endOfDay)
                    .ToListAsync(); // Загружаем в память!

                _logger.LogInformation($"Загружено {events.Count} событий из БД");

                // 2. Фильтруем в памяти для студентов
                if (user.Role == UserRole.student)
                {
                    if (!string.IsNullOrEmpty(user.Group))
                    {
                        var studentGroups = user.Group.Split(',').Select(g => g.Trim()).ToList();
                        _logger.LogInformation($"Группы студента: {string.Join(", ", studentGroups)}");

                        // Фильтрация в памяти с помощью LINQ to Objects
                        events = events
                            .Where(e =>
                                e.TargetGroups == null ||
                                !e.TargetGroups.Any() ||
                                e.TargetGroups.Any(g => studentGroups.Contains(g)))
                            .ToList();
                    }
                    else
                    {
                        _logger.LogInformation("У студента нет группы, показываем все события");
                    }
                }

                // 3. Сортируем и конвертируем
                events = events.OrderBy(e => e.StartTime).ToList();

                _logger.LogInformation($"После фильтрации осталось {events.Count} событий");

                var eventDtos = events.Select(e => MapToDto(e)).ToList();
                return Ok(eventDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении событий");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера", Error = ex.Message });
            }
        }

        // POST: api/events
        [HttpPost]
        [Authorize(Roles = "teacher,admin")]
        public async Task<ActionResult<EventDto>> CreateEvent(CreateEventDto createDto)
        {
            try
            {
                var userId = GetCurrentUserId();

                _logger.LogInformation($"Создание события пользователем {userId}");

                var newEvent = new Event
                {
                    Type = createDto.Type,
                    Category = createDto.Category,
                    Name = createDto.Name,
                    Teacher = createDto.Teacher,
                    Room = createDto.Room,
                    Group = createDto.Group,
                    StartTime = createDto.StartTime.ToUniversalTime(),
                    EndTime = createDto.EndTime.ToUniversalTime(),
                    Tags = createDto.Tags ?? new List<string>(),
                    TargetGroups = createDto.TargetGroups,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Events.Add(newEvent);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Событие создано с ID: {newEvent.Id}");

                return Ok(MapToDto(newEvent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании события");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // PUT: api/events/5
        [HttpPut("{id}")]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> UpdateEvent(int id, CreateEventDto updateDto)
        {
            try
            {
                var evt = await _dbContext.Events.FindAsync(id);
                if (evt == null)
                    return NotFound(new { Message = "Событие не найдено" });

                // Проверка прав
                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                if (userRole != "admin" && evt.CreatedByUserId != userId)
                    return Forbid();

                // Обновляем поля
                evt.Type = updateDto.Type;
                evt.Category = updateDto.Category;
                evt.Name = updateDto.Name;
                evt.Teacher = updateDto.Teacher;
                evt.Room = updateDto.Room;
                evt.Group = updateDto.Group;
                evt.StartTime = updateDto.StartTime.ToUniversalTime();
                evt.EndTime = updateDto.EndTime.ToUniversalTime();
                evt.Tags = updateDto.Tags ?? new List<string>();
                evt.TargetGroups = updateDto.TargetGroups;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Событие {id} обновлено");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении события");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // DELETE: api/events/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "teacher,admin")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                _logger.LogInformation("=== УДАЛЕНИЕ СОБЫТИЯ ===");

                var userId = GetCurrentUserId();
                var userRole = GetCurrentUserRole();

                _logger.LogInformation($"User ID: {userId}");
                _logger.LogInformation($"User Role: {userRole}");

                // Логируем все claims для отладки
                foreach (var claim in User.Claims)
                {
                    _logger.LogInformation($"Claim: {claim.Type} = {claim.Value}");
                }

                var evt = await _dbContext.Events.FindAsync(id);
                if (evt == null)
                {
                    _logger.LogWarning($"Событие {id} не найдено");
                    return NotFound(new { Message = "Событие не найдено" });
                }

                // Проверка прав
                if (userRole != "admin" && evt.CreatedByUserId != userId)
                {
                    _logger.LogWarning($"Пользователь {userId} с ролью {userRole} не имеет прав на удаление события {id}");
                    return Forbid();
                }

                _dbContext.Events.Remove(evt);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Событие {id} успешно удалено пользователем {userId}");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении события");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        private string GetCurrentUserRole()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(roleClaim))
            {
                return roleClaim;
            }

            // Если нет, пробуем другие варианты
            roleClaim = User.FindFirst("role")?.Value;

            if (!string.IsNullOrEmpty(roleClaim))
            {
                return roleClaim;
            }

            // Проверяем, может быть это студент
            if (User.HasClaim(c => c.Type == "group"))
            {
                return "student";
            }

            return "student";
        }

        private EventDto MapToDto(Event evt)
        {
            var now = DateTime.Now;
            var isCurrent = now >= evt.StartTime && now <= evt.EndTime;
            var timeRemaining = isCurrent ? GetTimeRemaining(evt.EndTime) : null;

            return new EventDto
            {
                Id = evt.Id,
                Type = evt.Type,
                Category = evt.Category,
                Name = evt.Name,
                Teacher = evt.Teacher,
                Room = evt.Room,
                Group = evt.Group,
                Tags = evt.Tags,
                IsCurrent = isCurrent,
                TimeRemaining = timeRemaining,
                StartTime = evt.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                EndTime = evt.EndTime.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }

        private string? GetTimeRemaining(DateTime endTime)
        {
            var remaining = endTime - DateTime.Now;
            if (remaining.TotalMinutes <= 0) return null;

            if (remaining.TotalHours >= 1)
                return $"до конца {Math.Floor(remaining.TotalHours)} ч {remaining.Minutes} мин";
            else
                return $"до конца {remaining.Minutes} мин";
        }
    }
}