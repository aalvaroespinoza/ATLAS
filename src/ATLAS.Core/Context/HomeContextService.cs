using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;

namespace ATLAS.Core.Context;

public class HomeContextService : IHomeContextService
{
    private readonly IHabitRepository _habitRepository;
    private readonly IRoadmapRepository _roadmapRepository;
    private readonly INoteRepository _noteRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ISecretVault _secretVault;

    public HomeContextService(
        IHabitRepository habitRepository,
        IRoadmapRepository roadmapRepository,
        INoteRepository noteRepository,
        ITransactionRepository transactionRepository,
        ISecretVault secretVault)
    {
        _habitRepository = habitRepository;
        _roadmapRepository = roadmapRepository;
        _noteRepository = noteRepository;
        _transactionRepository = transactionRepository;
        _secretVault = secretVault;
    }

    public async Task<HomeContextData> LoadHomeContextAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var today = now.Date;

        // Concurrent load from SQLite
        var habitsTask = _habitRepository.GetAllAsync(cancellationToken);
        var habitEventsTask = _habitRepository.GetEventsAsync(since: now.AddDays(-30), cancellationToken: cancellationToken);
        var roadmapsTask = _roadmapRepository.GetAllAsync(cancellationToken: cancellationToken);
        var notesTask = _noteRepository.GetRecentAsync(10, cancellationToken);
        var transactionsTask = _transactionRepository.GetRecentAsync(50, cancellationToken);

        await Task.WhenAll(habitsTask, habitEventsTask, roadmapsTask, notesTask, transactionsTask);

        var habits = await habitsTask;
        var habitEvents = await habitEventsTask;
        var roadmaps = await roadmapsTask;
        var notes = await notesTask;
        var transactions = await transactionsTask;

        // 1. Process Habits & Streaks
        var habitBadges = new List<HomeHabitBadge>();
        var agendaItems = new List<HomeAgendaItem>();
        int completedTodayCount = 0;
        int maxStreak = 0;
        string maxStreakHabitName = string.Empty;

