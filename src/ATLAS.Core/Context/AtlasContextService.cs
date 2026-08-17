using System.Globalization;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;
using Microsoft.Extensions.DependencyInjection;
using ATLAS.Core.Integrations.Telegram;

namespace ATLAS.Core.Context;

/// <summary>
/// Universal context service composing transversal operational state from repositories.
/// Uses Task.WhenAll for parallel execution, maintaining local-first responsiveness.
/// </summary>
public class AtlasContextService : IAtlasContextService
{
    private static readonly CultureInfo ArCulture = new("es-AR");

    private readonly IHabitRepository _habitRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly IRoadmapRepository _roadmapRepository;
    private readonly INoteRepository _noteRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IActivityRepository _activityRepository;
    private readonly ISecretVault _secretVault;
    private readonly IServiceProvider _serviceProvider;

    public AtlasContextService(
        IHabitRepository habitRepository,
        IGoalRepository goalRepository,
        IRoadmapRepository roadmapRepository,
        INoteRepository noteRepository,
        ITransactionRepository transactionRepository,
        ISecretVault secretVault,
        IActivityRepository activityRepository,
        IServiceProvider serviceProvider)
    {
        _habitRepository = habitRepository ?? throw new ArgumentNullException(nameof(habitRepository));
        _goalRepository = goalRepository ?? throw new ArgumentNullException(nameof(goalRepository));
        _roadmapRepository = roadmapRepository ?? throw new ArgumentNullException(nameof(roadmapRepository));
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
        _activityRepository = activityRepository ?? throw new ArgumentNullException(nameof(activityRepository));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<AtlasContextSnapshot> GetCurrentContextAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var today = now.Date;

        // Concurrent load from all SQLite repositories
        var habitsTask = _habitRepository.GetAllAsync(cancellationToken);
        var habitEventsTask = _habitRepository.GetEventsAsync(since: now.AddDays(-30), cancellationToken: cancellationToken);
        var goalsTask = _goalRepository.GetAllAsync(null, cancellationToken);
        var roadmapsTask = _roadmapRepository.GetAllAsync(null, cancellationToken);
        var notesTask = _noteRepository.GetRecentAsync(20, cancellationToken);
        var transactionsTask = _transactionRepository.GetRecentAsync(500, cancellationToken);
        var activitiesTask = _activityRepository.GetRecentAsync(minRelevance: 2, count: 12, cancellationToken);

        await Task.WhenAll(habitsTask, habitEventsTask, goalsTask, roadmapsTask, notesTask, transactionsTask, activitiesTask).ConfigureAwait(false);

        var habits = await habitsTask.ConfigureAwait(false);
        var habitEvents = await habitEventsTask.ConfigureAwait(false);
        var goals = await goalsTask.ConfigureAwait(false);
        var roadmaps = await roadmapsTask.ConfigureAwait(false);
        var notes = await notesTask.ConfigureAwait(false);
        var transactions = await transactionsTask.ConfigureAwait(false);
        var activities = await activitiesTask.ConfigureAwait(false);

        // 1. Process Habits Summary
        var habitsSummary = ProcessHabits(habits, habitEvents, today);

        // 2. Process Roadmaps & Next Milestone
        var (roadmapSignals, nextMilestone) = ProcessRoadmaps(roadmaps);

        // 3. Process Goals in Focus
        var activeGoals = goals
            .Where(g => g.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(g => g.Progress)
            .Select(g =>
            {
                int? daysRemaining = g.TargetDate.HasValue ? (int)(g.TargetDate.Value.Date - today).TotalDays : null;
                return new AtlasGoalSignal(
                    Id: g.Id,
                    Title: g.Title,
                    Description: g.Description,
                    Status: g.Status,
                    Progress: g.Progress,
                    TargetDate: g.TargetDate,
                    DaysRemaining: daysRemaining
                );
            })
            .ToList();

        // 4. Process Finance Summary
        var financeSummary = ProcessFinance(transactions, today);

        // 5. Build Unified Activity Feed
        var activityFeed = activities.Select(a => new AtlasActivityItem(
            Id: a.Id,
            Type: a.Type,
            Title: a.Title,
            Subtitle: a.Summary,
            Timestamp: a.Timestamp,
            RelativeTime: FormatRelativeTime(a.Timestamp, now),
            Source: "atlas",
            Icon: GetActivityIcon(a.Type),
            ColorVariant: GetActivityColor(a.Type)
        )).ToList();

        // 6. Build Attention Signals
        var attentionSignals = BuildAttentionSignals(habitsSummary, nextMilestone, activeGoals);

        // 7. Check Integrations Status
        bool isTelegramRunning = false;
        try
        {
            var telegramListener = _serviceProvider.GetService<ITelegramListenerService>();
            if (telegramListener != null)
            {
                isTelegramRunning = telegramListener.IsRunning;
            }
        }
        catch
        {
            // Ignore if service is not available or disposed
        }

        var lastMpSync = transactions
            .Where(t => t.Origen.Equals("mercadopago", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => (DateTimeOffset?)t.CreatedAt)
            .FirstOrDefault();

        var integrations = new AtlasIntegrationsStatus(
            HasAi: _secretVault.HasSecret(SecretKeys.GeminiApiKey),
            HasTelegram: _secretVault.HasSecret(SecretKeys.TelegramBotToken),
            HasMercadoPago: _secretVault.HasSecret(SecretKeys.MercadoPagoAccessToken),
            HasGmail: _secretVault.HasSecret(SecretKeys.GmailClientId),
            HasSupabase: _secretVault.HasSecret(SecretKeys.SupabaseUrl),
            IsTelegramRunning: isTelegramRunning,
            LastMercadoPagoSync: lastMpSync
        );

        return new AtlasContextSnapshot(
            Timestamp: now,
            TimeOfDayGreeting: GetGreeting(now.ToLocalTime()),
            Habits: habitsSummary,
            NextMilestone: nextMilestone,
            GoalsInFocus: activeGoals,
            Roadmaps: roadmapSignals,
            Finance: financeSummary,
            RecentActivity: activityFeed,
            AttentionSignals: attentionSignals,
            Integrations: integrations
        );
    }

    public async Task<AtlasReducedContext> GetReducedContextAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await GetCurrentContextAsync(cancellationToken).ConfigureAwait(false);

        var topSignals = snapshot.AttentionSignals
            .Where(s => s.Priority == AtlasSignalPriority.High || s.Priority == AtlasSignalPriority.Medium)
            .Take(3)
            .ToList();

        return new AtlasReducedContext(
            Timestamp: snapshot.Timestamp,
            Greeting: snapshot.TimeOfDayGreeting,
            HabitsPendingCount: snapshot.Habits.PendingTodayCount,
            HabitsCompletedCount: snapshot.Habits.CompletedTodayCount,
            NextMilestoneTitle: snapshot.NextMilestone?.MilestoneTitle,
            NextMilestoneRoadmapTitle: snapshot.NextMilestone?.RoadmapTitle,
            MonthlyBalance: snapshot.Finance.NetBalance,
            PrioritySignals: topSignals
        );
    }

    public async Task<AtlasEntityContext> GetEntityContextAsync(object entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var now = DateTimeOffset.UtcNow;

        if (entity is Note note)
        {
            var tags = (note.Tags ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return new AtlasEntityContext(
                EntityType: "Note",
                EntityId: note.Id,
                Title: GetNoteTitle(note),
                Status: "saved",
                Tags: tags,
                RelatedActivity: [],
                Metadata: new Dictionary<string, object?>
                {
                    ["CreatedAt"] = note.CreatedAt,
                    ["Type"] = note.Type,
                    ["ContentLength"] = note.Content?.Length ?? 0
                }
            );
        }

        if (entity is Goal goal)
        {
            var roadmaps = await _roadmapRepository.GetAllAsync(null, cancellationToken).ConfigureAwait(false);
            var linkedRoadmaps = roadmaps.Where(r => r.GoalId == goal.Id).ToList();

            return new AtlasEntityContext(
                EntityType: "Goal",
                EntityId: goal.Id,
                Title: goal.Title,
                Status: goal.Status,
                Tags: [],
                RelatedActivity: [],
                Metadata: new Dictionary<string, object?>
                {
                    ["Progress"] = goal.Progress,
                    ["TargetDate"] = goal.TargetDate,
                    ["LinkedRoadmapsCount"] = linkedRoadmaps.Count
                }
            );
        }

        if (entity is Habit habit)
        {
            var events = await _habitRepository.GetEventsAsync(since: now.AddDays(-30), cancellationToken: cancellationToken).ConfigureAwait(false);
            var hEvents = events.Where(e => e.HabitId == habit.Id).OrderByDescending(e => e.CompletedAt).ToList();
            int streak = CalculateStreak(hEvents, now.Date);
            bool isCompletedToday = hEvents.Any(e => e.CompletedAt.Date == now.Date);

            return new AtlasEntityContext(
                EntityType: "Habit",
                EntityId: habit.Id,
                Title: habit.Name,
                Status: isCompletedToday ? "completed_today" : "pending_today",
                Tags: [habit.Frequency],
                RelatedActivity: [],
                Metadata: new Dictionary<string, object?>
                {
                    ["Frequency"] = habit.Frequency,
                    ["CurrentStreak"] = streak,
                    ["TotalEventsLast30Days"] = hEvents.Count,
                    ["IsCompletedToday"] = isCompletedToday
                }
            );
        }

        if (entity is Roadmap roadmap)
        {
            return new AtlasEntityContext(
                EntityType: "Roadmap",
                EntityId: roadmap.Id,
                Title: roadmap.Title,
                Status: $"{roadmap.ProgressPercentage}%",
                Tags: [],
                RelatedActivity: [],
                Metadata: new Dictionary<string, object?>
                {
                    ["ProgressPercentage"] = roadmap.ProgressPercentage,
                    ["MilestonesCount"] = roadmap.Milestones?.Count ?? 0,
                    ["CompletedMilestonesCount"] = roadmap.Milestones?.Count(m => m.Status.Equals("completed", StringComparison.OrdinalIgnoreCase)) ?? 0
                }
            );
        }

        if (entity is Transaction tx)
        {
            return new AtlasEntityContext(
                EntityType: "Transaction",
                EntityId: tx.Id,
                Title: tx.Descripcion,
                Status: tx.Tipo,
                Tags: tx.Categoria != null ? [tx.Categoria] : [],
                RelatedActivity: [],
                Metadata: new Dictionary<string, object?>
                {
                    ["Amount"] = tx.Monto,
                    ["Currency"] = tx.Moneda,
                    ["Date"] = tx.Fecha,
                    ["Source"] = tx.Origen
                }
            );
        }

        return new AtlasEntityContext(
            EntityType: entity.GetType().Name,
            EntityId: string.Empty,
            Title: entity.ToString() ?? string.Empty,
            Status: "unknown",
            Tags: [],
            RelatedActivity: [],
            Metadata: new Dictionary<string, object?>()
        );
    }

    public async Task<IReadOnlyList<AtlasAttentionSignal>> GetAttentionSignalsAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await GetCurrentContextAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.AttentionSignals;
    }

    public async Task<AtlasRoadmapSignal?> GetNextMilestoneAsync(CancellationToken cancellationToken = default)
    {
        var roadmaps = await _roadmapRepository.GetAllAsync(null, cancellationToken).ConfigureAwait(false);
        var (_, nextMilestone) = ProcessRoadmaps(roadmaps);
        return nextMilestone;
    }

    public async Task<AtlasHabitsSummary> GetHabitsSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var habits = await _habitRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var habitEvents = await _habitRepository.GetEventsAsync(since: now.AddDays(-30), cancellationToken: cancellationToken).ConfigureAwait(false);
        return ProcessHabits(habits, habitEvents, now.Date);
    }

