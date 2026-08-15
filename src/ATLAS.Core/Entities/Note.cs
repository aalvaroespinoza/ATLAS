namespace ATLAS.Core.Entities;

/// <summary>
/// Domain model for a captured quick note.
/// </summary>
public class Note
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Content { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string Source { get; init; } = "quick_capture";
}
