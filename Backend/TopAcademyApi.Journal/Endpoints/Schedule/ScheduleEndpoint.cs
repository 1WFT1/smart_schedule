using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TopAcademyAPI.Journal.Commands;


namespace TopAcademyAPI.Journal.Endpoints.Schedule
{
    // Endpoint для получения расписания из API журнала
    public static class ScheduleEndpoint
    {
        // Получить расписание на конкретную дату
        // "journalApi" Экземпляр JournalAp 
        // "date" Дата для получения расписания
        // Список занятий на указанную дату
        public static async Task<List<JournalApiLessonDto>?> GetScheduleByDateAsync(
            this JournalApi journalApi,
            DateTime date)
        {
            var endpoint = $"{BaseEndpoints.ScheduleGetByDateEndpoint}?date_filter={date:yyyy-MM-dd}";

            // API возвращает прямой массив объектов
            return await Command.ExecuteAsync(
                () => journalApi.HttpService.GetAsync<List<JournalApiLessonDto>>(endpoint),
                journalApi);
        }
    }


}
