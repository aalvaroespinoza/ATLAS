using System.Collections.ObjectModel;
using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace ATLAS.UI.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private static readonly CultureInfo ArCulture = new("es-AR");

    private readonly IHabitRepository _habitRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly INoteRepository _noteRepository;
    private readonly ITransactionRepository _transactionRepository;

    // --- Dashboard 5b Properties ---

    // Card 1: Hábitos
    [ObservableProperty]
    public partial string HabitsProgressText { get; set; } = "0 de 0 completados";

    [ObservableProperty]
    public partial int HabitsProgressPercent { get; set; }

    [ObservableProperty]
    public partial string NextPendingHabitText { get; set; } = "Cargando hábitos...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoHabitsVisibility))]
    public partial bool HasHabits { get; set; }

    public Visibility HasNoHabitsVisibility => HasHabits ? Visibility.Collapsed : Visibility.Visible;

    // Card 2: Metas
    public ObservableCollection<GoalActiveItemViewModel> ActiveGoals { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoGoalsVisibility))]
    public partial bool HasGoals { get; set; }

    public Visibility HasNoGoalsVisibility => HasGoals ? Visibility.Collapsed : Visibility.Visible;

    // Card 3: Finanzas & Gráfico Nativo
    [ObservableProperty]
    public partial string NetBalanceText { get; set; } = "$0";

    [ObservableProperty]
    public partial string FinanceDetailsText { get; set; } = "Gastos: $0 • Ingresos: $0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoTransactionsVisibility))]
    public partial bool HasTransactions { get; set; }

    public Visibility HasNoTransactionsVisibility => HasTransactions ? Visibility.Collapsed : Visibility.Visible;

    public ObservableCollection<DailyFinanceBarItem> FinanceDailyBars { get; } = [];

    // Card 4: Second Brain (Capturas recientes)
    public ObservableCollection<NoteItemViewModel> RecentNotes { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoNotesVisibility))]
    public partial bool HasNotes { get; set; }

    public Visibility HasNoNotesVisibility => HasNotes ? Visibility.Collapsed : Visibility.Visible;

    // State & Navigation
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Greeting))]
    public partial string GreetingText { get; set; } = "Hola";

    public string Greeting => GreetingText;

    // Legacy HomeWindow Bindings Compatibility
    [ObservableProperty]
    public partial string HabitsSummaryText { get; set; } = "Cargando hábitos...";

    [ObservableProperty]
    public partial string GoalsSummaryText { get; set; } = "Cargando metas...";

    public ObservableCollection<HabitTodayItemViewModel> HabitsToday { get; } = [];
    public ObservableCollection<RecentActivityItemViewModel> RecentActivity { get; } = [];

    public Visibility NoHabitsVisibility => HasNoHabitsVisibility;
    public Visibility NoGoalsVisibility => HasNoGoalsVisibility;
    public Visibility NoRecentActivityVisibility => HasNoNotesVisibility;

    [RelayCommand]
    public async Task RefreshAsync() => await LoadDashboardAsync();

    [RelayCommand]
    public void OpenLauncher() => OpenLauncherRequested?.Invoke();

    [RelayCommand]
    public void OpenActivity() => OpenActivityRequested?.Invoke();

    [RelayCommand]
    public void OpenSettings() => OpenSettingsRequested?.Invoke();

    public event Action? OpenLauncherRequested;
    public event Action? OpenActivityRequested;
    public event Action? OpenSettingsRequested;
    public event Action<string>? NavigateRequested;

    public HomeViewModel(
        IHabitRepository habitRepository,
        IGoalRepository goalRepository,
        INoteRepository noteRepository,
        ITransactionRepository transactionRepository)
    {
        _habitRepository = habitRepository ?? throw new ArgumentNullException(nameof(habitRepository));
        _goalRepository = goalRepository ?? throw new ArgumentNullException(nameof(goalRepository));
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));

        UpdateGreeting();
    }

    private void UpdateGreeting()
    {
        var hour = DateTime.Now.Hour;
        GreetingText = hour switch
        {
            < 12 => "Buenos días",
            < 20 => "Buenas tardes",
            _ => "Buenas noches"
        };
    }

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        if (IsLoading) return;
        IsLoading = true;

        UpdateGreeting();

        try
        {
            await Task.WhenAll(
                LoadHabitsCardAsync(),
                LoadGoalsCardAsync(),
                LoadFinanceCardAsync(),
                LoadNotesCardAsync());
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadHabitsCardAsync()
    {
        try
        {
            var habits = await _habitRepository.GetAllAsync();
            var todayStart = DateTimeOffset.UtcNow.Date;
            var todayEvents = await _habitRepository.GetEventsAsync(since: todayStart);

            var latestEventsByHabit = todayEvents
                .GroupBy(e => e.HabitId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CompletedAt).FirstOrDefault());

            var completedHabitIds = todayEvents.Select(e => e.HabitId).ToHashSet();
            var total = habits.Count;
            var completed = habits.Count(h => completedHabitIds.Contains(h.Id));

            HasHabits = total > 0;

            HabitsToday.Clear();
            foreach (var h in habits)
            {
                latestEventsByHabit.TryGetValue(h.Id, out var latestEvent);
                HabitsToday.Add(new HabitTodayItemViewModel(h, latestEvent));
            }

            if (total > 0)
            {
                HabitsProgressPercent = (int)Math.Round((double)completed / total * 100);
                HabitsProgressText = $"{completed} de {total} completados ({HabitsProgressPercent}%)";
                HabitsSummaryText = HabitsProgressText;

                var nextPending = habits.FirstOrDefault(h => !completedHabitIds.Contains(h.Id));
                NextPendingHabitText = nextPending != null
                    ? $"Próximo pendiente: {nextPending.Name}"
                    : "✓ ¡Completaste todos tus hábitos de hoy!";
            }
            else
            {
                HabitsProgressPercent = 0;
                HabitsProgressText = "Sin hábitos registrados";
                HabitsSummaryText = HabitsProgressText;
                NextPendingHabitText = "Creá hábitos diarios para empezar a medir tu constancia.";
            }
        }
        catch (Exception ex)
        {
            NextPendingHabitText = $"Error: {ex.Message}";
        }
    }

    private async Task LoadGoalsCardAsync()
    {
        try
        {
            var goals = await _goalRepository.GetAllAsync(status: "active");
            ActiveGoals.Clear();

            foreach (var g in goals.Take(3))
            {
                ActiveGoals.Add(new GoalActiveItemViewModel(g));
            }

            HasGoals = ActiveGoals.Count > 0;
            GoalsSummaryText = HasGoals ? $"{ActiveGoals.Count} metas en curso" : "Sin metas activas";
        }
        catch
        {
            HasGoals = false;
            GoalsSummaryText = "Error al cargar metas";
        }
    }

    private async Task LoadFinanceCardAsync()
    {
        try
        {
            var recent = await _transactionRepository.GetRecentAsync(100);
            var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);
            var monthly = recent.Where(t => t.Fecha >= thirtyDaysAgo).ToList();

            HasTransactions = monthly.Count > 0;

            if (monthly.Count > 0)
            {
                decimal totalExpenses = 0;
                decimal totalIncome = 0;

                foreach (var t in monthly)
                {
                    if (t.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase))
                        totalIncome += t.Monto;
                    else
                        totalExpenses += t.Monto;
                }

                var net = totalIncome - totalExpenses;
                var netSign = net >= 0 ? "+" : "-";
                NetBalanceText = $"{netSign} ${Math.Abs(net).ToString("N0", ArCulture)}";
                FinanceDetailsText = $"Gastos: ${totalExpenses.ToString("N0", ArCulture)} • Ingresos: ${totalIncome.ToString("N0", ArCulture)}";

                // Generate 7-day bar chart
                FinanceDailyBars.Clear();
                var now = DateTimeOffset.UtcNow.Date;
                var days = Enumerable.Range(0, 7)
                    .Select(i => now.AddDays(-6 + i))
                    .ToList();

                var dailyExpenses = monthly
                    .Where(t => !t.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(t => t.Fecha.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

                var dailyIncomes = monthly
                    .Where(t => t.Tipo.Equals("income", StringComparison.OrdinalIgnoreCase))
                    .GroupBy(t => t.Fecha.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Monto));

                var maxVal = Math.Max(
                    dailyExpenses.Values.DefaultIfEmpty(0).Max(),
                    dailyIncomes.Values.DefaultIfEmpty(0).Max());
                if (maxVal == 0) maxVal = 1;

                foreach (var day in days)
                {
                    dailyExpenses.TryGetValue(day, out var exp);
                    dailyIncomes.TryGetValue(day, out var inc);

                    var expRatio = (double)(exp / maxVal);
                    var incRatio = (double)(inc / maxVal);

                    var expHeight = exp > 0 ? Math.Max(6, expRatio * 42) : 3;
                    var incHeight = inc > 0 ? Math.Max(6, incRatio * 42) : 3;

                    var tooltip = $"{day:dddd dd/MM}\nGastos: ${exp.ToString("N0", ArCulture)}\nIngresos: ${inc.ToString("N0", ArCulture)}";

                    FinanceDailyBars.Add(new DailyFinanceBarItem
                    {
                        DayLabel = day.ToString("ddd", ArCulture),
                        FullDateLabel = day.ToString("dd/MM"),
                        ExpenseAmount = exp,
                        IncomeAmount = inc,
                        ExpenseHeightPx = expHeight,
                        IncomeHeightPx = incHeight,
                        ToolTipText = tooltip
                    });
                }
            }
            else
            {
                NetBalanceText = "$0";
                FinanceDetailsText = "Sin movimientos en los últimos 30 días.";
                FinanceDailyBars.Clear();
            }
        }
        catch
        {
            HasTransactions = false;
            NetBalanceText = "$0";
            FinanceDetailsText = "No se pudieron cargar las finanzas.";
            FinanceDailyBars.Clear();
        }
    }

    private async Task LoadNotesCardAsync()
    {
        try
        {
            var notes = await _noteRepository.GetRecentAsync(3);
            RecentNotes.Clear();
            RecentActivity.Clear();

            foreach (var n in notes)
            {
                RecentNotes.Add(new NoteItemViewModel(n));
                RecentActivity.Add(new RecentActivityItemViewModel(n));
            }

            HasNotes = RecentNotes.Count > 0;
        }
        catch
        {
            HasNotes = false;
        }
    }

    [RelayCommand]
    public void NavigateToSection(string? tag)
    {
        if (!string.IsNullOrWhiteSpace(tag))
        {
            NavigateRequested?.Invoke(tag);
        }
    }
}
