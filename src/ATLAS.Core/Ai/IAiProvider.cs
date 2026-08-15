namespace ATLAS.Core.Ai;

/// <summary>
/// Core contract for AI providers in ATLAS.
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// Summarizes the given input text.
    /// </summary>
    Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a question/prompt to the AI model and returns the response.
    /// </summary>
    Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default);
}
