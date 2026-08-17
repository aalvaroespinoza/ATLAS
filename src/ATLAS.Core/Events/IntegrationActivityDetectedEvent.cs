using System;

namespace ATLAS.Core.Events;

/// <summary>
/// Event emitted when an integration (like Gmail) detects a relevant activity.
/// </summary>
public record IntegrationActivityDetectedEvent(
    string IntegrationId,
    string SourceId,
    string ActivityType, // e.g. "gaming", "security", "education"
    string Title,
    string Summary,
    int RelevanceScore,
    string Source,
    string EventId,
    DateTimeOffset OccurredAt
) : IAtlasEvent;
