using System.Collections.ObjectModel;
using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace ATLAS.UI.ViewModels;

public sealed class HabitTodayItemViewModel
{
    public Habit Habit { get; }
    public bool IsCompleted { get; }
    public string StatusSymbol => IsCompleted ? "[✓]" : "[ ]";
    public string Name => Habit.Name;
    public string FrequencyText => Habit.Frequency;
    public string CompletionDetail { get; }

    public HabitTodayItemViewModel(Habit habit, HabitEvent? latestEventToday)
    {
        Habit = habit ?? throw new ArgumentNullException(nameof(habit));
        IsCompleted = latestEventToday != null;

        if (latestEventToday != null)
        {
            var localTime = latestEventToday.CompletedAt.ToLocalTime();
            var noteSuffix = !string.IsNullOrWhiteSpace(latestEventToday.Note) ? $" — {latestEventToday.Note.Trim()}" : "";
            CompletionDetail = $"Completado a las {localTime:HH:mm}{noteSuffix}";
        }
        else
        {
            CompletionDetail = "Pendiente para hoy";
        }
    }
}

public sealed class GoalActiveItemViewModel
{
    public Goal Goal { get; }
    public string Title => Goal.Title;
    public int Progress => Math.Clamp(Goal.Progress, 0, 100);
    public string ProgressText => $"[{Progress}%]";
    public string ProgressBarAscii => GenerateAsciiBar(Progress);
    public string? TargetDateText { get; }
    public Visibility TargetDateVisibility => !string.IsNullOrWhiteSpace(TargetDateText) ? Visibility.Visible : Visibility.Collapsed;

    public GoalActiveItemViewModel(Goal goal)
    {
        Goal = goal ?? throw new ArgumentNullException(nameof(goal));
        if (goal.TargetDate.HasValue)
        {
            TargetDateText = $"Objetivo: {goal.TargetDate.Value.ToLocalTime():dd MMM yyyy}";
        }
    }

    private static string GenerateAsciiBar(int percent)
    {
        const int totalBlocks = 15;
        var filled = (int)Math.Round((percent / 100.0) * totalBlocks);
        filled = Math.Clamp(filled, 0, totalBlocks);
        var empty = totalBlocks - filled;
        return new string('█', filled) + new string('░', empty);
    }
}

public sealed class RecentActivityItemViewModel
{
    public string Title { get; }
    public string RelativeTime { get; }
    public string Kind { get; }

    public RecentActivityItemViewModel(string title, DateTimeOffset timestamp, string kind)
    {
        Title = title;
        Kind = kind;
        RelativeTime = FormatRelative(timestamp);
    }

    private static string FormatRelative(DateTimeOffset dto)
    {
        var dt = dto.ToLocalTime();
        var now = DateTimeOffset.Now;
        var diff = now - dt;

        if (diff.TotalMinutes < 1) return "hace instantes";
        if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes}m";
        if (diff.TotalHours < 24 && dt.Date == now.Date) return $"hoy {dt:HH:mm}";
        if (diff.TotalDays < 2 && dt.Date == now.Date.AddDays(-1)) return $"ayer {dt:HH:mm}";

        return dt.ToString("dd MMM", CultureInfo.CurrentCulture);
    }
}

public partial class HomeViewModel : ObservableObject
{
    private readonly IHabitRepository _habitRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly INoteRepository _noteRepository;

