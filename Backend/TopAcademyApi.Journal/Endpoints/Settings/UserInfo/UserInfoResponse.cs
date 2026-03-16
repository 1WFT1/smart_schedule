using System.Text.Json.Serialization;
using TopAcademyAPI.Journal.Endpoints.Settings.UserInfo.Models;

namespace TopAcademyAPI.Journal.Endpoints.Settings.UserInfo;

    
public record UserInfoResponse(
    [property: JsonPropertyName("groups")] List<Group> Groups,
    [property: JsonPropertyName("manual_link")] string? ManualLink,
    [property: JsonPropertyName("student_id")] int StudentId,
    [property: JsonPropertyName("current_group_id")] int CurrentGroupId,
    [property: JsonPropertyName("full_name")] string FullName,
    [property: JsonPropertyName("achieves_count")] int AchievesCount,
    [property: JsonPropertyName("stream_id")] int StreamId,
    [property: JsonPropertyName("stream_name")] string StreamName,
    [property: JsonPropertyName("group_name")] string GroupName,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("photo")] string Photo,
    [property: JsonPropertyName("gaming_points")] List<GamingPoint> GamingPoints,
    [property: JsonPropertyName("spent_gaming_points")] List<GamingPoint> SpentGamingPoints,
    [property: JsonPropertyName("visibility")] Visibility Visibility,
    [property: JsonPropertyName("current_group_status")] int CurrentGroupStatus,
    [property: JsonPropertyName("birthday")] string Birthday,
    [property: JsonPropertyName("age")] int Age,
    [property: JsonPropertyName("last_date_visit")] string LastDateVisit,
    [property: JsonPropertyName("registration_date")] string RegistrationDate,
    [property: JsonPropertyName("gender")] int Gender,
    [property: JsonPropertyName("study_form_short_name")] string StudyFormShortName
);
