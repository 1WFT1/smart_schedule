using TopAcademyAPI.Journal.Commands;

namespace TopAcademyAPI.Journal.Endpoints.Auth.Login;

public static class LoginEndpoint
{
    public static async Task<LoginResponse?> LoginAsync(this JournalApi journalApi, LoginRequest request)
    {
        var action = journalApi.HttpService.PostAsync<LoginRequest, LoginResponse>(BaseEndpoints.LoginEndpoint, request);
        
        var response =  await Command.ExecuteAsync(() => action, journalApi, isSkipCheckAccessToken: true);

        journalApi.AccessToken = response.AccessToken;
        
        return response;
    }
}