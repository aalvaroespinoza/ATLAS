using System.Collections.ObjectModel;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLAS.UI.ViewModels;

public partial class LauncherViewModel : ObservableObject
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly IHabitRepository _habitRepository;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAskMode))]
    [NotifyPropertyChangedFor(nameof(IsGoalMode))]
    [NotifyPropertyChangedFor(nameof(IsHabitCreateMode))]
    [NotifyPropertyChangedFor(nameof(IsHabitCompleteMode))]
    [NotifyPropertyChangedFor(nameof(ModeBadgeText))]
    [NotifyPropertyChangedFor(nameof(HasModeBadge))]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    [NotifyPropertyChangedFor(nameof(ShowFooterHint))]
    public partial string Input { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShowingAiResult))]
    [NotifyPropertyChangedFor(nameof(IsAiResultReady))]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    [NotifyPropertyChangedFor(nameof(ShowFooterHint))]
    public partial bool IsAiThinking { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsShowingAiResult))]
    [NotifyPropertyChangedFor(nameof(IsAiResultReady))]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    [NotifyPropertyChangedFor(nameof(ShowFooterHint))]
    public partial string? AiResponse { get; set; }

    [ObservableProperty]
    public partial string AiModeTitle { get; set; } = "Respuesta de IA";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    [NotifyPropertyChangedFor(nameof(ShowFooterHint))]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    public partial LauncherItemViewModel? ExpandedItem { get; set; }

    [ObservableProperty]
    public partial LauncherItemViewModel? SelectedItem { get; set; }

    public ObservableCollection<LauncherItemViewModel> SearchResults { get; } = [];

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    // Modes
    public bool IsAskMode => !string.IsNullOrWhiteSpace(Input) && Input.TrimStart().StartsWith("?");

    public bool IsGoalMode => MatchesPrefix(Input, "/goal", "!goal");

    public bool IsHabitCreateMode => MatchesPrefix(Input, "/habit", "!habit");

    public bool IsHabitCompleteMode => MatchesPrefix(Input, "/done", "!done", "hecho");

    public bool HasModeBadge => IsAskMode || IsGoalMode || IsHabitCreateMode || IsHabitCompleteMode;

    public string ModeBadgeText
    {
        get
        {
            if (IsAskMode) return "IA Mode (?)";
            if (IsGoalMode) return "Nueva Meta";
            if (IsHabitCreateMode) return "Nuevo Hábito";
            if (IsHabitCompleteMode) return "Completar Hábito";
            return string.Empty;
        }
    }

    public bool IsShowingAiResult => !string.IsNullOrWhiteSpace(AiResponse) || IsAiThinking;

    public bool IsAiResultReady => !string.IsNullOrWhiteSpace(AiResponse) && !IsAiThinking;

    public bool HasSearchResults => SearchResults.Count > 0 && !IsExpanded && !IsShowingAiResult;

    public bool ShowFooterHint => !string.IsNullOrWhiteSpace(Input) || IsExpanded || HasSearchResults || IsShowingAiResult;

    public event Action? CloseRequested;
    public event Action? OpenActivityRequested;
    public event Action? WindowSizeChanged;

    public LauncherViewModel(ICommandRegistry commandRegistry, IHabitRepository habitRepository)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
        _habitRepository = habitRepository ?? throw new ArgumentNullException(nameof(habitRepository));
    }

    async partial void OnInputChanged(string value)
    {
        ErrorMessage = null;

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;

        if (IsExpanded)
        {
            IsExpanded = false;
            ExpandedItem = null;
        }

        if (IsShowingAiResult && !IsAiThinking)
        {
            AiResponse = null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            SearchResults.Clear();
            SelectedItem = null;
            OnPropertyChanged(nameof(HasSearchResults));
            OnPropertyChanged(nameof(ShowFooterHint));
            WindowSizeChanged?.Invoke();
            return;
        }

        // Ask Mode (? query) -> no search list
        if (IsAskMode)
        {
            SearchResults.Clear();
            SelectedItem = null;
            OnPropertyChanged(nameof(HasSearchResults));
            OnPropertyChanged(nameof(ShowFooterHint));
            WindowSizeChanged?.Invoke();
            return;
        }

        // Goal Mode (/goal title) -> no search list
        if (IsGoalMode)
        {
            SearchResults.Clear();
            SelectedItem = null;
            OnPropertyChanged(nameof(HasSearchResults));
            OnPropertyChanged(nameof(ShowFooterHint));
            WindowSizeChanged?.Invoke();
            return;
        }

        // Habit Create Mode (/habit name) -> no search list
        if (IsHabitCreateMode)
        {
            SearchResults.Clear();
            SelectedItem = null;
            OnPropertyChanged(nameof(HasSearchResults));
            OnPropertyChanged(nameof(ShowFooterHint));
            WindowSizeChanged?.Invoke();
            return;
        }

        var cts = new CancellationTokenSource();
        _searchCts = cts;

        try
        {
            await Task.Delay(140, cts.Token);

            if (IsHabitCompleteMode)
            {
                await PerformHabitSearchAsync(value.Trim(), cts.Token);
            }
            else
            {
                await PerformNoteSearchAsync(value.Trim(), cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
    }

    private async Task PerformNoteSearchAsync(string query, CancellationToken cancellationToken)
    {
        IsSearching = true;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["query"] = query,
                ["limit"] = 6
            };

            var result = await _commandRegistry.ExecuteAsync(KnowledgeSearchCommand.CommandId, parameters, cancellationToken);

            if (result.IsSuccess && result.Data is IReadOnlyList<Note> notes)
            {
                SearchResults.Clear();
                foreach (var note in notes)
                {
                    SearchResults.Add(new LauncherItemViewModel(note));
                }

                SelectedItem = null;
                OnPropertyChanged(nameof(HasSearchResults));
                OnPropertyChanged(nameof(ShowFooterHint));
                WindowSizeChanged?.Invoke();
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al buscar: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    private async Task PerformHabitSearchAsync(string input, CancellationToken cancellationToken)
    {
        IsSearching = true;

        try
        {
            var (habitQuery, _) = ParseHabitCompleteInput(input);
            var allHabits = await _habitRepository.GetAllAsync(cancellationToken);

            var matches = string.IsNullOrWhiteSpace(habitQuery)
                ? allHabits
                : allHabits.Where(h =>
                    h.Name.Contains(habitQuery, StringComparison.CurrentCultureIgnoreCase) ||
                    (!string.IsNullOrEmpty(h.Description) && h.Description.Contains(habitQuery, StringComparison.CurrentCultureIgnoreCase)))
                  .ToList();

            SearchResults.Clear();
            foreach (var habit in matches)
            {
                SearchResults.Add(new LauncherItemViewModel(habit));
            }

            SelectedItem = SearchResults.FirstOrDefault();
            OnPropertyChanged(nameof(HasSearchResults));
            OnPropertyChanged(nameof(ShowFooterHint));
            WindowSizeChanged?.Invoke();
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error al buscar hábitos: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    [RelayCommand]
    public async Task SubmitAsync()
    {
        if (IsBusy || IsAiThinking)
        {
            return;
        }

        if (IsShowingAiResult && !IsAiThinking)
        {
            Reset();
            CloseRequested?.Invoke();
            return;
        }

        // 1. AI Ask Mode
        if (IsAskMode)
        {
            var prompt = Input.TrimStart()[1..].Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                ErrorMessage = "Escribí tu pregunta después del signo '?'.";
                return;
            }
            await ExecuteAiAskAsync(prompt);
            return;
        }

        // 2. Goal Create Mode
        if (IsGoalMode)
        {
            var title = StripPrefix(Input, "/goal", "!goal");
            if (string.IsNullOrWhiteSpace(title))
            {
                ErrorMessage = "Escribí el título de la meta (ej: /goal Aprender C#).";
                return;
            }

            await ExecuteGoalCreateAsync(title);
            return;
        }

        // 3. Habit Create Mode
        if (IsHabitCreateMode)
        {
            var habitPayload = StripPrefix(Input, "/habit", "!habit");
            if (string.IsNullOrWhiteSpace(habitPayload))
            {
                ErrorMessage = "Escribí el nombre del hábito (ej: /habit Tomar agua | daily).";
                return;
            }

            await ExecuteHabitCreateAsync(habitPayload);
            return;
        }

        // 4. Habit Complete Mode
        if (IsHabitCompleteMode)
        {
            var (habitQuery, note) = ParseHabitCompleteInput(Input);
            var targetHabit = SelectedItem?.Habit ?? SearchResults.FirstOrDefault(s => s.IsHabit)?.Habit;

            if (targetHabit == null)
            {
                ErrorMessage = string.IsNullOrWhiteSpace(habitQuery)
                    ? "No tenés hábitos registrados para completar."
                    : $"No se encontró ningún hábito que coincida con '{habitQuery}'.";
                return;
            }

            await ExecuteHabitCompleteAsync(targetHabit.Id, note);
            return;
        }

        // 5. Selected item from search list
        if (SelectedItem != null && !IsExpanded)
        {
            if (SelectedItem.IsHabit && SelectedItem.Habit != null)
            {
                await ExecuteHabitCompleteAsync(SelectedItem.Habit.Id, null);
                return;
            }

            ExpandItem(SelectedItem);
            return;
        }

        if (IsExpanded)
        {
            Reset();
            CloseRequested?.Invoke();
            return;
        }

        // 6. Default Note Capture
        if (string.IsNullOrWhiteSpace(Input))
        {
            ErrorMessage = "Escribí algo antes de guardar.";
            return;
        }

        await ExecuteCaptureNoteAsync(Input.Trim());
    }

    private async Task ExecuteGoalCreateAsync(string title)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["title"] = title
            };

            var result = await _commandRegistry.ExecuteAsync(GoalCreateCommand.CommandId, parameters);
            if (result.IsSuccess)
            {
                Reset();
                CloseRequested?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Error al crear la meta.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteHabitCreateAsync(string payload)
    {
        IsBusy = true;
        ErrorMessage = null;

        var name = payload;
        var frequency = "daily";

        if (payload.Contains('|'))
        {
            var parts = payload.Split('|', 2, StringSplitOptions.TrimEntries);
            name = parts[0];
            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                frequency = parts[1];
            }
        }

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["name"] = name,
                ["frequency"] = frequency
            };

            var result = await _commandRegistry.ExecuteAsync(HabitCreateCommand.CommandId, parameters);
            if (result.IsSuccess)
            {
                Reset();
                CloseRequested?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Error al crear el hábito.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteHabitCompleteAsync(string habitId, string? note)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["habit_id"] = habitId,
                ["note"] = note
            };

            var result = await _commandRegistry.ExecuteAsync(HabitCompleteCommand.CommandId, parameters);
            if (result.IsSuccess)
            {
                Reset();
                CloseRequested?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Error al completar el hábito.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteCaptureNoteAsync(string content)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["content"] = content,
                ["source"] = "launcher"
            };

            var result = await _commandRegistry.ExecuteAsync(CaptureNoteCommand.CommandId, parameters);

            if (result.IsSuccess)
            {
                Reset();
                CloseRequested?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Ocurrió un error al guardar la nota.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task SummarizeExpandedNoteAsync()
    {
        if (ExpandedItem?.Note == null || string.IsNullOrWhiteSpace(ExpandedItem.Note.Content) || IsAiThinking)
        {
            return;
        }

        IsAiThinking = true;
        ErrorMessage = null;
        AiResponse = null;
        AiModeTitle = $"Resumen: {ExpandedItem.DisplayTitle}";
        WindowSizeChanged?.Invoke();

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["text"] = ExpandedItem.Note.Content
            };

            var result = await _commandRegistry.ExecuteAsync(AiSummarizeCommand.CommandId, parameters);

            if (result.IsSuccess)
            {
                AiResponse = result.Data?.ToString() ?? "Sin resumen.";
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Error al generar resumen.";
                AiResponse = null;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            AiResponse = null;
        }
        finally
        {
            IsAiThinking = false;
            WindowSizeChanged?.Invoke();
        }
    }

    private async Task ExecuteAiAskAsync(string prompt)
    {
        IsAiThinking = true;
        ErrorMessage = null;
        AiResponse = null;
        AiModeTitle = $"Pregunta: {prompt}";
        WindowSizeChanged?.Invoke();

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["prompt"] = prompt
            };

            var result = await _commandRegistry.ExecuteAsync(AiAskCommand.CommandId, parameters);

            if (result.IsSuccess)
            {
                AiResponse = result.Data?.ToString() ?? "Sin respuesta.";
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Error al consultar a Gemini.";
                AiResponse = null;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            AiResponse = null;
        }
        finally
        {
            IsAiThinking = false;
            WindowSizeChanged?.Invoke();
        }
    }

    [RelayCommand]
    public void SelectItem(LauncherItemViewModel? item)
    {
        if (item == null) return;

        if (item.IsHabit && item.Habit != null)
        {
            _ = ExecuteHabitCompleteAsync(item.Habit.Id, null);
            return;
        }

        ExpandItem(item);
    }

    private void ExpandItem(LauncherItemViewModel item)
    {
        ExpandedItem = item;
        IsExpanded = true;
        SelectedItem = null;
        AiResponse = null;
        IsAiThinking = false;
        OnPropertyChanged(nameof(HasSearchResults));
        OnPropertyChanged(nameof(ShowFooterHint));
        WindowSizeChanged?.Invoke();
    }

    [RelayCommand]
    public void CloseExpanded()
    {
        IsExpanded = false;
        ExpandedItem = null;
        AiResponse = null;
        IsAiThinking = false;
        OnPropertyChanged(nameof(HasSearchResults));
        OnPropertyChanged(nameof(ShowFooterHint));
        WindowSizeChanged?.Invoke();
    }

    [RelayCommand]
    public void CloseAiResult()
    {
        AiResponse = null;
        IsAiThinking = false;
        OnPropertyChanged(nameof(IsShowingAiResult));
        OnPropertyChanged(nameof(HasSearchResults));
        OnPropertyChanged(nameof(ShowFooterHint));
        WindowSizeChanged?.Invoke();
    }

    [RelayCommand]
    public void OpenActivity()
    {
        Reset();
        CloseRequested?.Invoke();
        OpenActivityRequested?.Invoke();
    }

    [RelayCommand]
    public void Cancel()
    {
        if (IsShowingAiResult)
        {
            CloseAiResult();
            return;
        }

        if (IsExpanded)
        {
            CloseExpanded();
            return;
        }

        Reset();
        CloseRequested?.Invoke();
    }

    public void Reset()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;

        Input = string.Empty;
        SearchResults.Clear();
        SelectedItem = null;
        ExpandedItem = null;
        IsExpanded = false;
        ErrorMessage = null;
        IsBusy = false;
        IsSearching = false;
        IsAiThinking = false;
        AiResponse = null;

        OnPropertyChanged(nameof(HasSearchResults));
        OnPropertyChanged(nameof(ShowFooterHint));
        WindowSizeChanged?.Invoke();
    }

    private static bool MatchesPrefix(string text, params string[] prefixes)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.TrimStart();
        foreach (var prefix in prefixes)
        {
            if (trimmed.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string StripPrefix(string text, params string[] prefixes)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var trimmed = text.TrimStart();
        foreach (var prefix in prefixes)
        {
            if (trimmed.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[(prefix.Length + 1)..].Trim();
            }
            if (trimmed.Equals(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
        }
        return trimmed;
    }

    private static (string Query, string? Note) ParseHabitCompleteInput(string input)
    {
        var raw = StripPrefix(input, "/done", "!done", "hecho");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (string.Empty, null);
        }

        if (raw.Contains(':'))
        {
            var parts = raw.Split(':', 2, StringSplitOptions.TrimEntries);
            return (parts[0], parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : null);
        }

        return (raw, null);
    }
}
