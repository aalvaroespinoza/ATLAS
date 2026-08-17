using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Core.Integrations.Gmail;

/// <summary>
/// Service responsible for fetching recent emails and transforming them into ATLAS Activity entries.
/// </summary>
public interface IGmailSyncService
{
    /// <summary>
    /// Scans the latest emails and generates new activity records in the local repository.
    /// Returns the number of new activities ingested.
    /// </summary>
    Task<int> SyncRecentActivityAsync(CancellationToken cancellationToken = default);
}
