namespace ATLAS.Core.Events;

/// <summary>
/// Immutable marker contract for all domain and operational events across ATLAS.
/// </summary>
public interface IAtlasEvent
{
    /// <summary>
    /// Unique identifier for this specific event occurrence.
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// Exact UTC timestamp when the event occurred.
    /// </summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// Operational origin of the event (e.g. "quick_capture", "telegram", "launcher", "home", "system").
    /// </summary>
    string Source { get; }
}
