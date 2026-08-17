namespace ATLAS.Core.Context;

public enum AtlasSignalPriority
{
    Low,
    Medium,
    High
}

public record AtlasIntegrationsStatus(
    bool HasAi,
    bool HasTelegram,
    bool HasMercadoPago,
    bool HasGmail,
    bool HasSupabase
);

public record AtlasHabitItem(
    string Id,
    string Name,
    string Frequency,
    int CurrentStreak,
    bool IsCompletedToday,
    string IconType, // "shield", "book", "meditation", "clock", "target"
    string ColorVariant, // "purple", "emerald", "cyan", "orange"
    DateTimeOffset? LastCompletedAt
);

public record AtlasHabitsSummary(
    int TotalCount,
    int CompletedTodayCount,
    int PendingTodayCount,
    int LongestStreakDays,
    string LongestStreakHabitName,
    IReadOnlyList<int> WeeklyCompletionTrend,
    IReadOnlyList<AtlasHabitItem> Items
);

public record AtlasRoadmapSignal(
    string RoadmapId,
    string RoadmapTitle,
    string? MilestoneId,
    string? MilestoneTitle,
    int MilestoneOrderIndex,
    int ProgressPercentage,
    int TotalMilestones,
    int CompletedMilestones,
    string Color // "purple", "cyan", "emerald", "orange"
);

public record AtlasGoalSignal(
    string Id,
    string Title,
    string? Description,
    string Status,
    int Progress,
    DateTimeOffset? TargetDate,
    int? DaysRemaining
);

public record AtlasFinanceMovement(
    string Id,
    string Description,
    decimal Amount,
    string Type, // "income" | "expense"
    string? Category,
    string Source,
    DateTimeOffset Date
);

public record AtlasFinanceSummary(
    decimal MonthlyIncome,
    decimal MonthlyExpenses,
    decimal NetBalance,
    int MovementCount,
    IReadOnlyList<decimal> WeeklyExpenseTrend,
    IReadOnlyList<AtlasFinanceMovement> RecentMovements
);

public record AtlasActivityItem(
    string Id,
    string Type, // "note" | "transaction" | "habit" | "milestone" | "gmail"
    string Title,
    string? Subtitle,
    DateTimeOffset Timestamp,
    string RelativeTime,
    string Source,
    string Icon,
    string ColorVariant
);

public record AtlasAttentionSignal(
    string Id,
    AtlasSignalPriority Priority,
    string Title,
    string Message,
    string? AssociatedCommandId,
    IReadOnlyDictionary<string, object?>? CommandParameters
);

public record AtlasContextSnapshot(
    DateTimeOffset Timestamp,
    string TimeOfDayGreeting,
    AtlasHabitsSummary Habits,
    AtlasRoadmapSignal? NextMilestone,
    IReadOnlyList<AtlasGoalSignal> GoalsInFocus,
    IReadOnlyList<AtlasRoadmapSignal> Roadmaps,
    AtlasFinanceSummary Finance,
    IReadOnlyList<AtlasActivityItem> RecentActivity,
    IReadOnlyList<AtlasAttentionSignal> AttentionSignals,
    AtlasIntegrationsStatus Integrations
);

public record AtlasReducedContext(
    DateTimeOffset Timestamp,
    string Greeting,
    int HabitsPendingCount,
    int HabitsCompletedCount,
    string? NextMilestoneTitle,
    string? NextMilestoneRoadmapTitle,
    decimal MonthlyBalance,
    IReadOnlyList<AtlasAttentionSignal> PrioritySignals
);

public record AtlasEntityContext(
    string EntityType,
    string EntityId,
    string Title,
    string Status,
    IReadOnlyList<string> Tags,
    IReadOnlyList<AtlasActivityItem> RelatedActivity,
    IReadOnlyDictionary<string, object?> Metadata
);
