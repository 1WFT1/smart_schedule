using System.Net;
using TopAcademyAPI.Journal.Services.Http;

namespace TopAcademyAPI.Journal.Factories;

public static class HttpClientFactory
{
    public static HttpClient Create(string? token = null)
    {
        var httpClientHandler = new HttpClientHandler 
        { 
            CookieContainer = new CookieContainer(),
            UseCookies = true
        };
        var httpClient = new HttpClient(httpClientHandler);
        httpClient.Configure(token);

        return httpClient;
    }
}