        var eventsByHabit = habitEvents
            .GroupBy(e => e.HabitId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CompletedAt).ToList());

        var weeklyHabitTrend = new int[7];
        for (int i = 0; i < 7; i++)
        {
            var targetDay = today.AddDays(-6 + i);
            weeklyHabitTrend[i] = habitEvents.Count(e => e.CompletedAt.Date == targetDay);
        }

        foreach (var habit in habits)
        {
            eventsByHabit.TryGetValue(habit.Id, out var hEvents);
            hEvents ??= new List<HabitEvent>();

            bool isCompletedToday = hEvents.Any(e => e.CompletedAt.Date == today);
            if (isCompletedToday) completedTodayCount++;

            int streak = CalculateStreak(hEvents, today);
            if (streak > maxStreak)
            {
                maxStreak = streak;
                maxStreakHabitName = habit.Name;
            }

            var iconType = InferHabitIcon(habit.Name);
            var colorVariant = InferHabitColor(habit.Name, habitBadges.Count);

            habitBadges.Add(new HomeHabitBadge(
                habit.Id,
                habit.Name,
                streak,
                isCompletedToday,
                iconType,
                colorVariant
            ));

            if (!isCompletedToday)
            {
                agendaItems.Add(new HomeAgendaItem(
                    Id: $"habit-{habit.Id}",
                    Title: habit.Name,
                    Subtitle: $"Hábito ({habit.Frequency})",
                    Type: "habit",
                    Meta: "HOY",
                    IsCompleted: false,
                    HabitId: habit.Id
                ));
            }
        }

        // 2. Process Roadmaps & Milestones
        var roadmapProgressList = new List<HomeRoadmapProgress>();
        var activeRoadmaps = roadmaps.Where(r => r.Status != "archived").ToList();

        string[] colorPalette = ["indigo", "cyan", "emerald", "purple"];
        int colorIdx = 0;

        foreach (var rm in activeRoadmaps)
        {
            int total = rm.Milestones.Count;
            int completed = rm.Milestones.Count(m => m.Status == "completed");
            int pct = total > 0 ? (completed * 100) / total : 0;

            var nextPending = rm.Milestones
                .Where(m => m.Status != "completed")
                .OrderBy(m => m.OrderIndex)
                .FirstOrDefault();

            var color = colorPalette[colorIdx % colorPalette.Length];
            colorIdx++;

            roadmapProgressList.Add(new HomeRoadmapProgress(
                rm.Id,
                rm.Title,
                pct,
                completed,
                total,
                nextPending?.Title,
                color
            ));

            if (nextPending != null && agendaItems.Count < 5)
            {
                agendaItems.Add(new HomeAgendaItem(
                    Id: $"milestone-{nextPending.Id}",
                    Title: nextPending.Title,
                    Subtitle: $"Hito en {rm.Title}",
                    Type: "milestone",
                    Meta: "EN PROGRESO",
                    IsCompleted: false,
                    MilestoneId: nextPending.Id,
                    RoadmapId: rm.Id
                ));
            }
        }

        // 3. Process Transactions & Month Net Balance
        var thisMonthTransactions = transactions
            .Where(t => t.Fecha.Year == now.Year && t.Fecha.Month == now.Month)
            .ToList();

        decimal incomeMonth = thisMonthTransactions.Where(t => t.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase)).Sum(t => t.Monto);
        decimal expenseMonth = thisMonthTransactions.Where(t => t.Tipo.Equals("expense", StringComparison.OrdinalIgnoreCase)).Sum(t => t.Monto);
        decimal netBalanceMonth = incomeMonth - expenseMonth;

        var monthlyExpenseTrend = new decimal[7];
        for (int i = 0; i < 7; i++)
        {
            var targetDay = today.AddDays(-6 + i);
            monthlyExpenseTrend[i] = thisMonthTransactions
                .Where(t => t.Tipo.Equals("expense", StringComparison.OrdinalIgnoreCase) && t.Fecha.Date == targetDay)
                .Sum(t => t.Monto);
        }

        // 4. Calculate Focus & Metrics
        int totalHabitsCount = habits.Count;
        int focusPercentage = totalHabitsCount > 0 ? (completedTodayCount * 100) / totalHabitsCount : 100;

        var focusTrend = new int[7];
        for (int i = 0; i < 7; i++)
        {
            focusTrend[i] = totalHabitsCount > 0 ? Math.Min(100, (weeklyHabitTrend[i] * 100) / totalHabitsCount) : 100;
        }

        var metrics = new HomeMetrics(
            MaxHabitStreak: maxStreak,
            MaxStreakHabitName: maxStreakHabitName,
            HabitStreakTrend: weeklyHabitTrend,
            FocusPercentage: focusPercentage,
            CompletedHabitsToday: completedTodayCount,
            TotalHabitsCount: totalHabitsCount,
            FocusTrend: focusTrend,
            NetBalanceThisMonth: netBalanceMonth,
            IncomeThisMonth: incomeMonth,
            ExpenseThisMonth: expenseMonth,
            MonthlyTransactionCount: thisMonthTransactions.Count,
            MonthlyExpenseTrend: monthlyExpenseTrend
        );

        // 5. Build Unified Real Activity Feed
        var activityFeed = new List<HomeActivityItem>();

        foreach (var note in notes.Take(4))
        {
            activityFeed.Add(new HomeActivityItem(
                Id: $"note-{note.Id}",
                Title: $"Capturaste una nueva nota: {note.Title}",
                Subtitle: note.Tags != null ? string.Join(" ", note.Tags) : null,
                Timestamp: note.CreatedAt,
                RelativeTime: FormatRelativeTime(note.CreatedAt, now),
                Type: "note",
                Icon: "📝",
                ColorVariant: "purple"
            ));
        }

        foreach (var tx in transactions.Take(4))
        {
            bool isExpense = tx.Tipo.Equals("expense", StringComparison.OrdinalIgnoreCase);
            activityFeed.Add(new HomeActivityItem(
                Id: $"tx-{tx.Id}",
                Title: $"{(isExpense ? "Gasto" : "Ingreso")} registrado: ${tx.Monto:N0} - {tx.Descripcion}",
                Subtitle: tx.Categoria ?? tx.Subcategoria ?? tx.Origen,
                Timestamp: tx.Fecha,
                RelativeTime: FormatRelativeTime(tx.Fecha, now),
                Type: "transaction",
                Icon: isExpense ? "💳" : "💰",
                ColorVariant: isExpense ? "rose" : "emerald"
            ));
        }

        foreach (var ev in habitEvents.Take(4))
        {
            var h = habits.FirstOrDefault(x => x.Id == ev.HabitId);
            if (h != null)
            {
                activityFeed.Add(new HomeActivityItem(
                    Id: $"ev-{ev.Id}",
                    Title: $"Completaste el hábito: {h.Name}",
                    Subtitle: null,
                    Timestamp: ev.CompletedAt,
                    RelativeTime: FormatRelativeTime(ev.CompletedAt, now),
                    Type: "habit",
                    Icon: "✓",
                    ColorVariant: "emerald"
                ));
            }
        }

        var orderedActivity = activityFeed
            .OrderByDescending(a => a.Timestamp)
            .Take(5)
            .ToList();

        // 6. Check Integrations Status
        var integrations = new HomeIntegrationsState(
            IsTelegramConfigured: _secretVault.HasSecret(SecretKeys.TelegramBotToken),
            IsTelegramActive: _secretVault.HasSecret(SecretKeys.TelegramBotToken),
            IsGmailConfigured: _secretVault.HasSecret(SecretKeys.GmailClientId),
            IsMercadoPagoConfigured: _secretVault.HasSecret(SecretKeys.MercadoPagoAccessToken),
            IsSupabaseConfigured: _secretVault.HasSecret(SecretKeys.SupabaseUrl)
        );

        return new HomeContextData(
            metrics,
            agendaItems,
            habitBadges,
            roadmapProgressList,
            orderedActivity,
            integrations
        );
    }

    private static int CalculateStreak(IReadOnlyList<HabitEvent> events, DateTime today)
    {
        if (events.Count == 0) return 0;

        var completedDates = events
            .Select(e => e.CompletedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int streak = 0;
        var checkDate = today;

        // If not completed today, allow checking from yesterday
        if (!completedDates.Contains(checkDate))
        {
            checkDate = today.AddDays(-1);
            if (!completedDates.Contains(checkDate))
            {
                return 0;
            }
        }

        while (completedDates.Contains(checkDate))
        {
            streak++;
            checkDate = checkDate.AddDays(-1);
        }

        return streak;
    }

    private static string InferHabitIcon(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("entren") || lower.Contains("gym") || lower.Contains("ejercic") || lower.Contains("fuerza")) return "shield";
        if (lower.Contains("leer") || lower.Contains("libro") || lower.Contains("estudi") || lower.Contains("read")) return "book";
        if (lower.Contains("medit") || lower.Contains("respir") || lower.Contains("paz") || lower.Contains("calma")) return "meditation";
        if (lower.Contains("dorm") || lower.Contains("sleep") || lower.Contains("tempran") || lower.Contains("hora")) return "clock";
        return "target";
    }

    private static string InferHabitColor(string name, int index)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("entren") || lower.Contains("gym")) return "purple";
        if (lower.Contains("leer") || lower.Contains("estudi")) return "emerald";
        if (lower.Contains("medit")) return "cyan";
        if (lower.Contains("dorm")) return "orange";

        string[] palette = ["purple", "emerald", "cyan", "orange"];
        return palette[index % palette.Length];
    }

    private static string FormatRelativeTime(DateTimeOffset timestamp, DateTimeOffset now)
    {
        var diff = now - timestamp;
        if (diff.TotalMinutes < 1) return "ahora";
        if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes}m";
        if (diff.TotalHours < 24) return $"hace {(int)diff.TotalHours}h";
        if (diff.TotalDays < 2) return "ayer";
        return $"hace {(int)diff.TotalDays}d";
    }
}
