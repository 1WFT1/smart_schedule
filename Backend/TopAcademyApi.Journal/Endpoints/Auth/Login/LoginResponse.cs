using System.Text.Json.Serialization;
using TopAcademyAPI.Journal.Endpoints.Auth.Login.Models;

namespace TopAcademyAPI.Journal.Endpoints.Auth.Login;

public record LoginResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken,
    [property: JsonPropertyName("expires_in_refresh")] int ExpiresInRefresh,
    [property: JsonPropertyName("expires_in_access")] int ExpiresInAccess,
    [property: JsonPropertyName("user_type")] int UserType,
    [property: JsonPropertyName("city_data")] CityData CityData,
    [property: JsonPropertyName("user_role")] string UserRole);