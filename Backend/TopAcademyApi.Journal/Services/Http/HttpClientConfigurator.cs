using System.Net.Http.Headers;
using TopAcademyAPI.Journal.Endpoints;

namespace TopAcademyAPI.Journal.Services.Http;


public static class HttpClientConfigurator
{
    public static void Configure(this HttpClient httpClient, string? token = null)
    {
        if (token != null) 
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        
        httpClient.DefaultRequestHeaders.Referrer = new Uri(BaseEndpoints.ReferrerEndpoint);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }
}