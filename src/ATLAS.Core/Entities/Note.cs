namespace ATLAS.Core.Entities;

/// <summary>
/// Domain model for a note in the ATLAS Knowledge system.
/// </summary>
public class Note
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string? Title { get; init; }
    public string Content { get; init; } = string.Empty;
    public string Type { get; init; } = "note";
    public string? Tags { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public string Source { get; init; } = "quick_capture";
}
