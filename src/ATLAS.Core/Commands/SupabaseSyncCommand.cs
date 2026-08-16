using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Integrations.Supabase;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to trigger a manual synchronization of all local SQLite databases to Supabase.
/// </summary>
public class SupabaseSyncCommand : ICommand
{
    public const string CommandId = "supabase.sync";

    private readonly ISupabaseSyncService _syncService;

    public string Id => CommandId;
    public string Name => "Sincronizar con Supabase";
    public string Description => "Sincroniza notas, metas, hábitos, roadmaps y transacciones con la base de datos Supabase.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema => Array.Empty<CommandParameterDescriptor>();

    public SupabaseSyncCommand(ISupabaseSyncService syncService)
    {
        _syncService = syncService;
    }

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _syncService.SyncAllAsync(cancellationToken);

        if (result.IsSuccess)
        {
            var summary = $"{result.Message} ({result.NotesSynced} notas, {result.GoalsSynced} metas, {result.HabitsSynced} hábitos, {result.RoadmapsSynced} roadmaps, {result.TransactionsSynced} finanzas)";
            return CommandResult.Success(summary);
        }

        return CommandResult.Failure(result.Message);
    }
}
