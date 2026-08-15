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
    public partial string Input { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

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

    public bool HasSearchResults => SearchResults.Count > 0 && !IsExpanded;

    public bool ShowFooterHint => !string.IsNullOrWhiteSpace(Input) || IsExpanded || HasSearchResults;

    public event Action? CloseRequested;
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
        if (IsBusy)
        {
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
        OnPropertyChanged(nameof(HasSearchResults));
        OnPropertyChanged(nameof(ShowFooterHint));
        WindowSizeChanged?.Invoke();
    }

    [RelayCommand]
    public void CloseExpanded()
    {
        IsExpanded = false;
        ExpandedItem = null;
        OnPropertyChanged(nameof(HasSearchResults));
        OnPropertyChanged(nameof(ShowFooterHint));
        WindowSizeChanged?.Invoke();
    }

    [RelayCommand]
    public void Cancel()
    {
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

        OnPropertyChanged(nameof(HasSearchResults));
        OnPropertyChanged(nameof(ShowFooterHint));
        WindowSizeChanged?.Invoke();
    }
}
