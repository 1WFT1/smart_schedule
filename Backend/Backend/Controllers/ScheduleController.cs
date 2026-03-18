using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Backend.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TopAcademyAPI.Journal;
using TopAcademyAPI.Journal.Endpoints.Schedule; // Для GetScheduleByDateAsync

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "student,teacher,admin")]
    public class ScheduleController : ControllerBase
    {
        private readonly JournalApi _journalApi;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<ScheduleController> _logger;
        private readonly JournalScheduleService _journalScheduleService;
        private readonly TokenService _tokenService;

        public ScheduleController(
            JournalApi journalApi,
            ApplicationDbContext dbContext,
            ILogger<ScheduleController> logger,
            JournalScheduleService journalScheduleService,
            TokenService tokenService)
        {
            _journalApi = journalApi;
            _dbContext = dbContext;
            _logger = logger;
            _journalScheduleService = journalScheduleService;
            _tokenService = tokenService;
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

                // Проверяем токен
                if (!_tokenService.IsTokenValid(user.TokenExpiresAt))
                {
                    return Unauthorized(new { Message = "Сессия истекла. Войдите заново." });
                }

                _journalApi.AccessToken = user.AccessToken;
                var lessons = await _journalApi.GetScheduleByDateAsync(DateTime.Today);

                if (lessons == null || !lessons.Any())
                {
                    return Ok(new { Message = "Нет пар на сегодня" });
                }

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

                var eventDto = ConvertToEventDto(currentLesson, DateTime.Today, user.Group);

                return Ok(eventDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении текущего занятия");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("today")]
        [Authorize(Roles = "student")]
        public async Task<ActionResult<List<EventDto>>> GetTodaySchedule()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                if (user == null)
                {
                    return Unauthorized(new { Message = "Пользователь не найден" });
                }

                if (string.IsNullOrEmpty(user.JournalLogin))
                {
                    return BadRequest(new { Message = "Студент не привязан к журналу" });
                }

                // Проверяем токен
                if (!_tokenService.IsTokenValid(user.TokenExpiresAt))
                {
                    return Unauthorized(new { Message = "Сессия истекла. Войдите заново." });
                }

                _journalApi.AccessToken = user.AccessToken;
                var lessons = await _journalApi.GetScheduleByDateAsync(DateTime.Today);

                if (lessons == null || !lessons.Any())
                {
                    return Ok(new List<EventDto>());
                }

                var eventDtos = lessons
                    .Select(l => ConvertToEventDto(l, DateTime.Today, user.Group))
                    .ToList();

                return Ok(eventDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении расписания");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("day")]
        public async Task<ActionResult<List<EventDto>>> GetDaySchedule([FromQuery] DateTime? date)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;
                var userId = GetCurrentUserId();

                var user = await _dbContext.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { Message = "Пользователь не найден" });
                }

                // Для админа
                if (user.Role == UserRole.admin)
                {
                    _logger.LogInformation("Админ запрашивает расписание");
                    return Ok(new List<EventDto>());
                }

                // Для студента
                if (string.IsNullOrEmpty(user.JournalLogin))
                {
                    return BadRequest(new { Message = "Студент не привязан к журналу" });
                }

                // Проверяем токен
                if (!_tokenService.IsTokenValid(user.TokenExpiresAt))
                {
                    var newTokens = await _tokenService.TryRefreshIfNeededAsync(user);
                    if (newTokens != null && newTokens.AccessToken != null)
                    {
                        user.AccessToken = newTokens.AccessToken;
                        user.RefreshToken = newTokens.RefreshToken;
                        await _dbContext.SaveChangesAsync();
                    }
                    else
                    {
                        return Unauthorized(new { Message = "Сессия истекла. Войдите заново." });
                    }
                }

                _journalApi.AccessToken = user.AccessToken;
                var lessons = await _journalApi.GetScheduleByDateAsync(targetDate);

                if (lessons == null || !lessons.Any())
                {
                    return Ok(new List<EventDto>());
                }

                var eventDtos = lessons
                    .Select(l => ConvertToEventDto(l, targetDate, user.Group))
                    .ToList();

                return Ok(eventDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении расписания на день");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("group/{groupName}/week")]
        public async Task<ActionResult<Dictionary<string, List<EventDto>>>> GetGroupWeekSchedule(
            string groupName,
            [FromQuery] DateTime? startDate)
        {
            try
            {
                var start = startDate ?? DateTime.Today;
                var end = start.AddDays(6);

                _logger.LogInformation($"Запрос расписания для группы {groupName} с {start:yyyy-MM-dd} по {end:yyyy-MM-dd}");

                // Находим любого студента из группы с токеном
                var student = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Group == groupName &&
                                              u.Role == UserRole.student &&
                                              u.AccessToken != null &&
                                              u.TokenExpiresAt > DateTime.UtcNow);

                if (student == null)
                {
                    _logger.LogWarning($"Нет активных студентов в группе {groupName}");
                    return Ok(new Dictionary<string, List<EventDto>>());
                }

                _journalApi.AccessToken = student.AccessToken;

                var weekSchedule = new Dictionary<string, List<EventDto>>();

                // Загружаем расписание для каждого дня недели
                for (var date = start; date <= end; date = date.AddDays(1))
                {
                    var lessons = await _journalApi.GetScheduleByDateAsync(date);

                    if (lessons != null && lessons.Any())
                    {
                        var dayEvents = lessons.Select(lesson => new EventDto
                        {
                            Id = lesson.LessonNumber,
                            Type = "lecture",
                            Category = "study",
                            Time = $"{lesson.StartedAt} – {lesson.FinishedAt}",
                            Name = lesson.SubjectName,
                            Teacher = lesson.TeacherName,
                            Room = lesson.RoomName ?? "",
                            Group = groupName,
                            Tags = new List<string> { "Занятие" },
                            StartTime = date.Date.Add(TimeSpan.Parse(lesson.StartedAt)).ToString("yyyy-MM-ddTHH:mm:ss"),
                            EndTime = date.Date.Add(TimeSpan.Parse(lesson.FinishedAt)).ToString("yyyy-MM-ddTHH:mm:ss")
                        }).ToList();

                        weekSchedule[date.ToString("yyyy-MM-dd")] = dayEvents;
                    }
                    else
                    {
                        weekSchedule[date.ToString("yyyy-MM-dd")] = new List<EventDto>();
                    }
                }

                return Ok(weekSchedule);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при получении расписания для группы {groupName}");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // Метод конвертации
        private EventDto ConvertToEventDto(TopAcademyAPI.Journal.Endpoints.Schedule.JournalApiLessonDto lesson, DateTime date, string? group)
        {
            var startTime = date.Date.Add(TimeSpan.Parse(lesson.StartedAt));
            var endTime = date.Date.Add(TimeSpan.Parse(lesson.FinishedAt));
            var now = DateTime.Now;
            var isCurrent = now >= startTime && now <= endTime;

            return new EventDto
            {
                Id = lesson.LessonNumber,
                Type = "lecture",
                Category = "study",
                Time = $"{lesson.StartedAt} – {lesson.FinishedAt}",
                Name = lesson.SubjectName,
                Teacher = lesson.TeacherName,
                Room = lesson.RoomName,
                Group = group,
                Tags = new List<string> { "Занятие" },
                IsCurrent = isCurrent,
                TimeRemaining = isCurrent ? GetTimeRemaining(endTime) : null,
                StartTime = startTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                EndTime = endTime.ToString("yyyy-MM-ddTHH:mm:ss")
            };
        }

        private string? GetTimeRemaining(DateTime endTime)
        {
            var remaining = endTime - DateTime.Now;
            if (remaining.TotalMinutes <= 0) return null;

            if (remaining.TotalHours >= 1)
                return $"до конца {Math.Ceiling(remaining.TotalHours)} ч {remaining.Minutes} мин";
            else
                return $"до конца {remaining.Minutes} мин";
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}