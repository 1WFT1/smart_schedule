using Backend.API.Data;
using Backend.API.DTOs;
using Backend.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<UserController> _logger;

        public UserController(
            ApplicationDbContext dbContext,
            ILogger<UserController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpPost("notifications/toggle")]
        public async Task<IActionResult> ToggleNotifications()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                if (user == null)
                    return NotFound();

                user.NotificationsEnabled = !user.NotificationsEnabled;
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    enabled = user.NotificationsEnabled,
                    message = user.NotificationsEnabled
                        ? "🔔 Уведомления включены"
                        : "🔕 Уведомления выключены"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при переключении уведомлений");
                return StatusCode(500);
            }
        }

        [HttpPost("notifications/time")]
        public async Task<IActionResult> SetNotificationTime([FromBody] int minutes)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                if (user == null)
                    return NotFound();

                if (minutes < 5 || minutes > 60)
                    return BadRequest(new { message = "Время должно быть от 5 до 60 минут" });

                user.NotificationMinutesBefore = minutes;
                await _dbContext.SaveChangesAsync();

                return Ok(new
                {
                    minutes = user.NotificationMinutesBefore,
                    message = $"⏰ Уведомления за {minutes} минут до пары"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при установке времени уведомлений");
                return StatusCode(500);
            }
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                if (user == null)
                    return NotFound();

                return Ok(new
                {
                    notifications = user.NotificationsEnabled,
                    notificationTime = user.NotificationMinutesBefore,
                    telegramLinked = user.TelegramId.HasValue,
                    telegramUsername = user.TelegramUsername
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении настроек");
                return StatusCode(500);
            }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}