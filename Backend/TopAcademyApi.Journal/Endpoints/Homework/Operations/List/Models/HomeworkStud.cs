using System.Text.Json.Serialization;

namespace TopAcademyAPI.Journal.Endpoints.Homework.Operations.List.Models;

public record HomeworkStud(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("filename")] string? Filename,
    [property: JsonPropertyName("file_path")] string? FilePath,
    [property: JsonPropertyName("tmp_file")] string? TmpFile,
    [property: JsonPropertyName("mark")] int? Mark,
    [property: JsonPropertyName("creation_time")] string CreationTime,
    [property: JsonPropertyName("stud_answer")] string StudAnswer,
    [property: JsonPropertyName("auto_mark")] bool AutoMark
);