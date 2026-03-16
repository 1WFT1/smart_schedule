using System.Text.Json.Serialization;

namespace TopAcademyAPI.Journal.Endpoints.Auth.Refresh;

public record RefreshResponse(
    [property: JsonPropertyName("refresh_token")] string RefreshToken);