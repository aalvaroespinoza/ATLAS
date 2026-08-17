using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Commands;
using ATLAS.Core.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ATLAS.Core.Integrations.MercadoPago;

/// <summary>
/// Background service that periodically triggers the Mercado Pago sync process.
/// </summary>
public class MercadoPagoListenerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(15);

    public MercadoPagoListenerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a bit before first run to let the app finish starting up
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                
                // Only sync if configured
                var secretVault = scope.ServiceProvider.GetRequiredService<ISecretVault>();
                if (secretVault.HasSecret(SecretKeys.MercadoPagoAccessToken))
                {
                    var commandRegistry = scope.ServiceProvider.GetRequiredService<ICommandRegistry>();
                    await commandRegistry.ExecuteAsync(FinanceSyncMercadoPagoCommand.CommandId, null, stoppingToken).ConfigureAwait(false);
                }
            }
            catch
            {
                // Fallar silenciosamente sin interrumpir la UI (req del usuario)
            }

            try
            {
                await Task.Delay(_pollingInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }
}
