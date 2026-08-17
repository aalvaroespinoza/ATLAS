namespace ATLAS.Core.Ai;

/// <summary>
/// Abstraction for specific AI providers (Gemini, Ollama, etc.) used by the Orchestrator.
/// </summary>
public interface IAiBackend
{
    AiProviderType Type { get; }
    string ProviderName { get; }
    
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default);
    Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default);
}
