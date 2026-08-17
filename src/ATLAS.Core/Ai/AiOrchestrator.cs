using Microsoft.Extensions.Logging;

namespace ATLAS.Core.Ai;

public class AiOrchestrator : IAiProvider
{
    private readonly IEnumerable<IAiBackend> _providers;
    private readonly ILogger<AiOrchestrator>? _logger;

    public AiOrchestrator(IEnumerable<IAiBackend> providers, ILogger<AiOrchestrator>? logger = null)
    {
        _providers = providers ?? Enumerable.Empty<IAiBackend>();
        _logger = logger;
    }

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(p => p.AskAsync(prompt, cancellationToken), cancellationToken);
    }

    public async Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithFallbackAsync(p => p.SummarizeAsync(text, cancellationToken), cancellationToken);
    }

    private async Task<string> ExecuteWithFallbackAsync(Func<IAiBackend, Task<string>> operation, CancellationToken cancellationToken)
    {
        // Enrutamiento base: intentar Cloud primero, luego Local como fallback.
        // Las opciones de privacidad se integrarán en la abstracción final del flujo de Request si es necesario sin afectar la interfaz base.
        var availableProviders = _providers.OrderBy(p => p.Type == AiProviderType.Local ? 1 : 0).ToList();

        var exceptions = new List<Exception>();

        foreach (var provider in availableProviders)
        {
            try
            {
                if (!await provider.IsAvailableAsync(cancellationToken))
                {
                    _logger?.LogWarning($"El proveedor IA '{provider.ProviderName}' no está disponible.");
                    continue;
                }

                // Timeouts implementados nativamente para evitar cuelgues del sistema
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(30));
                
                try 
                {
                    return await operation(provider);
                }
                catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"El proveedor '{provider.ProviderName}' excedió el tiempo límite.", ex);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                _logger?.LogError(ex, $"Fallo al ejecutar operación IA con el proveedor '{provider.ProviderName}'. Intentando fallback...");
                exceptions.Add(ex);
            }
        }

        if (exceptions.Any())
        {
            throw new AggregateException("Todos los proveedores IA fallaron o no estaban disponibles.", exceptions);
        }
        
        throw new InvalidOperationException("No se encontró ningún proveedor IA configurado o disponible.");
    }
}
