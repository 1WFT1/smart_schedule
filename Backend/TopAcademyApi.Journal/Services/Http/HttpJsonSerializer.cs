using System.Net.Http.Json;
using System.Text.Json;

namespace TopAcademyAPI.Journal.Services.Http;

public static class HttpJsonSerializer
{
    public static HttpContent Serialize<TRequest>(TRequest data)
    {
        return JsonContent.Create(data); 
    }
    
    public static async Task<TResponse> DeserializeAsync<TResponse>(HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(json)!;
    }
}