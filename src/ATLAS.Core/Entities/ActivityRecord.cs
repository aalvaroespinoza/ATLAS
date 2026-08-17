namespace ATLAS.Core.Entities;

/// <summary>
/// Domain model representing a normalized activity event in the system.
/// </summary>
public class ActivityRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// Type of activity: "knowledge", "strategy", "finance", "system", "communication"
    /// </summary>
    public string Type { get; init; } = string.Empty;
    
    /// <summary>
    /// The ID of the original entity (NoteId, TransactionId, HabitId, etc.)
    /// </summary>
    public string? SourceId { get; init; }
    
    /// <summary>
    /// Human-readable title of the activity.
    /// </summary>
    public string Title { get; init; } = string.Empty;
    
    /// <summary>
    /// Optional context or description.
    /// </summary>
    public string? Summary { get; init; }
    
    /// <summary>
    /// Relevance score from 0 to 10 to determine if it should be shown in feeds/notifications.
    /// </summary>
    public int RelevanceScore { get; init; }
    
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
