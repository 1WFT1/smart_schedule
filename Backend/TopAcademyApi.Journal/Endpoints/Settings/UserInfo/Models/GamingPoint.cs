using System.Text.Json.Serialization;

namespace TopAcademyAPI.Journal.Endpoints.Settings.UserInfo.Models;

public record GamingPoint(
    [property: JsonPropertyName("new_gaming_point_types__id")] int Id,
    [property: JsonPropertyName("points")] int Points);