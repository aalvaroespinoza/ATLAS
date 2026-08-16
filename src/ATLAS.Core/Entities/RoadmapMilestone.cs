namespace ATLAS.Core.Entities;

/// <summary>
/// Represents an ordered milestone / step in a structured roadmap.
/// </summary>
public class RoadmapMilestone
{
    public string Id { get; set; } = string.Empty;
    public string RoadmapId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string Status { get; set; } = "pending"; // pending, completed
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
