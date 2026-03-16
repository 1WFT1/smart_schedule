using Backend.API.Data;
using Backend.API.Models;
using Backend.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "teacher,admin")] // Только кураторы и админы
    public class GroupsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<GroupsController> _logger;

        public GroupsController(
            ApplicationDbContext dbContext,
            ILogger<GroupsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET: api/groups
        // Получить все группы (которыми управляет куратор)
        [HttpGet]
        public async Task<ActionResult<List<GroupDto>>> GetGroups()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                if (user == null)
                    return Unauthorized();

                IQueryable<Group> query = _dbContext.Groups
                    .Include(g => g.Students)
                    .Include(g => g.Curator)
                    .Where(g => g.IsActive);

                // Для куратора показываем только его группы
                if (user.Role == UserRole.teacher)
                {
                    query = query.Where(g => g.CuratorId == userId);
                }
                // Для админа показываем все группы

                var groups = await query
                    .OrderBy(g => g.Name)
                    .ToListAsync();

                return Ok(groups.Select(g => new GroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    DisplayName = g.DisplayName ?? g.Name,
                    StudentCount = g.Students?.Count ?? g.StudentCount ?? 0,
                    CreatedAt = g.CreatedAt,
                    LastActive = g.LastActive,
                    Source = g.Source,
                    IsActive = g.IsActive,
                    CuratorId = g.CuratorId,
                    CuratorName = g.Curator?.FullName
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения групп");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // GET: api/groups/{id}
        // Получить конкретную группу
        [HttpGet("{id}")]
        public async Task<ActionResult<GroupDto>> GetGroup(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                var group = await _dbContext.Groups
                    .Include(g => g.Students)
                    .Include(g => g.Curator)
                    .FirstOrDefaultAsync(g => g.Id == id && g.IsActive);

                if (group == null)
                    return NotFound();

                // Проверяем права доступа
                if (user?.Role == UserRole.teacher && group.CuratorId != userId)
                    return Forbid();

                return Ok(new GroupDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    DisplayName = group.DisplayName ?? group.Name,
                    StudentCount = group.Students?.Count ?? group.StudentCount ?? 0,
                    CreatedAt = group.CreatedAt,
                    LastActive = group.LastActive,
                    Source = group.Source,
                    IsActive = group.IsActive,
                    CuratorId = group.CuratorId,
                    CuratorName = group.Curator?.FullName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения группы");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // POST: api/groups
        // Создать новую группу
        [HttpPost]
        public async Task<ActionResult<GroupDto>> CreateGroup(CreateGroupDto createDto)
        {
            _logger.LogInformation($"Получен запрос на создание группы: Name={createDto?.Name}, DisplayName={createDto?.DisplayName}");

            // Проверяем, что Name обязателен
            if (string.IsNullOrEmpty(createDto?.Name))
            {
                return BadRequest(new { Message = "Имя группы обязательно" });
            }

            try
            {
                var curatorId = GetCurrentUserId();
                var curator = await _dbContext.Users.FindAsync(curatorId);

                if (curator == null)
                    return Unauthorized();

                // Проверяем, существует ли уже такая группа
                var existing = await _dbContext.Groups
                    .FirstOrDefaultAsync(g => g.Name == createDto.Name);

                if (existing != null)
                {
                    return BadRequest(new { Message = "Группа с таким названием уже существует" });
                }

                var group = new Group
                {
                    Name = createDto.Name,
                    DisplayName = createDto.DisplayName ?? createDto.Name,
                    StudentCount = createDto.StudentCount,
                    CreatedAt = DateTime.UtcNow,
                    Source = createDto.Source ?? "manual",
                    IsActive = true,
                    CuratorId = curatorId
                };

                _dbContext.Groups.Add(group);
                await _dbContext.SaveChangesAsync();

                return Ok(new GroupDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    DisplayName = group.DisplayName,
                    StudentCount = group.StudentCount ?? 0,
                    CreatedAt = group.CreatedAt,
                    LastActive = group.LastActive,
                    Source = group.Source,
                    IsActive = group.IsActive,
                    CuratorId = group.CuratorId,
                    CuratorName = curator.FullName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка создания группы");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // PUT: api/groups/{id}
        // Обновить группу
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGroup(int id, UpdateGroupDto updateDto)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                var group = await _dbContext.Groups.FindAsync(id);
                if (group == null)
                    return NotFound();

                // Проверяем права доступа
                if (user?.Role == UserRole.teacher && group.CuratorId != userId)
                    return Forbid();

                if (updateDto.Name != null)
                    group.Name = updateDto.Name;

                if (updateDto.DisplayName != null)
                    group.DisplayName = updateDto.DisplayName;

                if (updateDto.StudentCount.HasValue)
                    group.StudentCount = updateDto.StudentCount;

                if (updateDto.IsActive.HasValue)
                    group.IsActive = updateDto.IsActive.Value;

                await _dbContext.SaveChangesAsync();
                return Ok(new { Message = "Группа обновлена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обновления группы");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // DELETE: api/groups/{id}
        // Удалить группу (мягкое удаление)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGroup(int id)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                var group = await _dbContext.Groups.FindAsync(id);
                if (group == null)
                    return NotFound();

                // Проверяем права доступа
                if (user?.Role == UserRole.teacher && group.CuratorId != userId)
                    return Forbid();

                // Мягкое удаление
                group.IsActive = false;
                await _dbContext.SaveChangesAsync();

                return Ok(new { Message = "Группа удалена" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка удаления группы");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // POST: api/groups/auto-add
        // Автоматическое добавление группы при входе студента
        [HttpPost("auto-add")]
        [AllowAnonymous] // Доступно из студенческого логина
        public async Task<ActionResult<GroupDto>> AutoAddGroup([FromBody] string groupName)
        {
            try
            {
                // Проверяем, существует ли группа
                var group = await _dbContext.Groups
                    .FirstOrDefaultAsync(g => g.Name == groupName);

                if (group == null)
                {
                    // Создаем новую группу автоматически
                    group = new Group
                    {
                        Name = groupName,
                        DisplayName = groupName,
                        StudentCount = 1,
                        CreatedAt = DateTime.UtcNow,
                        LastActive = DateTime.UtcNow,
                        Source = "journal",
                        IsActive = true,
                        CuratorId = null // Без куратора, пока не назначат
                    };

                    _dbContext.Groups.Add(group);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Автоматически создана группа: {groupName}");
                }
                else
                {
                    // Обновляем lastActive и счетчик
                    group.LastActive = DateTime.UtcNow;
                    group.StudentCount = (group.StudentCount ?? 0) + 1;
                    await _dbContext.SaveChangesAsync();
                }

                return Ok(new GroupDto
                {
                    Id = group.Id,
                    Name = group.Name,
                    DisplayName = group.DisplayName ?? group.Name,
                    StudentCount = group.StudentCount ?? 0,
                    CreatedAt = group.CreatedAt,
                    LastActive = group.LastActive,
                    Source = group.Source,
                    IsActive = group.IsActive,
                    CuratorId = group.CuratorId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка автоматического добавления группы");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        // GET: api/groups/students/{groupId}
        // Получить студентов группы
        [HttpGet("students/{groupId}")]
        public async Task<ActionResult<List<UserDto>>> GetGroupStudents(int groupId)
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _dbContext.Users.FindAsync(userId);

                var group = await _dbContext.Groups
                    .Include(g => g.Students)
                    .FirstOrDefaultAsync(g => g.Id == groupId && g.IsActive);

                if (group == null)
                    return NotFound();

                // Проверяем права доступа
                if (user?.Role == UserRole.teacher && group.CuratorId != userId)
                    return Forbid();

                var students = group.Students?
                    .Where(s => s.Role == UserRole.student)
                    .Select(s => new UserDto
                    {
                        Id = s.Id,
                        Username = s.JournalLogin ?? s.Username ?? "",
                        FullName = s.FullName,
                        Role = s.Role.ToString(),
                        Group = s.Group
                    })
                    .ToList() ?? new List<UserDto>();

                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения студентов группы");
                return StatusCode(500, new { Message = "Внутренняя ошибка сервера" });
            }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}