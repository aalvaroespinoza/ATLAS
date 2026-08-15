namespace ATLAS.Core.Entities;

/// <summary>
/// Domain model representing a raw completion event for a habit.
/// </summary>
public class HabitEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string HabitId { get; init; } = string.Empty;
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? Note { get; init; }
}