    public async Task<AtlasFinanceSummary> GetFinanceSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var transactions = await _transactionRepository.GetRecentAsync(500, cancellationToken).ConfigureAwait(false);
        return ProcessFinance(transactions, now.Date);
    }

    public async Task<string> BuildAiSystemContextPromptAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await GetCurrentContextAsync(cancellationToken).ConfigureAwait(false);

        var pendingHabitsStr = snapshot.Habits.Items
            .Where(h => !h.IsCompletedToday)
            .Select(h => h.Name)
            .ToList();

        var goalsStr = snapshot.GoalsInFocus
            .Take(3)
            .Select(g => $"{g.Title} ({g.Progress}%)")
            .ToList();

        var text = $"[CONTEXTO OPERATIVO DEL USUARIO — {snapshot.Timestamp.ToLocalTime():dd/MM/yyyy HH:mm}]\n" +
                   $"• Saludo/Momento: {snapshot.TimeOfDayGreeting}\n" +
                   $"• Hábitos de Hoy: {snapshot.Habits.CompletedTodayCount}/{snapshot.Habits.TotalCount} completados. " +
                   (pendingHabitsStr.Count > 0 ? $"Pendientes: {string.Join(", ", pendingHabitsStr)}. " : "¡Todos los hábitos completados! ") +
                   (snapshot.Habits.LongestStreakDays > 0 ? $"Racha récord: {snapshot.Habits.LongestStreakDays} días en '{snapshot.Habits.LongestStreakHabitName}'.\n" : "\n") +
                   (snapshot.NextMilestone != null ? $"• Próximo Hito: Roadmap '{snapshot.NextMilestone.RoadmapTitle}' -> '{snapshot.NextMilestone.MilestoneTitle}' ({snapshot.NextMilestone.ProgressPercentage}% avance total).\n" : string.Empty) +
                   (goalsStr.Count > 0 ? $"• Metas en Foco: {string.Join(", ", goalsStr)}.\n" : string.Empty) +
                   $"• Finanzas del Mes: Ingresos ${snapshot.Finance.MonthlyIncome:N0}, Gastos ${snapshot.Finance.MonthlyExpenses:N0}, Balance Neto ${snapshot.Finance.NetBalance:N0} ARS.";

        return text;
    }

    // --- Internal Helpers ---

    private static AtlasHabitsSummary ProcessHabits(IReadOnlyList<Habit> habits, IReadOnlyList<HabitEvent> habitEvents, DateTime today)
    {
        var eventsByHabit = habitEvents
            .GroupBy(e => e.HabitId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CompletedAt).ToList());

        var weeklyTrend = new int[7];
        for (int i = 0; i < 7; i++)
        {
            var targetDay = today.AddDays(-6 + i);
            weeklyTrend[i] = habitEvents.Count(e => e.CompletedAt.Date == targetDay);
        }

        var items = new List<AtlasHabitItem>();
        int completedTodayCount = 0;
        int maxStreak = 0;
        string maxStreakHabitName = string.Empty;

        foreach (var habit in habits)
        {
            eventsByHabit.TryGetValue(habit.Id, out var hEvents);
            hEvents ??= [];

            bool isCompletedToday = hEvents.Any(e => e.CompletedAt.Date == today);
            if (isCompletedToday) completedTodayCount++;

            int streak = CalculateStreak(hEvents, today);
            if (streak > maxStreak)
            {
                maxStreak = streak;
                maxStreakHabitName = habit.Name;
            }

            var iconType = InferHabitIcon(habit.Name);
            var colorVariant = InferHabitColor(habit.Name, items.Count);
            var lastEvent = hEvents.FirstOrDefault();

            items.Add(new AtlasHabitItem(
                Id: habit.Id,
                Name: habit.Name,
                Frequency: habit.Frequency,
                CurrentStreak: streak,
                IsCompletedToday: isCompletedToday,
                IconType: iconType,
                ColorVariant: colorVariant,
                LastCompletedAt: lastEvent?.CompletedAt
            ));
        }

        return new AtlasHabitsSummary(
            TotalCount: habits.Count,
            CompletedTodayCount: completedTodayCount,
            PendingTodayCount: habits.Count - completedTodayCount,
            LongestStreakDays: maxStreak,
            LongestStreakHabitName: maxStreakHabitName,
            WeeklyCompletionTrend: weeklyTrend,
            Items: items
        );
    }

    private static (IReadOnlyList<AtlasRoadmapSignal> Signals, AtlasRoadmapSignal? NextMilestone) ProcessRoadmaps(IReadOnlyList<Roadmap> roadmaps)
    {
        var signals = new List<AtlasRoadmapSignal>();
        AtlasRoadmapSignal? topNextMilestone = null;

        var colors = new[] { "purple", "cyan", "emerald", "orange" };
        int colorIdx = 0;

        foreach (var r in roadmaps)
        {
            var milestones = r.Milestones?.OrderBy(m => m.OrderIndex).ToList() ?? [];
            var total = milestones.Count;
            var completed = milestones.Count(m => m.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));
            var next = milestones.FirstOrDefault(m => !m.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));

            var signal = new AtlasRoadmapSignal(
                RoadmapId: r.Id,
                RoadmapTitle: r.Title,
                MilestoneId: next?.Id,
                MilestoneTitle: next?.Title,
                MilestoneOrderIndex: next?.OrderIndex ?? -1,
                ProgressPercentage: r.ProgressPercentage,
                TotalMilestones: total,
                CompletedMilestones: completed,
                Color: colors[colorIdx % colors.Length]
            );

            signals.Add(signal);
            colorIdx++;

            if (topNextMilestone == null && next != null)
            {
                topNextMilestone = signal;
            }
        }

        return (signals, topNextMilestone);
    }

    private static AtlasFinanceSummary ProcessFinance(IReadOnlyList<Transaction> transactions, DateTime today)
    {
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        var thisMonthTransactions = transactions.Where(t => t.Fecha.Date >= firstDayOfMonth).ToList();

        decimal monthlyIncome = thisMonthTransactions
            .Where(t => t.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase))
            .Sum(t => t.Monto);

        decimal monthlyExpenses = thisMonthTransactions
            .Where(t => !t.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase))
            .Sum(t => t.Monto);

        var weeklyExpenseTrend = new decimal[7];
        for (int i = 0; i < 7; i++)
        {
            var targetDay = today.AddDays(-6 + i);
            weeklyExpenseTrend[i] = transactions
                .Where(t => t.Fecha.Date == targetDay && !t.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase))
                .Sum(t => t.Monto);
        }

        var recentMovements = transactions
            .OrderByDescending(t => t.Fecha)
            .Take(10)
            .Select(t => new AtlasFinanceMovement(
                Id: t.Id,
                Description: t.Descripcion,
                Amount: t.Monto,
                Type: t.Tipo,
                Category: t.Categoria,
                Source: t.Origen,
                Date: t.Fecha
            ))
            .ToList();

        return new AtlasFinanceSummary(
            MonthlyIncome: monthlyIncome,
            MonthlyExpenses: monthlyExpenses,
            NetBalance: monthlyIncome - monthlyExpenses,
            MovementCount: thisMonthTransactions.Count,
            WeeklyExpenseTrend: weeklyExpenseTrend,
            RecentMovements: recentMovements
        );
    }

    private static string GetActivityIcon(string type) => type.ToLowerInvariant() switch
    {
        "knowledge" => "📝",
        "finance" => "💳",
        "strategy" => "✓",
        "system" => "⚙️",
        "gaming" => "🎮",
        "education" => "📖",
        "security" => "🛡️",
        _ => "📌"
    };

    private static string GetActivityColor(string type) => type.ToLowerInvariant() switch
    {
        "knowledge" => "purple",
        "finance" => "emerald",
        "strategy" => "cyan",
        "system" => "orange",
        "gaming" => "purple",
        "education" => "cyan",
        "security" => "orange",
        _ => "purple"
    };

    private static IReadOnlyList<AtlasAttentionSignal> BuildAttentionSignals(
        AtlasHabitsSummary habits,
        AtlasRoadmapSignal? nextMilestone,
        IReadOnlyList<AtlasGoalSignal> activeGoals)
    {
        var signals = new List<AtlasAttentionSignal>();

        // High priority: Pending habits with active streak >= 3
        foreach (var pending in habits.Items.Where(h => !h.IsCompletedToday))
        {
            if (pending.CurrentStreak >= 3)
            {
                signals.Add(new AtlasAttentionSignal(
                    Id: $"habit-streak-{pending.Id}",
                    Priority: AtlasSignalPriority.High,
                    Title: $"Racha en riesgo: {pending.Name}",
                    Message: $"Llevás {pending.CurrentStreak} días consecutivos. Completalo hoy para mantener la racha.",
                    AssociatedCommandId: HabitCompleteCommand.CommandId,
                    CommandParameters: new Dictionary<string, object?> { ["id"] = pending.Id }
                ));
            }
            else
            {
                signals.Add(new AtlasAttentionSignal(
                    Id: $"habit-pending-{pending.Id}",
                    Priority: AtlasSignalPriority.Medium,
                    Title: $"Hábito pendiente: {pending.Name}",
                    Message: $"Frecuencia configurada: {pending.Frequency}.",
                    AssociatedCommandId: HabitCompleteCommand.CommandId,
                    CommandParameters: new Dictionary<string, object?> { ["id"] = pending.Id }
                ));
            }
        }

        // High priority: Next milestone
        if (nextMilestone != null && !string.IsNullOrWhiteSpace(nextMilestone.MilestoneTitle))
        {
            signals.Add(new AtlasAttentionSignal(
                Id: $"milestone-{nextMilestone.MilestoneId}",
                Priority: AtlasSignalPriority.High,
                Title: $"Próximo paso en {nextMilestone.RoadmapTitle}",
                Message: nextMilestone.MilestoneTitle,
                AssociatedCommandId: RoadmapCompleteMilestoneCommand.CommandId,
                CommandParameters: new Dictionary<string, object?>
                {
                    ["roadmap_id"] = nextMilestone.RoadmapId,
                    ["milestone_id"] = nextMilestone.MilestoneId
                }
            ));
        }

        // Medium priority: Goals nearing deadline
        foreach (var goal in activeGoals.Where(g => g.DaysRemaining.HasValue && g.DaysRemaining.Value <= 7 && g.Progress < 100))
        {
            signals.Add(new AtlasAttentionSignal(
                Id: $"goal-deadline-{goal.Id}",
                Priority: AtlasSignalPriority.High,
                Title: $"Meta próxima a vencer: {goal.Title}",
                Message: $"Vence en {goal.DaysRemaining} días con {goal.Progress}% de avance.",
                AssociatedCommandId: null,
                CommandParameters: null
            ));
        }

        return signals.OrderBy(s => s.Priority switch
        {
            AtlasSignalPriority.High => 0,
            AtlasSignalPriority.Medium => 1,
            _ => 2
        }).ToList();
    }

    private static int CalculateStreak(IReadOnlyList<HabitEvent> events, DateTime today)
    {
        if (events == null || events.Count == 0) return 0;

        var uniqueDays = events
            .Select(e => e.CompletedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        int streak = 0;
        var checkDay = uniqueDays.Contains(today) ? today : today.AddDays(-1);

        while (uniqueDays.Contains(checkDay))
        {
            streak++;
            checkDay = checkDay.AddDays(-1);
        }

        return streak;
    }

    private static string GetGreeting(DateTimeOffset localTime)
    {
        var hour = localTime.Hour;
        if (hour < 12) return "Buenos días";
        if (hour < 19) return "Buenas tardes";
        return "Buenas noches";
    }

    private static string GetNoteTitle(Note note)
    {
        if (!string.IsNullOrWhiteSpace(note.Title)) return note.Title;
        if (string.IsNullOrWhiteSpace(note.Content)) return "Nota sin título";
        var lines = note.Content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var first = lines.Length > 0 ? lines[0] : note.Content;
        if (first.Length > 45) return first[..45] + "...";
        return first;
    }

    private static string FormatRelativeTime(DateTimeOffset timestamp, DateTimeOffset now)
    {
        var diff = now - timestamp;
        if (diff.TotalMinutes < 2) return "ahora";
        if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"hace {(int)diff.TotalHours} h";
        if (diff.TotalDays < 7) return $"hace {(int)diff.TotalDays} d";
        return timestamp.ToLocalTime().ToString("dd MMM", ArCulture);
    }

    private static string InferHabitIcon(string name)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("lectura") || n.Contains("leer") || n.Contains("libro")) return "book";
        if (n.Contains("medita") || n.Contains("respirar") || n.Contains("paz")) return "meditation";
        if (n.Contains("madrugar") || n.Contains("despertar") || n.Contains("dormir") || n.Contains("hora")) return "clock";
        if (n.Contains("gym") || n.Contains("entrenar") || n.Contains("ejercicio") || n.Contains("salud")) return "shield";
        return "target";
    }

    private static string InferHabitColor(string name, int index)
    {
        var n = name.ToLowerInvariant();
        if (n.Contains("gym") || n.Contains("entrenar")) return "purple";
        if (n.Contains("lectura") || n.Contains("leer")) return "emerald";
        if (n.Contains("medita")) return "cyan";
        if (n.Contains("madrugar") || n.Contains("hora")) return "orange";

        var colors = new[] { "purple", "emerald", "cyan", "orange" };
        return colors[index % colors.Length];
    }
}
