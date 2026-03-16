using System.Text.Json.Serialization;
using TopAcademyAPI.Journal.Endpoints.Homework.Operations.List.Models;

namespace TopAcademyAPI.Journal.Endpoints.Homework.Operations.List;

public record ListHomeworkResponse(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("id_spec")] int IdSpec,
    [property: JsonPropertyName("id_teach")] int IdTeach,
    [property: JsonPropertyName("id_group")] int IdGroup,
    [property: JsonPropertyName("fio_teach")] string FioTeach,
    [property: JsonPropertyName("theme")] string Theme,
    [property: JsonPropertyName("completion_time")] string CompletionTime,
    [property: JsonPropertyName("creation_time")] string CreationTime,
    [property: JsonPropertyName("overdue_time")] string OverdueTime,
    [property: JsonPropertyName("filename")] string? Filename,
    [property: JsonPropertyName("file_path")] string? FilePath,
    [property: JsonPropertyName("comment")] string? Comment,
    [property: JsonPropertyName("name_spec")] string NameSpec,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("common_status")] int? CommonStatus,
    [property: JsonPropertyName("homework_stud")] HomeworkStud HomeworkStud,
    [property: JsonPropertyName("homework_comment")] HomeworkComment HomeworkComment,
    [property: JsonPropertyName("cover_image")] string CoverImage
);