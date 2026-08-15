using System.Collections.ObjectModel;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace ATLAS.UI.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ICommandRegistry _commandRegistry;
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    public partial string SearchQuery { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial NoteItemViewModel? SelectedNote { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAiSummary))]
    [NotifyPropertyChangedFor(nameof(HasAiSummaryVisibility))]
    public partial string? AiSummary { get; set; }

    public bool HasAiSummary => !string.IsNullOrWhiteSpace(AiSummary);

    public Visibility HasAiSummaryVisibility => HasAiSummary ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAiSummarizingVisibility))]
    public partial bool IsAiSummarizing { get; set; }

    public Visibility IsAiSummarizingVisibility => IsAiSummarizing ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoResultsVisibility))]
    public partial bool HasNoResults { get; set; }

    public Visibility HasNoResultsVisibility => HasNoResults ? Visibility.Visible : Visibility.Collapsed;

    public ObservableCollection<NoteItemViewModel> Notes { get; } = [];

    public SearchViewModel(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
    }

    async partial void OnSearchQueryChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;

        var cts = new CancellationTokenSource();
        _searchCts = cts;

        try
        {
            await Task.Delay(150, cts.Token);
            await ExecuteSearchAsync(value.Trim(), cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
    }

    [RelayCommand]
    public async Task LoadInitialNotesAsync()
    {
        await ExecuteSearchAsync(SearchQuery.Trim(), CancellationToken.None);
    }

    private async Task ExecuteSearchAsync(string query, CancellationToken cancellationToken)
    {
        IsSearching = true;
        ErrorMessage = null;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["query"] = query,
                ["limit"] = 100
            };

            var result = await _commandRegistry.ExecuteAsync(KnowledgeSearchCommand.CommandId, parameters, cancellationToken);

            if (result.IsSuccess && result.Data is IReadOnlyList<Note> notes)
            {
                Notes.Clear();
                foreach (var note in notes)
                {
                    Notes.Add(new NoteItemViewModel(note));
                }

                if (SelectedNote == null || !Notes.Any(n => n.Note.Id == SelectedNote.Note.Id))
                {
                    SelectedNote = Notes.FirstOrDefault();
                    AiSummary = null;
                }

                HasNoResults = Notes.Count == 0;
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Error al buscar notas.";
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
    public async Task SummarizeSelectedNoteAsync()
    {
        if (SelectedNote == null || string.IsNullOrWhiteSpace(SelectedNote.Note.Content) || IsAiSummarizing)
        {
            return;
        }

        IsAiSummarizing = true;
        ErrorMessage = null;
        AiSummary = null;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["text"] = SelectedNote.Note.Content
            };

            var result = await _commandRegistry.ExecuteAsync(AiSummarizeCommand.CommandId, parameters);

            if (result.IsSuccess)
            {
                AiSummary = result.Data?.ToString() ?? "Sin resumen.";
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Error al generar resumen.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsAiSummarizing = false;
        }
    }
}