    [ObservableProperty]
    public partial string Greeting { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string HabitsSummaryText { get; set; } = "Cargando hábitos...";

    [ObservableProperty]
    public partial string GoalsSummaryText { get; set; } = "Cargando metas...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoHabitsVisibility))]
    public partial bool HasHabitsToday { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoGoalsVisibility))]
    public partial bool HasActiveGoals { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoRecentActivityVisibility))]
    public partial bool HasRecentActivity { get; set; }

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    public Visibility NoHabitsVisibility => !HasHabitsToday ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NoGoalsVisibility => !HasActiveGoals ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NoRecentActivityVisibility => !HasRecentActivity ? Visibility.Visible : Visibility.Collapsed;

    public ObservableCollection<HabitTodayItemViewModel> HabitsToday { get; } = [];
    public ObservableCollection<GoalActiveItemViewModel> ActiveGoals { get; } = [];
    public ObservableCollection<RecentActivityItemViewModel> RecentActivity { get; } = [];

    public event Action? OpenLauncherRequested;
    public event Action? OpenActivityRequested;
    public event Action? OpenSettingsRequested;

    public HomeViewModel(
        IHabitRepository habitRepository,
        IGoalRepository goalRepository,
        INoteRepository noteRepository)
    {
        _habitRepository = habitRepository ?? throw new ArgumentNullException(nameof(habitRepository));
        _goalRepository = goalRepository ?? throw new ArgumentNullException(nameof(goalRepository));
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));

        UpdateGreeting();
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        if (hour < 12)
        {
            Greeting = "Buenos días, Álvaro";
        }
        else if (hour < 20)
        {
            Greeting = "Buenas tardes, Álvaro";
        }
        else
        {
            Greeting = "Buenas noches, Álvaro";
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        IsLoading = true;
        UpdateGreeting();

        try
        {
            // 1. Cargar hábitos y eventos de hoy
            var todayUtc = DateTimeOffset.UtcNow.Date;
            var todayStart = new DateTimeOffset(todayUtc, TimeSpan.Zero);

            var habits = await _habitRepository.GetAllAsync();
            var todayEvents = await _habitRepository.GetEventsAsync(since: todayStart);

            var eventsByHabit = todayEvents
                .GroupBy(e => e.HabitId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CompletedAt).First());

            HabitsToday.Clear();
            var completedCount = 0;
            var dueTodayHabits = habits.Where(h => IsDueToday(h.Frequency, DateTime.Now)).ToList();

            foreach (var habit in dueTodayHabits)
            {
                eventsByHabit.TryGetValue(habit.Id, out var latestEvent);
                var item = new HabitTodayItemViewModel(habit, latestEvent);
                if (item.IsCompleted) completedCount++;
                HabitsToday.Add(item);
            }

            HasHabitsToday = HabitsToday.Count > 0;
            HabitsSummaryText = HasHabitsToday
                ? $"{completedCount} de {HabitsToday.Count} completados hoy"
                : "No hay hábitos programados para hoy";

            // 2. Cargar metas activas
            var goals = await _goalRepository.GetAllAsync("active");
            ActiveGoals.Clear();
            foreach (var goal in goals)
            {
                ActiveGoals.Add(new GoalActiveItemViewModel(goal));
            }

            HasActiveGoals = ActiveGoals.Count > 0;
            GoalsSummaryText = HasActiveGoals
                ? $"{ActiveGoals.Count} metas en curso"
                : "No tenés metas activas actualmente";

            // 3. Cargar actividad reciente (notas y hábitos)
            RecentActivity.Clear();
            var recentNotes = await _noteRepository.GetRecentAsync(5);
            var combinedActivities = new List<(string Title, DateTimeOffset Timestamp, string Kind)>();

            foreach (var note in recentNotes)
            {
                var title = !string.IsNullOrWhiteSpace(note.Title) ? note.Title : (note.Content.Length > 40 ? note.Content[..40] + "..." : note.Content);
                combinedActivities.Add((title.Trim(), note.CreatedAt, "Nota"));
            }

            foreach (var ev in todayEvents.Take(4))
            {
                var habitName = habits.FirstOrDefault(h => h.Id == ev.HabitId)?.Name ?? "Hábito";
                combinedActivities.Add(($"Hábito completado: {habitName}", ev.CompletedAt, "Hábito"));
            }

            var sortedActivities = combinedActivities.OrderByDescending(a => a.Timestamp).Take(5);
            foreach (var act in sortedActivities)
            {
                RecentActivity.Add(new RecentActivityItemViewModel(act.Title, act.Timestamp, act.Kind));
            }

            HasRecentActivity = RecentActivity.Count > 0;
        }
        catch
        {
            // Error handling silencioso
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void OpenLauncher()
    {
        OpenLauncherRequested?.Invoke();
    }

    [RelayCommand]
    public void OpenActivity()
    {
        OpenActivityRequested?.Invoke();
    }

    [RelayCommand]
    public void OpenSettings()
    {
        OpenSettingsRequested?.Invoke();
    }

    private static bool IsDueToday(string frequency, DateTime localDate)
    {
        if (string.IsNullOrWhiteSpace(frequency)) return true;
        var trimmed = frequency.Trim().ToLowerInvariant();

        if (trimmed == "daily") return true;

        if (trimmed.StartsWith("weekly")) return true;

        if (trimmed.StartsWith("days:"))
        {
            var daysPart = trimmed[5..];
            var isoDay = ((int)localDate.DayOfWeek == 0) ? 7 : (int)localDate.DayOfWeek; // 1=Mon .. 7=Sun
            var allowedDays = daysPart.Split(',', StringSplitOptions.TrimEntries)
                                      .Select(s => int.TryParse(s, out var d) ? d : -1);
            return allowedDays.Contains(isoDay);
        }

        return true;
    }
}
