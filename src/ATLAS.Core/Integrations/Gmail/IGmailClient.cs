using ATLAS.Core.Entities;

namespace ATLAS.Core.Integrations.Gmail;

/// <summary>
/// Client interface for interacting with Gmail API (read-only triage/capture).
/// </summary>
public interface IGmailClient
{
    /// <summary>
    /// Exchanges an OAuth authorization code for tokens and stores the refresh token in the secure vault.
    /// </summary>
    Task<string> ExchangeCodeAndSaveTokensAsync(string code, string redirectUri, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves recent email messages without modifying labels or marking them as read.
    /// </summary>
    Task<IReadOnlyList<GmailMessageSummary>> ListRecentMessagesAsync(int limit = 10, string? query = null, CancellationToken cancellationToken = default);
}
