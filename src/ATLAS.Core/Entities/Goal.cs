namespace ATLAS.Core.Entities;

/// <summary>
/// Domain model representing a personal Goal in ATLAS.
/// </summary>
public class Goal
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Status { get; init; } = "active"; // "active", "completed", "paused", "abandoned"
    public int Progress { get; init; } // 0 to 100
    public DateTimeOffset? TargetDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
