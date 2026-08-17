namespace ATLAS.Core.Integrations;

public enum IntegrationHealthStatus
{
    NotConfigured,
    AuthenticationRequired,
    Healthy,
    Degraded,
    Error
}

/// <summary>
/// Declarative capability flags for an integration.
/// </summary>
public record IntegrationCapabilities(
    bool CanIngest,
    bool CanSend,
    bool CanSync,
    bool RequiresPolling,
    bool SupportsOAuth
);

/// <summary>
/// Standardized diagnostic health report for an integration.
/// </summary>
public record IntegrationHealthReport(
    string IntegrationId,
    IntegrationHealthStatus Status,
    string Message,
    TimeSpan? Latency,
    DateTimeOffset CheckedAt
);

/// <summary>
/// Standard exception thrown by integration adapters when external service operations fail.
/// </summary>
public class IntegrationException : Exception
{
    public string IntegrationId { get; }

    public IntegrationException(string integrationId, string message, Exception? innerException = null)
        : base($"[{integrationId}] {message}", innerException)
    {
        IntegrationId = integrationId;
    }
}
