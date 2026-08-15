namespace ATLAS.Core.Entities;

/// <summary>
/// Domain model representing a Habit definition in ATLAS.
/// </summary>
public class Habit
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Frequency { get; init; } = "daily"; // "daily", "weekly:N", "days:1,3,5"
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
