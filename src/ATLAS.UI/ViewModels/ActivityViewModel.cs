using System.Collections.ObjectModel;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLAS.UI.ViewModels;

public partial class ActivityViewModel : ObservableObject
{
    private readonly ICommandRegistry _commandRegistry;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoNotes))]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial NoteItemViewModel? SelectedNote { get; set; }

    public ObservableCollection<NoteItemViewModel> Notes { get; } = [];

    public bool HasNotes => Notes.Count > 0;

    public bool HasNoNotes => Notes.Count == 0 && !IsLoading;

    public string NotesCountText => Notes.Count == 1 ? "1 nota guardada" : $"{Notes.Count} notas guardadas";

    public event Action? OpenSettingsRequested;

    public ActivityViewModel(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
    }

    [RelayCommand]
    public async Task LoadNotesAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["query"] = "",
                ["limit"] = 100
            };

            var result = await _commandRegistry.ExecuteAsync(KnowledgeSearchCommand.CommandId, parameters);

            if (result.IsSuccess && result.Data is IReadOnlyList<Note> notes)
            {
                Notes.Clear();
                foreach (var note in notes)
                {
                    Notes.Add(new NoteItemViewModel(note));
                }

                SelectedNote = Notes.FirstOrDefault();
                OnPropertyChanged(nameof(HasNotes));
                OnPropertyChanged(nameof(HasNoNotes));
                OnPropertyChanged(nameof(NotesCountText));
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Error al cargar la actividad.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasNoNotes));
        }
    }

    [RelayCommand]
    public void OpenSettings()
    {
        OpenSettingsRequested?.Invoke();
    }
}
