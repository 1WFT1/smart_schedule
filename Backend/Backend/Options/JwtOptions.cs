namespace Backend.API.Options
{
    // Настройки JWT
    public class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string Key { get; set; } = "my-super-secret-key-12345!!!-change-this-in-production";
        public string Issuer { get; set; } = "ScheduleAPI";
        public string Audience { get; set; } = "ScheduleClient";
    }
}
