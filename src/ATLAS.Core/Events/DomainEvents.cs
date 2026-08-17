namespace ATLAS.Core.Events;

/// <summary>
/// Event emitted whenever a note or second brain item is captured.
/// </summary>
public record NoteCapturedEvent(
    string NoteId,
    string? Title,
    string Content,
    string? Tags,
    string Source,
    string EventId,
    DateTimeOffset OccurredAt
) : IAtlasEvent;

/// <summary>
/// Event emitted whenever a habit completion event is recorded.
/// </summary>
public record HabitCompletedEvent(
    string HabitId,
    string HabitName,
    string? Note,
    DateTimeOffset CompletedAt,
    string Source,
    string EventId,
    DateTimeOffset OccurredAt
) : IAtlasEvent;

/// <summary>
/// Event emitted whenever a new financial transaction (expense/income) is persisted.
/// </summary>
public record TransactionCreatedEvent(
    string TransactionId,
    string Description,
    decimal Amount,
    string Type, // "expense" | "income" | "transfer"
    string? Category,
    string Source,
    string EventId,
    DateTimeOffset OccurredAt
) : IAtlasEvent;

/// <summary>
/// Event emitted whenever a roadmap milestone is marked completed or uncompleted.
/// </summary>
public record RoadmapMilestoneCompletedEvent(
    string RoadmapId,
    string? RoadmapTitle,
    string MilestoneId,
    string MilestoneTitle,
    bool Completed,
    int NewRoadmapProgress,
    string Source,
    string EventId,
    DateTimeOffset OccurredAt
) : IAtlasEvent;
