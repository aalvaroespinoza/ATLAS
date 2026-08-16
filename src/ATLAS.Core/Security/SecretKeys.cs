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

    /// <summary>
    /// Google Cloud OAuth Client ID for Gmail integration.
    /// </summary>
    public const string GmailClientId = "GmailClientId";

    /// <summary>
    /// Google Cloud OAuth Client Secret for Gmail integration.
    /// </summary>
    public const string GmailClientSecret = "GmailClientSecret";

    /// <summary>
    /// Google OAuth Refresh Token for offline access to Gmail.
    /// </summary>
    public const string GmailRefreshToken = "GmailRefreshToken";

    /// <summary>
    /// Master 4-digit security PIN for unlocking and viewing encrypted credentials in Settings.
    /// </summary>
    public const string MasterSecurityPin = "MasterSecurityPin";

    /// <summary>
    /// Supabase project URL (e.g. https://xyz.supabase.co).
    /// </summary>
    public const string SupabaseUrl = "SupabaseUrl";

    /// <summary>
    /// Supabase Anon / Service API Key.
    /// </summary>
    public const string SupabaseAnonKey = "SupabaseAnonKey";

    /// <summary>
    /// Supabase Auth JWT Access Token for authenticated RLS.
    /// </summary>
    public const string SupabaseAccessToken = "SupabaseAccessToken";

    /// <summary>
    /// Supabase Auth Refresh Token for session renewal.
    /// </summary>
    public const string SupabaseRefreshToken = "SupabaseRefreshToken";

    /// <summary>
    /// Unix timestamp (seconds) when the Supabase access token expires.
    /// </summary>
    public const string SupabaseTokenExpiresAt = "SupabaseTokenExpiresAt";

    /// <summary>
    /// Supabase User UUID (auth.uid()).
    /// </summary>
    public const string SupabaseUserId = "SupabaseUserId";

    /// <summary>
    /// Supabase User Email for UI display.
    /// </summary>
    public const string SupabaseUserEmail = "SupabaseUserEmail";
}
