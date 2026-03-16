namespace TopAcademyAPI.Journal.Endpoints;

public static class BaseEndpoints
{
    // Ѕазовые URL endpoints дл€ API журнала
    public const string ReferrerEndpoint = "https://journal.top-academy.ru/";
    public const string LoginEndpoint  = BaseEndpoint + "auth/login";
    public const string RefreshEndpoint  = BaseEndpoint + "auth/refresh";
    public const string UserInfoEndpoint  = BaseEndpoint + "settings/user-info";
    public const string HomeworkEndpoint  = BaseEndpoint + "count/homework";
    public const string ListHomeworkEndpoint  = BaseEndpoint + "homework/operations/list";

    // Endpoint дл€ получени€ расписани€ по дате
    public const string ScheduleGetByDateEndpoint = BaseEndpoint + "schedule/operations/get-by-date";



    private const string BaseEndpoint = "https://msapi.top-academy.ru/api/v2/";
}