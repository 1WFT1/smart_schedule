using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "teacher,admin")]
    public class CuratorController : ControllerBase
    {
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            return Ok(new
            {
                totalLessons = 42,
                totalEvents = 8,
                freeRooms = 14
            });
        }

        [HttpGet("groups")]
        public IActionResult GetGroups()
        {
            return Ok(new[]
            {
            new { id = "all", name = "Все группы", count = 24 },
            new { id = "group1", name = "ПИ-21-1", count = 12 },
            new { id = "group2", name = "ПИ-21-2", count = 12 }
        });
        }

        [HttpGet("week-schedule")]
        public IActionResult GetWeekSchedule()
        {
            return Ok(new[]
            {
            new { dayIndex = 1, subject = "Программирование", room = "404", teacher = "Иванов", type = "lecture", timeSlot = 2 },
            new { dayIndex = 2, subject = "Собрание", room = "Актовый зал", teacher = "", type = "event", timeSlot = 3 }
        });
        }

        [HttpPost("sync")]
        public IActionResult SyncWithJournal()
        {
            return Ok(new { success = true, message = "Синхронизация выполнена" });
        }
    }
}
