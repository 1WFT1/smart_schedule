using System.Text.Json.Serialization;

namespace TopAcademyAPI.Journal.Endpoints.Auth.Login.Models;

public record CityData(
    [property: JsonPropertyName("id_city")] int IdCity,
    [property: JsonPropertyName("prefix")] string Prefix,
    [property: JsonPropertyName("translate_key")] string TranslateKey,
    [property: JsonPropertyName("timezone_name")] string TimezoneName,
    [property: JsonPropertyName("country_code")] string CountryCode,
    [property: JsonPropertyName("market_status")] int MarketStatus,
    [property: JsonPropertyName("name")] string Name
);