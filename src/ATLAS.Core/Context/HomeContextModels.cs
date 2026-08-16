namespace ATLAS.Core.Context;

public record HomeMetrics(
    int MaxHabitStreak,
    string MaxStreakHabitName,
    IReadOnlyList<int> HabitStreakTrend,
    int FocusPercentage,
    int CompletedHabitsToday,
    int TotalHabitsCount,
    IReadOnlyList<int> FocusTrend,
    decimal NetBalanceThisMonth,
    decimal IncomeThisMonth,
    decimal ExpenseThisMonth,
    int MonthlyTransactionCount,
    IReadOnlyList<decimal> MonthlyExpenseTrend
);

public record HomeAgendaItem(
    string Id,
    string Title,
    string Subtitle,
    string Type, // "habit" | "milestone"
    string Meta,
    bool IsCompleted,
    string? HabitId = null,
    string? MilestoneId = null,
    string? RoadmapId = null
);

public record HomeHabitBadge(
    string Id,
    string Name,
    int StreakDays,
    bool IsCompletedToday,
    string IconType, // "shield" | "book" | "meditation" | "clock" | "target"
    string ColorVariant // "purple" | "emerald" | "cyan" | "orange"
);

public record HomeRoadmapProgress(
    string Id,
    string Title,
    int ProgressPercentage,
    int CompletedMilestones,
    int TotalMilestones,
    string? NextMilestoneTitle,
    string Color // "indigo" | "cyan" | "emerald" | "purple"
);

public record HomeActivityItem(
    string Id,
    string Title,
    string? Subtitle,
    DateTimeOffset Timestamp,
    string RelativeTime,
    string Type, // "note" | "transaction" | "habit" | "gmail"
    string Icon,
    string ColorVariant
);

public record HomeIntegrationsState(
    bool IsTelegramConfigured,
    bool IsTelegramActive,
    bool IsGmailConfigured,
    bool IsMercadoPagoConfigured,
    bool IsSupabaseConfigured
);

public record HomeContextData(
    HomeMetrics Metrics,
    IReadOnlyList<HomeAgendaItem> AgendaItems,
    IReadOnlyList<HomeHabitBadge> Habits,
    IReadOnlyList<HomeRoadmapProgress> Roadmaps,
    IReadOnlyList<HomeActivityItem> RecentActivity,
    HomeIntegrationsState Integrations
);
