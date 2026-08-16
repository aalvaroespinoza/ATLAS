namespace ATLAS.Core.Entities;

/// <summary>
/// Represents a structured sequential roadmap, optionally linked to a high-level Goal.
/// </summary>
public class Roadmap
{
    public string Id { get; set; } = string.Empty;
    public string? GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = "active"; // active, completed, archived
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<RoadmapMilestone> Milestones { get; set; } = [];

    /// <summary>
    /// Computes the percentage of completed milestones (0-100).
    /// </summary>
    public int ProgressPercentage
    {
        get
        {
            if (Milestones.Count == 0) return 0;
            var completed = Milestones.Count(m => string.Equals(m.Status, "completed", StringComparison.OrdinalIgnoreCase));
            return (int)Math.Round((double)completed / Milestones.Count * 100);
        }
    }
}
