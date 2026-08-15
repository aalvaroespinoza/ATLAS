using System.Collections.ObjectModel;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLAS.UI.ViewModels;

public partial class LauncherViewModel : ObservableObject
{
    private readonly ICommandRegistry _commandRegistry;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAskMode))]
    [NotifyPropertyChangedFor(nameof(AskPromptPreview))]
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
    public partial NoteItemViewModel? ExpandedItem { get; set; }

    [ObservableProperty]
    public partial NoteItemViewModel? SelectedItem { get; set; }

    public ObservableCollection<NoteItemViewModel> SearchResults { get; } = [];

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public bool IsAskMode => !string.IsNullOrWhiteSpace(Input) && Input.TrimStart().StartsWith("?");

    public string AskPromptPreview => IsAskMode ? Input.TrimStart()[1..].Trim() : string.Empty;

    public bool IsShowingAiResult => !string.IsNullOrWhiteSpace(AiResponse) || IsAiThinking;

    public bool IsAiResultReady => !string.IsNullOrWhiteSpace(AiResponse) && !IsAiThinking;

    public bool HasSearchResults => SearchResults.Count > 0 && !IsExpanded && !IsShowingAiResult && !IsAskMode;

    public bool ShowFooterHint => !string.IsNullOrWhiteSpace(Input) || IsExpanded || HasSearchResults || IsShowingAiResult || IsAskMode;

    public event Action? CloseRequested;
    public event Action? OpenActivityRequested;
    public event Action? WindowSizeChanged;

    public LauncherViewModel(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
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

        // If in ask mode (starts with ?), do not search SQLite notes
        if (value.TrimStart().StartsWith("?"))
        {
            SearchResults.Clear();
            SelectedItem = null;
            OnPropertyChanged(nameof(HasSearchResults));
            OnPropertyChanged(nameof(ShowFooterHint));
            WindowSizeChanged?.Invoke();
            return;
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

        var cts = new CancellationTokenSource();
        _searchCts = cts;

        try
        {
            await Task.Delay(160, cts.Token);
            await PerformSearchAsync(value.Trim(), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Newer keystroke arrived
        }
    }

    private async Task PerformSearchAsync(string query, CancellationToken cancellationToken)
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
                    SearchResults.Add(new NoteItemViewModel(note));
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

    [RelayCommand]
    public async Task SubmitAsync()
    {
        if (IsBusy || IsAiThinking)
        {
            return;
        }

        // If currently displaying AI result, pressing enter closes launcher
        if (IsShowingAiResult && !IsAiThinking)
        {
            Reset();
            CloseRequested?.Invoke();
            return;
        }

        // AI Ask mode (? prompt)
        if (IsAskMode)
        {
            var prompt = AskPromptPreview;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                ErrorMessage = "Escribí tu pregunta después del signo '?'.";
                return;
            }

            await ExecuteAiAskAsync(prompt);
            return;
        }

        // If an item in the search list is selected, expand it in the launcher
        if (SelectedItem != null && !IsExpanded)
        {
            ExpandItem(SelectedItem);
            return;
        }

        if (IsExpanded)
        {
            Reset();
            CloseRequested?.Invoke();
            return;
        }

        // Capture note
        if (string.IsNullOrWhiteSpace(Input))
        {
            ErrorMessage = "Escribí algo antes de guardar.";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["content"] = Input.Trim(),
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
        if (ExpandedItem == null || string.IsNullOrWhiteSpace(ExpandedItem.Note.Content) || IsAiThinking)
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
    public void SelectItem(NoteItemViewModel? item)
    {
        if (item == null) return;
        ExpandItem(item);
    }

    private void ExpandItem(NoteItemViewModel item)
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
}
