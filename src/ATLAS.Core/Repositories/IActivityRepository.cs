namespace ATLAS.Core.Repositories;
using ATLAS.Core.Entities;

public interface IActivityRepository
{
    Task<ActivityRecord> CreateAsync(ActivityRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ActivityRecord>> GetRecentAsync(int minRelevance = 0, int count = 20, CancellationToken cancellationToken = default);
}
