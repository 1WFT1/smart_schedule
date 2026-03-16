using System.Text.Json.Serialization;

namespace TopAcademyAPI.Journal.Endpoints.Count.Homework;

public record HomeworkResponse(
    [property: JsonPropertyName("counter_type")] int CounterType,
    [property: JsonPropertyName("counter")] int Counter);