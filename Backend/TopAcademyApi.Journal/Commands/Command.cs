using TopAcademyAPI.Journal.Exceptions;

namespace TopAcademyAPI.Journal.Commands;

public static class Command
{
    public static async Task<TResponse> ExecuteAsync<TResponse>(Func<Task<TResponse>> action, JournalApi journalApi, bool isSkipCheckAccessToken = false)
    {
        
        // Console.WriteLine(journalApi.AccessToken);
        if (!isSkipCheckAccessToken && journalApi.AccessToken == null)
        {
            throw new NullTokenException();
        }
        //
        // journalApi.AccessToken = token;
        
        return await action();
    }
    
}