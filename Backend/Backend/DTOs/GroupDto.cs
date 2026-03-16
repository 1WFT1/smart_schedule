namespace Backend.API.DTOs
{
    public class GroupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActive { get; set; }
        public string Source { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int? CuratorId { get; set; }
        public string? CuratorName { get; set; }
    }

    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public int? StudentCount { get; set; }
        public string? Source { get; set; }
    }

    public class UpdateGroupDto
    {
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public int? StudentCount { get; set; }
        public bool? IsActive { get; set; }
    }
}