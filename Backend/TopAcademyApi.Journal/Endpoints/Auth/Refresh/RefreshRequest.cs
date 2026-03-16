using System.Text.Json.Serialization;

namespace TopAcademyAPI.Journal.Endpoints.Auth.Refresh;

public record RefreshRequest(
    [property: JsonPropertyName("refresh_token")] string RefreshToken);