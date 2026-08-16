using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using ATLAS.Core.Entities;
using ATLAS.Core.Security;

namespace ATLAS.Core.Integrations.Gmail;

/// <summary>
/// Implementation of IGmailClient using raw HttpClient and Google OAuth 2.0 / Gmail REST API.
/// </summary>
public sealed class GmailClient : IGmailClient
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GmailApiBase = "https://gmail.googleapis.com/gmail/v1/users/me";

    private readonly HttpClient _httpClient;
    private readonly ISecretVault _secretVault;

    private string? _cachedAccessToken;
    private DateTimeOffset _accessTokenExpiresAt = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    public GmailClient(HttpClient httpClient, ISecretVault secretVault)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
    }

    public async Task<string> ExchangeCodeAndSaveTokensAsync(string code, string redirectUri, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("El código de autorización no puede estar vacío.", nameof(code));

        var clientId = _secretVault.GetSecret(SecretKeys.GmailClientId);
        var clientSecret = _secretVault.GetSecret(SecretKeys.GmailClientSecret);

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            throw new GmailAuthException("Faltan configurar el Client ID y Client Secret de Google Cloud en Configuración.");

        var requestBody = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(requestBody)
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new GmailAuthException($"Error al intercambiar código de Google OAuth: {response.StatusCode} - {responseContent}");
        }

        var json = JsonNode.Parse(responseContent);
        var refreshToken = json?["refresh_token"]?.GetValue<string>();
        var accessToken = json?["access_token"]?.GetValue<string>();
        var expiresIn = json?["expires_in"]?.GetValue<int>() ?? 3600;

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            _secretVault.SetSecret(SecretKeys.GmailRefreshToken, refreshToken);
        }

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            _cachedAccessToken = accessToken;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);
        }

        return accessToken ?? string.Empty;
    }

    public async Task<IReadOnlyList<GmailMessageSummary>> ListRecentMessagesAsync(int limit = 10, string? query = null, CancellationToken cancellationToken = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, 50);
        var accessToken = await GetValidAccessTokenAsync(cancellationToken);

        var url = $"{GmailApiBase}/messages?maxResults={clampedLimit}";
        if (!string.IsNullOrWhiteSpace(query))
        {
            url += $"&q={Uri.EscapeDataString(query)}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token might have expired early, invalidate cache and retry once
            _cachedAccessToken = null;
            var freshToken = await GetValidAccessTokenAsync(cancellationToken);

            using var retryRequest = new HttpRequestMessage(HttpMethod.Get, url);
            retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);

            using var retryResponse = await _httpClient.SendAsync(retryRequest, cancellationToken);
            if (!retryResponse.IsSuccessStatusCode)
            {
                throw new GmailAuthException($"Error de autenticación al acceder a Gmail: {retryResponse.StatusCode}");
            }

            return await ParseMessagesListAsync(retryResponse, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Error al listar correos de Gmail: {response.StatusCode} - {err}");
        }

        return await ParseMessagesListAsync(response, cancellationToken);
    }

    private async Task<IReadOnlyList<GmailMessageSummary>> ParseMessagesListAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var root = JsonNode.Parse(content);
        var messagesArray = root?["messages"]?.AsArray();

        if (messagesArray == null || messagesArray.Count == 0)
        {
            return Array.Empty<GmailMessageSummary>();
        }

        var results = new List<GmailMessageSummary>();

        foreach (var msgNode in messagesArray)
        {
            var id = msgNode?["id"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(id)) continue;

            var summary = await FetchMessageDetailsAsync(id, cancellationToken);
            if (summary != null)
            {
                results.Add(summary);
            }
        }

        return results;
    }

    private async Task<GmailMessageSummary?> FetchMessageDetailsAsync(string messageId, CancellationToken cancellationToken)
    {
        var accessToken = await GetValidAccessTokenAsync(cancellationToken);
        var url = $"{GmailApiBase}/messages/{messageId}?format=metadata&metadataHeaders=Subject&metadataHeaders=From&metadataHeaders=Date";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var root = JsonNode.Parse(content);

        var id = root?["id"]?.GetValue<string>() ?? messageId;
        var threadId = root?["threadId"]?.GetValue<string>() ?? string.Empty;
        var snippetRaw = root?["snippet"]?.GetValue<string>() ?? string.Empty;
        var snippet = WebUtility.HtmlDecode(snippetRaw);

        var from = "Desconocido";
        var subject = "(Sin asunto)";
        var date = DateTimeOffset.UtcNow;

        var headers = root?["payload"]?["headers"]?.AsArray();
        if (headers != null)
        {
            foreach (var h in headers)
            {
                var name = h?["name"]?.GetValue<string>();
                var value = h?["value"]?.GetValue<string>() ?? string.Empty;

                if (string.Equals(name, "Subject", StringComparison.OrdinalIgnoreCase))
                {
                    subject = string.IsNullOrWhiteSpace(value) ? "(Sin asunto)" : value;
                }
                else if (string.Equals(name, "From", StringComparison.OrdinalIgnoreCase))
                {
                    from = value;
                }
                else if (string.Equals(name, "Date", StringComparison.OrdinalIgnoreCase))
                {
                    if (DateTimeOffset.TryParse(value, out var parsedDate))
                    {
                        date = parsedDate;
                    }
                }
            }
        }

        return new GmailMessageSummary(id, threadId, from, subject, snippet, date);
    }

    private async Task<string> GetValidAccessTokenAsync(CancellationToken cancellationToken)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedAccessToken) && DateTimeOffset.UtcNow < _accessTokenExpiresAt)
            {
                return _cachedAccessToken;
            }

            var clientId = _secretVault.GetSecret(SecretKeys.GmailClientId);
            var clientSecret = _secretVault.GetSecret(SecretKeys.GmailClientSecret);
            var refreshToken = _secretVault.GetSecret(SecretKeys.GmailRefreshToken);

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new GmailAuthException("Faltan configurar las credenciales OAuth de Google Cloud (Client ID y Client Secret) en Configuración.");
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new GmailAuthException("No se encontró una sesión activa de Gmail. Por favor vinculá tu cuenta de Google desde Configuración.");
            }

            var requestBody = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(requestBody)
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (content.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
                {
                    throw new GmailAuthException("El acceso a Gmail fue revocado o expiró. Por favor volvé a vincular tu cuenta desde Configuración.");
                }

                throw new GmailAuthException($"Error al refrescar token de Gmail: {response.StatusCode} - {content}");
            }

            var json = JsonNode.Parse(content);
            var newAccessToken = json?["access_token"]?.GetValue<string>();
            var expiresIn = json?["expires_in"]?.GetValue<int>() ?? 3600;

            if (string.IsNullOrWhiteSpace(newAccessToken))
            {
                throw new GmailAuthException("La respuesta de Google no devolvió un access token válido.");
            }

            _cachedAccessToken = newAccessToken;
            _accessTokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);

            return newAccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }
}
