using TopAcademyAPI.Journal.Commands;
using TopAcademyAPI.Journal.Endpoints.Auth.Login;

namespace TopAcademyAPI.Journal.Endpoints.Auth.Refresh;

public static class RefreshEndpoint
{
    public static async Task<RefreshResponse> RefreshAsync(this JournalApi journalApi, RefreshRequest request)
    {
        return await Command.ExecuteAsync(() => journalApi.HttpService.PostAsync<RefreshRequest, RefreshResponse>(BaseEndpoints.RefreshEndpoint, request), journalApi, isSkipCheckAccessToken: true);
    }
}