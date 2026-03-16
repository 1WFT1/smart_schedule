using TopAcademyAPI.Journal.Factories;
using TopAcademyAPI.Journal.Services.Http;

namespace TopAcademyAPI.Journal;

public sealed class JournalApi
{
    private string? _accessToken;

    public string? AccessToken
    {
        get => _accessToken;
        set
        {
            if (_accessToken == value && _accessToken != null) return;
            _accessToken = value;
            HttpService = CreateHttpService(_accessToken);
        }
    }

    public HttpService HttpService { get; private set; } = null!;

    public JournalApi(string? token = null)
    {
        AccessToken = token;
    }

    private static HttpService CreateHttpService(string? token = null)
    {
        var httpClient = HttpClientFactory.Create(token);
        return new HttpService(httpClient);
    }
}
