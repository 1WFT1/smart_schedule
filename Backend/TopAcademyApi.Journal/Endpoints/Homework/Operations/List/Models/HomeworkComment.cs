using System.Text.Json.Serialization;

namespace TopAcademyAPI.Journal.Endpoints.Homework.Operations.List.Models;

public record HomeworkComment(
    [property: JsonPropertyName("text_comment")] string? TextComment,
    [property: JsonPropertyName("attachment")] string? Attachment,
    [property: JsonPropertyName("attachment_path")] string? AttachmentPath,
    [property: JsonPropertyName("date_updated")] string DateUpdated
);
