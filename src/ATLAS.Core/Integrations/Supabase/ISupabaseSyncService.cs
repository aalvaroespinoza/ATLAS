using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Core.Integrations.Supabase;

public record SupabaseSyncResult(
    bool IsSuccess,
    string Message,
    int NotesSynced = 0,
    int GoalsSynced = 0,
    int HabitsSynced = 0,
    int HabitEventsSynced = 0,
    int RoadmapsSynced = 0,
    int MilestonesSynced = 0,
    int TransactionsSynced = 0
);

/// <summary>
/// Service responsible for syncing local SQLite data to Supabase (PostgreSQL / PostgREST).
/// </summary>
public interface ISupabaseSyncService
{
    bool IsConfigured();
    Task<SupabaseSyncResult> SyncAllAsync(CancellationToken cancellationToken = default);
}
