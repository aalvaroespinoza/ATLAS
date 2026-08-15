using ATLAS.Core.Commands;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ATLAS.UI.ViewModels;

public partial class LauncherViewModel : ObservableObject
{
    private readonly ICommandRegistry _commandRegistry;

    [ObservableProperty]
    public partial string Input { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasErrorMessage))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    public bool HasErrorMessage => !string.IsNullOrWhiteSpace(ErrorMessage);

    public event Action? CloseRequested;

    public LauncherViewModel(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
    }

    [RelayCommand]
    public async Task SubmitAsync()
    {
        if (IsBusy)
        {
            return;
        }

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
                ["content"] = Input,
                ["source"] = "launcher"
            };

            var result = await _commandRegistry.ExecuteAsync(CaptureNoteCommand.CommandId, parameters);

            if (result.IsSuccess)
            {
                Input = string.Empty;
                ErrorMessage = null;
                CloseRequested?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Ocurrió un error inesperado al guardar la nota.";
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
    public void Cancel()
    {
        Input = string.Empty;
        ErrorMessage = null;
        CloseRequested?.Invoke();
    }

    public void Reset()
    {
        Input = string.Empty;
        ErrorMessage = null;
        IsBusy = false;
    }
}
