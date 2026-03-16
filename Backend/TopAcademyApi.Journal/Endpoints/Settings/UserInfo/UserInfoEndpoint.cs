using TopAcademyAPI.Journal.Commands;

namespace TopAcademyAPI.Journal.Endpoints.Settings.UserInfo;

public static class UserInfoEndpoint
{
    public static async Task<UserInfoResponse?> UserInfoAsync(this JournalApi journalApi)
    {
        return await Command.ExecuteAsync(() => journalApi.HttpService.GetAsync<UserInfoResponse>(BaseEndpoints.UserInfoEndpoint), journalApi);
    }
}