using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.Supabase;

public class SupabaseAuthService : ISupabaseAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ISecretVault _secretVault;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public SupabaseAuthService(HttpClient httpClient, ISecretVault secretVault)
    {
        _httpClient = httpClient;
        _secretVault = secretVault;
    }

    public bool IsAuthenticated()
    {
        var refreshToken = _secretVault.GetSecret(SecretKeys.SupabaseRefreshToken);
        var userId = _secretVault.GetSecret(SecretKeys.SupabaseUserId);
        return !string.IsNullOrWhiteSpace(refreshToken) && !string.IsNullOrWhiteSpace(userId);
    }

    public string? GetUserId()
    {
        return _secretVault.GetSecret(SecretKeys.SupabaseUserId);
    }

    public string? GetUserEmail()
    {
        return _secretVault.GetSecret(SecretKeys.SupabaseUserEmail);
    }

    public async Task<SupabaseAuthResult> SignInWithPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            return new SupabaseAuthResult(false, "El email es requerido.");

        if (string.IsNullOrWhiteSpace(password))
            return new SupabaseAuthResult(false, "La contraseña es requerida.");

        var url = _secretVault.GetSecret(SecretKeys.SupabaseUrl)?.TrimEnd('/');
        var anonKey = _secretVault.GetSecret(SecretKeys.SupabaseAnonKey);

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(anonKey))
            return new SupabaseAuthResult(false, "Supabase no está configurado (falta URL o Anon Key).");

        try
        {
            var endpoint = $"{url}/auth/v1/token?grant_type=password";
            var payload = new { email = email.Trim(), password };
            var json = JsonSerializer.Serialize(payload, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("apikey", anonKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = ExtractErrorMessage(responseBody, $"Error de autenticación (HTTP {response.StatusCode})");
                return new SupabaseAuthResult(false, errorMsg);
            }

            var tokenResponse = JsonSerializer.Deserialize<SupabaseTokenResponse>(responseBody, JsonOptions);
            if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                return new SupabaseAuthResult(false, "Respuesta de autenticación inválida.");
            }

            PersistSession(tokenResponse, email);

            return new SupabaseAuthResult(
                IsSuccess: true,
                Message: "Sesión iniciada con éxito.",
                UserId: tokenResponse.User?.Id,
                Email: tokenResponse.User?.Email ?? email
            );
        }
        catch (Exception ex)
        {
            return new SupabaseAuthResult(false, $"Error al conectar con Supabase Auth: {ex.Message}");
        }
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = _secretVault.GetSecret(SecretKeys.SupabaseAccessToken);
        var expiresAtStr = _secretVault.GetSecret(SecretKeys.SupabaseTokenExpiresAt);

        if (long.TryParse(expiresAtStr, out var expiresAt))
        {
            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            // If token has at least 60 seconds of validity remaining, use it
            if (expiresAt > nowUnix + 60 && !string.IsNullOrWhiteSpace(accessToken))
            {
                return accessToken;
            }
        }

        // Token expired or missing, attempt refresh
        var refreshed = await RefreshSessionAsync(cancellationToken);
        if (refreshed)
        {
            return _secretVault.GetSecret(SecretKeys.SupabaseAccessToken);
        }

        return null;
    }

    public async Task<bool> RefreshSessionAsync(CancellationToken cancellationToken = default)
    {
        var url = _secretVault.GetSecret(SecretKeys.SupabaseUrl)?.TrimEnd('/');
        var anonKey = _secretVault.GetSecret(SecretKeys.SupabaseAnonKey);
        var refreshToken = _secretVault.GetSecret(SecretKeys.SupabaseRefreshToken);

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(anonKey) || string.IsNullOrWhiteSpace(refreshToken))
        {
            return false;
        }

        try
        {
            var endpoint = $"{url}/auth/v1/token?grant_type=refresh_token";
            var payload = new { refresh_token = refreshToken };
            var json = JsonSerializer.Serialize(payload, JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("apikey", anonKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var tokenResponse = JsonSerializer.Deserialize<SupabaseTokenResponse>(responseBody, JsonOptions);
            if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                return false;
            }

            PersistSession(tokenResponse, _secretVault.GetSecret(SecretKeys.SupabaseUserEmail));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void SignOut()
    {
        _secretVault.DeleteSecret(SecretKeys.SupabaseAccessToken);
        _secretVault.DeleteSecret(SecretKeys.SupabaseRefreshToken);
        _secretVault.DeleteSecret(SecretKeys.SupabaseTokenExpiresAt);
        _secretVault.DeleteSecret(SecretKeys.SupabaseUserId);
        _secretVault.DeleteSecret(SecretKeys.SupabaseUserEmail);
    }

    private void PersistSession(SupabaseTokenResponse token, string? fallbackEmail)
    {
        if (!string.IsNullOrWhiteSpace(token.AccessToken))
            _secretVault.SetSecret(SecretKeys.SupabaseAccessToken, token.AccessToken);

        if (!string.IsNullOrWhiteSpace(token.RefreshToken))
            _secretVault.SetSecret(SecretKeys.SupabaseRefreshToken, token.RefreshToken);

        var expiresAt = token.ExpiresAt > 0
            ? token.ExpiresAt
            : DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn > 0 ? token.ExpiresIn : 3600).ToUnixTimeSeconds();

        _secretVault.SetSecret(SecretKeys.SupabaseTokenExpiresAt, expiresAt.ToString());

        var userId = token.User?.Id;
        if (!string.IsNullOrWhiteSpace(userId))
            _secretVault.SetSecret(SecretKeys.SupabaseUserId, userId);

        var email = token.User?.Email ?? fallbackEmail;
        if (!string.IsNullOrWhiteSpace(email))
            _secretVault.SetSecret(SecretKeys.SupabaseUserEmail, email);
    }

    private static string ExtractErrorMessage(string responseBody, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("error_description", out var errorDesc))
                return errorDesc.GetString() ?? fallback;

            if (root.TryGetProperty("msg", out var msg))
                return msg.GetString() ?? fallback;

            if (root.TryGetProperty("message", out var message))
                return message.GetString() ?? fallback;

            if (root.TryGetProperty("error", out var err))
                return err.GetString() ?? fallback;
        }
        catch
        {
            // Ignore parse errors
        }

        return fallback;
    }

    private sealed class SupabaseTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("expires_at")]
        public long ExpiresAt { get; set; }

        [JsonPropertyName("user")]
        public SupabaseUserResponse? User { get; set; }
    }

    private sealed class SupabaseUserResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }
}
