using ATLAS.Core.Ai;
using ATLAS.Core.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace ATLAS.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISecretVault _secretVault;

    [ObservableProperty]
    public partial string ApiKeyInput { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasConfiguredKey { get; set; }

    [ObservableProperty]
    public partial string MaskedKeyDisplay { get; set; } = "No configurada";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasStatusMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusSeverity))]
    public partial bool IsSuccessStatus { get; set; }

    public InfoBarSeverity StatusSeverity => IsSuccessStatus ? InfoBarSeverity.Success : InfoBarSeverity.Error;

    public SettingsViewModel(ISecretVault secretVault)
    {
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
        LoadKeyStatus();
    }

    [RelayCommand]
    public void LoadKeyStatus()
    {
        var key = _secretVault.GetSecret(GeminiProvider.SecretKeyName);
        HasConfiguredKey = !string.IsNullOrWhiteSpace(key);
        MaskedKeyDisplay = HasConfiguredKey ? MaskKey(key!) : "No configurada";
    }

    [RelayCommand]
    public void SaveKey()
    {
        if (string.IsNullOrWhiteSpace(ApiKeyInput))
        {
            StatusMessage = "Por favor ingresá una API Key válida de Google Gemini.";
            IsSuccessStatus = false;
            HasStatusMessage = true;
            return;
        }

        try
        {
            _secretVault.SetSecret(GeminiProvider.SecretKeyName, ApiKeyInput.Trim());
            ApiKeyInput = string.Empty;
            StatusMessage = "API Key guardada exitosamente en Windows Credential Locker.";
            IsSuccessStatus = true;
            HasStatusMessage = true;
            LoadKeyStatus();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al guardar la clave: {ex.Message}";
            IsSuccessStatus = false;
            HasStatusMessage = true;
        }
    }

    [RelayCommand]
    public void DeleteKey()
    {
        try
        {
            _secretVault.DeleteSecret(GeminiProvider.SecretKeyName);
            StatusMessage = "API Key eliminada del almacenamiento seguro.";
            IsSuccessStatus = true;
            HasStatusMessage = true;
            LoadKeyStatus();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al eliminar la clave: {ex.Message}";
            IsSuccessStatus = false;
            HasStatusMessage = true;
        }
    }

    private static string MaskKey(string key)
    {
        if (key.Length <= 8) return "••••••••";
        return $"{key[..4]}••••••••{key[^4..]}";
    }
}
