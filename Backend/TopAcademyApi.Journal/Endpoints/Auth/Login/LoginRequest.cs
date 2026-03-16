using System.Text.Json.Serialization;

namespace TopAcademyAPI.Journal.Endpoints.Auth.Login;

public record LoginRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password,
    [property: JsonPropertyName("application_key")] string? ApplicationKey = "6a56a5df2667e65aab73ce76d1dd737f7d1faef9c52e8b8c55ac75f565d8e8a6",
    [property: JsonPropertyName("id_city")] string? IdCity = null);