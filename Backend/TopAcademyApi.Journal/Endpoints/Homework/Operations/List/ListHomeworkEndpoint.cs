using TopAcademyAPI.Journal.Commands;

namespace TopAcademyAPI.Journal.Endpoints.Homework.Operations.List;

public static class ListHomeworkEndpoint
{
    
    // https://msapi.top-academy.ru/api/v2/homework/operations/list?page=1&status=0&type=0&group_id=3
    public static async Task<List<ListHomeworkResponse>?> ListHomeworkAsync(this JournalApi journalApi, int? status = null,
        int? type = null, int? groupId = null, int? page = 0)
    {
        var endpoint = BaseEndpoints.ListHomeworkEndpoint;
        var queryParts = new List<string>();
        if (page != null)
        {
            queryParts.Add($"page={page}");
        }
        if (status != null)
        {
            queryParts.Add($"status={status}");
        }
        if (type != null)
        {
            queryParts.Add($"type={type}");
        }
        if (groupId != null)
        {
            queryParts.Add($"group_id={groupId}");
        }
        if (queryParts.Count > 0)
        {
            endpoint += "?" + string.Join("&", queryParts);
        }
        
        Console.WriteLine(endpoint);
        var action = journalApi.HttpService.GetAsync<List<ListHomeworkResponse>>(endpoint);
        return await Command.ExecuteAsync(() => action, journalApi);
    }
}
