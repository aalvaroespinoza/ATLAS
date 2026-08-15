using ATLAS.Core.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ATLAS.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISecretVault _secretVault;

    // Gemini API Key properties
    [ObservableProperty]
    public partial string GeminiApiKeyInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeleteGeminiButtonVisibility))]
    public partial bool HasGeminiKey { get; set; }

    [ObservableProperty]
    public partial string MaskedGeminiKeyDisplay { get; set; } = "No configurada";

    public Visibility DeleteGeminiButtonVisibility => HasGeminiKey ? Visibility.Visible : Visibility.Collapsed;

    // Telegram Bot Token properties
    [ObservableProperty]
    public partial string TelegramTokenInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeleteTelegramButtonVisibility))]
    public partial bool HasTelegramToken { get; set; }

    [ObservableProperty]
    public partial string MaskedTelegramTokenDisplay { get; set; } = "No configurado";

    public Visibility DeleteTelegramButtonVisibility => HasTelegramToken ? Visibility.Visible : Visibility.Collapsed;

    // Mercado Pago Access Token properties
    [ObservableProperty]
    public partial string MercadoPagoTokenInput { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DeleteMercadoPagoButtonVisibility))]
    public partial bool HasMercadoPagoToken { get; set; }

    [ObservableProperty]
    public partial string MaskedMercadoPagoTokenDisplay { get; set; } = "No configurado";

    public Visibility DeleteMercadoPagoButtonVisibility => HasMercadoPagoToken ? Visibility.Visible : Visibility.Collapsed;

    // Feedback status banner
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
        LoadAllSecretStatuses();
    }

    [RelayCommand]
    public void LoadAllSecretStatuses()
    {
        // 1. Gemini
        var geminiKey = _secretVault.GetSecret(SecretKeys.GeminiApiKey);
        HasGeminiKey = !string.IsNullOrWhiteSpace(geminiKey);
        MaskedGeminiKeyDisplay = HasGeminiKey ? MaskKey(geminiKey!) : "No configurada";

        // 2. Telegram
        var telegramToken = _secretVault.GetSecret(SecretKeys.TelegramBotToken);
        HasTelegramToken = !string.IsNullOrWhiteSpace(telegramToken);
        MaskedTelegramTokenDisplay = HasTelegramToken ? MaskKey(telegramToken!) : "No configurado";

        // 3. Mercado Pago
        var mpToken = _secretVault.GetSecret(SecretKeys.MercadoPagoAccessToken);
        HasMercadoPagoToken = !string.IsNullOrWhiteSpace(mpToken);
        MaskedMercadoPagoTokenDisplay = HasMercadoPagoToken ? MaskKey(mpToken!) : "No configurado";
    }

    [RelayCommand]
    public void LoadKeyStatus() => LoadAllSecretStatuses();

    [RelayCommand]
    public void SaveGeminiKey()
    {
        if (string.IsNullOrWhiteSpace(GeminiApiKeyInput))
        {
            SetStatus("Por favor ingresá una API Key válida de Google Gemini.", isSuccess: false);
            return;
        }

        try
        {
            _secretVault.SetSecret(SecretKeys.GeminiApiKey, GeminiApiKeyInput.Trim());
            GeminiApiKeyInput = string.Empty;
            SetStatus("Google Gemini API Key guardada exitosamente en Windows Credential Locker.", isSuccess: true);
            LoadAllSecretStatuses();
        }
        catch (Exception ex)
        {
            SetStatus($"Error al guardar la clave: {ex.Message}", isSuccess: false);
        }
    }

    [RelayCommand]
    public void DeleteGeminiKey()
    {
        try
        {
            _secretVault.DeleteSecret(SecretKeys.GeminiApiKey);
            SetStatus("API Key de Gemini eliminada del almacenamiento seguro.", isSuccess: true);
            LoadAllSecretStatuses();
        }
        catch (Exception ex)
        {
            SetStatus($"Error al eliminar la clave: {ex.Message}", isSuccess: false);
        }
    }

    [RelayCommand]
    public void SaveTelegramToken()
    {
        if (string.IsNullOrWhiteSpace(TelegramTokenInput))
        {
            SetStatus("Por favor ingresá un Token de Bot de Telegram válido.", isSuccess: false);
            return;
        }

        try
        {
            _secretVault.SetSecret(SecretKeys.TelegramBotToken, TelegramTokenInput.Trim());
            TelegramTokenInput = string.Empty;
            SetStatus("Token de Telegram guardado exitosamente en Windows Credential Locker.", isSuccess: true);
            LoadAllSecretStatuses();
        }
        catch (Exception ex)
        {
            SetStatus($"Error al guardar el token: {ex.Message}", isSuccess: false);
        }
    }

    [RelayCommand]
    public void DeleteTelegramToken()
    {
        try
        {
            _secretVault.DeleteSecret(SecretKeys.TelegramBotToken);
            SetStatus("Token de Telegram eliminado del almacenamiento seguro.", isSuccess: true);
            LoadAllSecretStatuses();
        }
        catch (Exception ex)
        {
            SetStatus($"Error al eliminar el token: {ex.Message}", isSuccess: false);
        }
    }

    [RelayCommand]
    public void SaveMercadoPagoToken()
    {
        if (string.IsNullOrWhiteSpace(MercadoPagoTokenInput))
        {
            SetStatus("Por favor ingresá un Access Token válido de Mercado Pago.", isSuccess: false);
            return;
        }

        try
        {
            _secretVault.SetSecret(SecretKeys.MercadoPagoAccessToken, MercadoPagoTokenInput.Trim());
            MercadoPagoTokenInput = string.Empty;
            SetStatus("Access Token de Mercado Pago guardado exitosamente en Windows Credential Locker.", isSuccess: true);
            LoadAllSecretStatuses();
        }
        catch (Exception ex)
        {
            SetStatus($"Error al guardar el token: {ex.Message}", isSuccess: false);
        }
    }

    [RelayCommand]
    public void DeleteMercadoPagoToken()
    {
        try
        {
            _secretVault.DeleteSecret(SecretKeys.MercadoPagoAccessToken);
            SetStatus("Access Token de Mercado Pago eliminado del almacenamiento seguro.", isSuccess: true);
            LoadAllSecretStatuses();
        }
        catch (Exception ex)
        {
            SetStatus($"Error al eliminar el token: {ex.Message}", isSuccess: false);
        }
    }

    private void SetStatus(string message, bool isSuccess)
    {
        StatusMessage = message;
        IsSuccessStatus = isSuccess;
        HasStatusMessage = true;
    }

    private static string MaskKey(string key)
    {
        if (key.Length <= 8) return "••••••••";
        return $"{key[..4]}••••••••{key[^4..]}";
    }
}
