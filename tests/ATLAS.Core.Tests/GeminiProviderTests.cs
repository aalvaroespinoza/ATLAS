using System.Net;
using System.Text;
using System.Text.Json;
using ATLAS.Core.Ai;
using ATLAS.Core.Security;

namespace ATLAS.Core.Tests;

public class GeminiProviderTests
{
    private class InMemorySecretVault : ISecretVault
    {
        private readonly Dictionary<string, string> _secrets = new();

        public void SetSecret(string key, string secret) => _secrets[key] = secret;
        public string? GetSecret(string key) => _secrets.TryGetValue(key, out var v) ? v : null;
        public void DeleteSecret(string key) => _secrets.Remove(key);
        public bool HasSecret(string key) => _secrets.ContainsKey(key);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task SummarizeAsync_WithoutConfiguredKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var vault = new InMemorySecretVault(); // No key configured
        var provider = new GeminiProvider(vault);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.SummarizeAsync("Texto de prueba"));
        Assert.Contains("API Key de Gemini no está configurada", ex.Message);
    }

    [Fact]
    public async Task AskAsync_WithoutConfiguredKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var vault = new InMemorySecretVault();
        var provider = new GeminiProvider(vault);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AskAsync("¿Qué es ATLAS?"));
        Assert.Contains("API Key de Gemini no está configurada", ex.Message);
    }

    [Fact]
    public async Task SummarizeAsync_WithEmptyText_ReturnsEmptyWithoutHttpCall()
    {
        // Arrange
        var vault = new InMemorySecretVault();
        var provider = new GeminiProvider(vault);

        // Act
        var result = await provider.SummarizeAsync("   ");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public async Task SummarizeAsync_WithValidKeyAndResponse_ReturnsSummarizedText()
    {
        // Arrange
        var vault = new InMemorySecretVault();
        vault.SetSecret(GeminiProvider.SecretKeyName, "AIzaSyTestMockKey123");

        var geminiResponseJson = """
        {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            {
                                "text": "Este es el resumen generado por Gemini."
                            }
                        ],
                        "role": "model"
                    },
                    "finishReason": "STOP"
                }
            ]
        }
        """;

        var mockHandler = new MockHttpMessageHandler(req =>
        {
            Assert.Contains("key=AIzaSyTestMockKey123", req.RequestUri?.Query ?? string.Empty);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(geminiResponseJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler);
        var provider = new GeminiProvider(vault, httpClient);

        // Act
        var result = await provider.SummarizeAsync("Texto largo con muchos detalles...");

        // Assert
        Assert.Equal("Este es el resumen generado por Gemini.", result);
    }

    [Fact]
    public async Task AskAsync_WithValidKeyAndResponse_ReturnsAnswer()
    {
        // Arrange
        var vault = new InMemorySecretVault();
        vault.SetSecret(GeminiProvider.SecretKeyName, "AIzaSyTestMockKey123");

        var geminiResponseJson = """
        {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            {
                                "text": "Respuesta directa al prompt del usuario."
                            }
                        ],
                        "role": "model"
                    }
                }
            ]
        }
        """;

        var mockHandler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(geminiResponseJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler);
        var provider = new GeminiProvider(vault, httpClient);

        // Act
        var result = await provider.AskAsync("¿Cómo organizo mis notas?");

        // Assert
        Assert.Equal("Respuesta directa al prompt del usuario.", result);
    }

    [Fact]
    public async Task GeminiProvider_WhenApiReturnsError_ThrowsInvalidOperationExceptionWithApiDetails()
    {
        // Arrange
        var vault = new InMemorySecretVault();
        vault.SetSecret(GeminiProvider.SecretKeyName, "AIzaSyInvalidKey");

        var errorResponseJson = """
        {
            "error": {
                "code": 400,
                "message": "API key not valid. Please pass a valid API key.",
                "status": "INVALID_ARGUMENT"
            }
        }
        """;

        var mockHandler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorResponseJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler);
        var provider = new GeminiProvider(vault, httpClient);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AskAsync("Hola"));
        Assert.Contains("API key not valid", ex.Message);
    }

    [Fact]
    public async Task GeminiProvider_WithModelsPrefixInModelName_StripsPrefixCorrectly()
    {
        // Arrange
        var vault = new InMemorySecretVault();
        vault.SetSecret(GeminiProvider.SecretKeyName, "AIzaSyTestMockKey123");

        var geminiResponseJson = """
        {
            "candidates": [
                {
                    "content": {
                        "parts": [
                            { "text": "Respuesta correcta." }
                        ]
                    }
                }
            ]
        }
        """;

        string? requestedUrl = null;
        var mockHandler = new MockHttpMessageHandler(req =>
        {
            requestedUrl = req.RequestUri?.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(geminiResponseJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(mockHandler);
        // Pass "models/gemini-1.5-flash-latest" with prefix
        var provider = new GeminiProvider(vault, httpClient, model: "models/gemini-1.5-flash-latest");

        // Act
        var result = await provider.AskAsync("Hola");

        // Assert
        Assert.Equal("Respuesta correcta.", result);
        Assert.NotNull(requestedUrl);
        Assert.StartsWith("https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash-latest:generateContent", requestedUrl);
        Assert.DoesNotContain("models/models/", requestedUrl);
    }
}
