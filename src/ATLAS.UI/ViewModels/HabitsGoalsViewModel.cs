using System.Collections.ObjectModel;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace ATLAS.UI.ViewModels;

public partial class HabitsGoalsViewModel : ObservableObject
{
    private readonly IHabitRepository _habitRepository;
    private readonly IGoalRepository _goalRepository;
    private readonly ICommandRegistry _commandRegistry;

    // Habits properties
    public ObservableCollection<HabitTodayItemViewModel> Habits { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoHabitsVisibility))]
    public partial bool HasNoHabits { get; set; }

    public Visibility HasNoHabitsVisibility => HasNoHabits ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    public partial string NewHabitName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewHabitFrequency { get; set; } = "daily";

    // Goals properties
    public ObservableCollection<GoalActiveItemViewModel> Goals { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoGoalsVisibility))]
    public partial bool HasNoGoals { get; set; }

    public Visibility HasNoGoalsVisibility => HasNoGoals ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    public partial string NewGoalTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public HabitsGoalsViewModel(
        IHabitRepository habitRepository,
        IGoalRepository goalRepository,
        ICommandRegistry commandRegistry)
    {
        _habitRepository = habitRepository ?? throw new ArgumentNullException(nameof(habitRepository));
        _goalRepository = goalRepository ?? throw new ArgumentNullException(nameof(goalRepository));
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
    }

    [RelayCommand]
    public async Task LoadAllAsync()
    {
        ErrorMessage = null;
        await Task.WhenAll(LoadHabitsAsync(), LoadGoalsAsync());
    }

    private async Task LoadHabitsAsync()
    {
        try
        {
            var habits = await _habitRepository.GetAllAsync();
            var todayStart = DateTimeOffset.UtcNow.Date;
            var todayEvents = await _habitRepository.GetEventsAsync(since: todayStart);

            var latestEventsByHabit = todayEvents
                .GroupBy(e => e.HabitId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CompletedAt).FirstOrDefault());

            Habits.Clear();
            foreach (var h in habits)
            {
                latestEventsByHabit.TryGetValue(h.Id, out var latestEvent);
                Habits.Add(new HabitTodayItemViewModel(h, latestEvent));
            }

            HasNoHabits = Habits.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar hábitos: {ex.Message}";
        }
    }

    private async Task LoadGoalsAsync()
    {
        try
        {
            var goals = await _goalRepository.GetAllAsync(status: "active");
            Goals.Clear();
            foreach (var g in goals)
            {
                Goals.Add(new GoalActiveItemViewModel(g));
            }

            HasNoGoals = Goals.Count == 0;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al cargar metas: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task CreateHabitAsync()
    {
        if (string.IsNullOrWhiteSpace(NewHabitName)) return;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["name"] = NewHabitName.Trim(),
                ["frequency"] = string.IsNullOrWhiteSpace(NewHabitFrequency) ? "daily" : NewHabitFrequency.Trim()
            };

            var result = await _commandRegistry.ExecuteAsync(HabitCreateCommand.CommandId, parameters);
            if (result.IsSuccess)
            {
                NewHabitName = string.Empty;
                await LoadHabitsAsync();
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task CompleteHabitAsync(string habitId)
    {
        if (string.IsNullOrWhiteSpace(habitId)) return;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["habit_id"] = habitId
            };

            var result = await _commandRegistry.ExecuteAsync(HabitCompleteCommand.CommandId, parameters);
            if (result.IsSuccess)
            {
                await LoadHabitsAsync();
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task CreateGoalAsync()
    {
        if (string.IsNullOrWhiteSpace(NewGoalTitle)) return;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["title"] = NewGoalTitle.Trim()
            };

            var result = await _commandRegistry.ExecuteAsync(GoalCreateCommand.CommandId, parameters);
            if (result.IsSuccess)
            {
                NewGoalTitle = string.Empty;
                await LoadGoalsAsync();
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    public async Task UpdateGoalProgressAsync(GoalActiveItemViewModel item)
    {
        if (item?.Goal == null) return;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["goal_id"] = item.Goal.Id,
                ["progress"] = item.Progress
            };

            var result = await _commandRegistry.ExecuteAsync(GoalUpdateProgressCommand.CommandId, parameters);
            if (result.IsSuccess)
            {
                await LoadGoalsAsync();
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }
}
