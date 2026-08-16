using ATLAS.Core.Entities;

namespace ATLAS.Core.Repositories;

/// <summary>
/// Repository interface for persisting and querying Roadmaps and Milestones.
/// </summary>
public interface IRoadmapRepository
{
    Task<Roadmap?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<Roadmap?> GetByGoalIdAsync(string goalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Roadmap>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default);
    Task CreateAsync(Roadmap roadmap, IEnumerable<RoadmapMilestone>? milestones = null, CancellationToken cancellationToken = default);
    Task AddMilestoneAsync(RoadmapMilestone milestone, CancellationToken cancellationToken = default);
    Task UpdateMilestoneStatusAsync(string milestoneId, string status, DateTimeOffset? completedAt, CancellationToken cancellationToken = default);
    Task<RoadmapMilestone?> GetMilestoneByIdAsync(string milestoneId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
