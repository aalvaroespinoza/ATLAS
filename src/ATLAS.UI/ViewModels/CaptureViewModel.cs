using ATLAS.Core.Commands;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace ATLAS.UI.ViewModels;

public partial class CaptureViewModel : ObservableObject
{
    private readonly ICommandRegistry _commandRegistry;

    [ObservableProperty]
    public partial string ContentInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TitleInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TagsInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    public partial bool IsBusy { get; set; }

    public bool IsNotBusy => !IsBusy;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasStatusMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusSeverity))]
    public partial bool IsSuccessStatus { get; set; }

    public InfoBarSeverity StatusSeverity => IsSuccessStatus ? InfoBarSeverity.Success : InfoBarSeverity.Error;

    public CaptureViewModel(ICommandRegistry commandRegistry)
    {
        _commandRegistry = commandRegistry ?? throw new ArgumentNullException(nameof(commandRegistry));
    }

    [RelayCommand]
    public async Task SaveNoteAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(ContentInput))
        {
            SetStatus("Por favor escribí el contenido de la nota.", isSuccess: false);
            return;
        }

        IsBusy = true;
        HasStatusMessage = false;

        try
        {
            var parameters = new Dictionary<string, object?>
            {
                ["content"] = ContentInput.Trim(),
                ["title"] = string.IsNullOrWhiteSpace(TitleInput) ? null : TitleInput.Trim(),
                ["tags"] = string.IsNullOrWhiteSpace(TagsInput) ? null : TagsInput.Trim(),
                ["source"] = "desktop_app",
                ["type"] = "note"
            };

            var result = await _commandRegistry.ExecuteAsync(CaptureNoteCommand.CommandId, parameters);

            if (result.IsSuccess)
            {
                ContentInput = string.Empty;
                TitleInput = string.Empty;
                TagsInput = string.Empty;
                SetStatus("✓ Nota guardada exitosamente en ATLAS.", isSuccess: true);
            }
            else
            {
                SetStatus(result.ErrorMessage ?? "Error al guardar la nota.", isSuccess: false);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}", isSuccess: false);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetStatus(string message, bool isSuccess)
    {
        StatusMessage = message;
        IsSuccessStatus = isSuccess;
        HasStatusMessage = true;
    }
}
