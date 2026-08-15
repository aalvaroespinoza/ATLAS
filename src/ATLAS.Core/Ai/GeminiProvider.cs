using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using ATLAS.Core.Security;

namespace ATLAS.Core.Ai;

/// <summary>
/// Implementation of IAiProvider using the Google Gemini API (gemini-1.5-flash free tier).
/// </summary>
public class GeminiProvider : IAiProvider
{
    private readonly ISecretVault _secretVault;
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public const string SecretKeyName = "GeminiApiKey";
    public const string DefaultModel = "gemini-1.5-flash";

    public GeminiProvider(ISecretVault secretVault, HttpClient? httpClient = null, string model = DefaultModel)
    {
        _secretVault = secretVault ?? throw new ArgumentNullException(nameof(secretVault));
        _httpClient = httpClient ?? new HttpClient();
        _model = model;
    }

    public async Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var prompt = $"Resumí de forma concisa y clara el siguiente texto, destacando los puntos más importantes:\n\n{text}";
        return await GenerateContentAsync(prompt, cancellationToken).ConfigureAwait(false);
    }

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return string.Empty;
        }

        return await GenerateContentAsync(prompt, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> GenerateContentAsync(string prompt, CancellationToken cancellationToken)
    {
        var apiKey = _secretVault.GetSecret(SecretKeyName);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "La API Key de Gemini no está configurada. Por favor, guardá tu API Key desde la ventana de Configuración.");
        }

        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={apiKey.Trim()}";

        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Error de conexión al consultar la API de Gemini: {ex.Message}", ex);
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = ExtractErrorMessage(responseBody, response.StatusCode.ToString());
            throw new InvalidOperationException($"Error de API de Gemini ({response.StatusCode}): {errorMessage}");
        }

        return ExtractGeneratedText(responseBody);
    }

    private static string ExtractErrorMessage(string responseJson, string defaultCode)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                if (errorElement.TryGetProperty("message", out var msgElement))
                {
                    return msgElement.GetString() ?? defaultCode;
                }
            }
        }
        catch
        {
            // Ignored on parse error
        }

        return $"Respuesta con estado {defaultCode}";
    }

    private static string ExtractGeneratedText(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var content))
                {
                    if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var firstPart = parts[0];
                        if (firstPart.TryGetProperty("text", out var textElement))
                        {
                            return textElement.GetString()?.Trim() ?? string.Empty;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"No se pudo interpretar la respuesta de Gemini: {ex.Message}", ex);
        }

        return string.Empty;
    }
}
