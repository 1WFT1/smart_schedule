using System.Text.Json.Serialization;
using TopAcademyAPI.Journal;

namespace Backend.API.Models.JournalApi
{
    // Модель для одного занятия (очищенная от лишних полей API)
    public class JournalLessonDto
    {
        public int LessonNumber { get; set; }
        public string StartedAt { get; set; } = string.Empty;
        public string FinishedAt { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
    }

    // Маппер для конвертации из API модели в вашу модель
    public static class JournalLessonMapper
    {
        public static JournalLessonDto MapFromApi(TopAcademyAPI.Journal.Endpoints.Schedule.JournalApiLessonDto apiLesson)
        {
            return new JournalLessonDto
            {
                LessonNumber = apiLesson.LessonNumber,
                StartedAt = apiLesson.StartedAt,
                FinishedAt = apiLesson.FinishedAt,
                TeacherName = apiLesson.TeacherName,
                SubjectName = apiLesson.SubjectName,
                RoomName = apiLesson.RoomName,
                Group = apiLesson.Group
            };
        }
    }



}
