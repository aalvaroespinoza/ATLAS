namespace ATLAS.Core.Security;

/// <summary>
/// Well-known identifier keys for secrets stored in ISecretVault (Windows Credential Locker).
/// </summary>
public static class SecretKeys
{
    /// <summary>
    /// Google Gemini API key for cloud intelligence features (ai.summarize, ai.ask).
    /// Preserves exact legacy key name to avoid breaking existing user configurations.
    /// </summary>
    public const string GeminiApiKey = "GeminiApiKey";

    /// <summary>
    /// Telegram Bot token obtained from @BotFather for mobile capture and habit completion.
    /// </summary>
    public const string TelegramBotToken = "TelegramBotToken";

    /// <summary>
    /// Mercado Pago personal access token for financial synchronization.
    /// </summary>
    public const string MercadoPagoAccessToken = "MercadoPagoAccessToken";
}
