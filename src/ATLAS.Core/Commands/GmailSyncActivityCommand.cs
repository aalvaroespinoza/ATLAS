using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Commands;
using ATLAS.Core.Integrations.Gmail;
using Microsoft.Extensions.DependencyInjection;

namespace ATLAS.Core.Commands;

public class GmailSyncActivityCommand : ICommand
{
    public const string CommandId = "ai.gmail.sync";
    
    private readonly IServiceProvider _serviceProvider;

    public GmailSyncActivityCommand(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public string Id => CommandId;

    public string Name => "Sincronizar Gmail";

    public string Description => "Extrae eventos recientes de Gmail (Steam, Hack4u, Alertas) y los añade a la actividad.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } = [];

    public async Task<CommandResult> ExecuteAsync(IReadOnlyDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var syncService = _serviceProvider.GetRequiredService<IGmailSyncService>();
            int ingested = await syncService.SyncRecentActivityAsync(cancellationToken).ConfigureAwait(false);
            
            return CommandResult.Success($"Sincronización de Gmail completada. Se ingresaron {ingested} nuevas actividades.");
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Falló la sincronización de Gmail: {ex.Message}");
        }
    }
}
