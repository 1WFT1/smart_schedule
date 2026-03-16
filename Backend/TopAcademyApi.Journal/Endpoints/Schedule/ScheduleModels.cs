using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TopAcademyAPI.Journal.Endpoints.Schedule
{
    // Модель занятия из API журнала
    // Соответствует реальному ответу от API
    public class JournalApiLessonDto
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;           // Дата занятия

        [JsonPropertyName("lesson")]
        public int LessonNumber { get; set; }                      // Номер пары (4,5,6...)

        [JsonPropertyName("started_at")]
        public string StartedAt { get; set; } = string.Empty;      // Время начала (14:40)

        [JsonPropertyName("finished_at")]
        public string FinishedAt { get; set; } = string.Empty;     // Время окончания (16:00)

        [JsonPropertyName("teacher_name")]
        public string TeacherName { get; set; } = string.Empty;    // ФИО преподавателя

        [JsonPropertyName("subject_name")]
        public string SubjectName { get; set; } = string.Empty;    // Название предмета

        [JsonPropertyName("room_name")]
        public string RoomName { get; set; } = string.Empty;       // Номер аудитории

        [JsonPropertyName("group")]
        public string Group { get; set; } = string.Empty;       // Группа
    }
}
