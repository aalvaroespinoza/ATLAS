using System.Net;
using System.Text.Json;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Integrations.Gmail;
using ATLAS.Core.Security;
using Xunit;

namespace ATLAS.Core.Tests;

public class GmailListRecentCommandTests
{
    private class InMemorySecretVault : ISecretVault
    {
        private readonly Dictionary<string, string> _secrets = new();

        public void SetSecret(string key, string secret) => _secrets[key] = secret;
        public string? GetSecret(string key) => _secrets.TryGetValue(key, out var val) ? val : null;
        public void DeleteSecret(string key) => _secrets.Remove(key);
        public bool HasSecret(string key) => _secrets.ContainsKey(key);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage>? Handler { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (Handler == null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
            }

            return Task.FromResult(Handler(request));
        }
    }

    [Fact]
    public async Task ExecuteAsync_WhenSecretsMissing_ReturnsFailure()
    {
        var vault = new InMemorySecretVault();
        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var gmailClient = new GmailClient(httpClient, vault);
        var command = new GmailListRecentCommand(gmailClient);

        var result = await command.ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("credenciales", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenMissing_ReturnsFailure()
    {
        var vault = new InMemorySecretVault();
        vault.SetSecret(SecretKeys.GmailClientId, "test-client-id");
        vault.SetSecret(SecretKeys.GmailClientSecret, "test-client-secret");

        var handler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var gmailClient = new GmailClient(httpClient, vault);
        var command = new GmailListRecentCommand(gmailClient);

        var result = await command.ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("sesión activa", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenRefreshTokenRevoked_ReturnsExplicitAuthFailure()
    {
        var vault = new InMemorySecretVault();
        vault.SetSecret(SecretKeys.GmailClientId, "test-client-id");
        vault.SetSecret(SecretKeys.GmailClientSecret, "test-client-secret");
        vault.SetSecret(SecretKeys.GmailRefreshToken, "test-refresh-token");

        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                if (req.RequestUri!.AbsoluteUri.Contains("oauth2.googleapis.com/token"))
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("{\"error\":\"invalid_grant\",\"error_description\":\"Token has been expired or revoked.\"}")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        };

        var httpClient = new HttpClient(handler);
        var gmailClient = new GmailClient(httpClient, vault);
        var command = new GmailListRecentCommand(gmailClient);

        var result = await command.ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("revocado o expiró", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenValid_ReturnsMessagesList()
    {
        var vault = new InMemorySecretVault();
        vault.SetSecret(SecretKeys.GmailClientId, "test-client-id");
        vault.SetSecret(SecretKeys.GmailClientSecret, "test-client-secret");
        vault.SetSecret(SecretKeys.GmailRefreshToken, "test-refresh-token");

        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                var uri = req.RequestUri!.AbsoluteUri;

                // 1. Token Refresh
                if (uri.Contains("oauth2.googleapis.com/token"))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"access_token\":\"mock-access-token\",\"expires_in\":3600,\"token_type\":\"Bearer\"}")
                    };
                }

                // 2. Messages List
                if (uri.Contains("/messages?maxResults="))
                {
                    var listJson = "{\"messages\":[{\"id\":\"msg_1\",\"threadId\":\"t_1\"},{\"id\":\"msg_2\",\"threadId\":\"t_2\"}]}";
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(listJson)
                    };
                }

                // 3. Message 1 Detail
                if (uri.Contains("/messages/msg_1"))
                {
                    var msg1 = new
                    {
                        id = "msg_1",
                        threadId = "t_1",
                        snippet = "Hola Álvaro, acá te paso el informe &amp; avance.",
                        payload = new
                        {
                            headers = new[]
                            {
                                new { name = "Subject", value = "Informe Semanal" },
                                new { name = "From", value = "Martin <martin@example.com>" },
                                new { name = "Date", value = "2026-08-15T18:30:00Z" }
                            }
                        }
                    };
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(msg1))
                    };
                }

                // 4. Message 2 Detail
                if (uri.Contains("/messages/msg_2"))
                {
                    var msg2 = new
                    {
                        id = "msg_2",
                        threadId = "t_2",
                        snippet = "Recordatorio de reunión",
                        payload = new
                        {
                            headers = new[]
                            {
                                new { name = "Subject", value = "Reunión de Sync" },
                                new { name = "From", value = "Lucia <lucia@example.com>" },
                                new { name = "Date", value = "2026-08-15T19:00:00Z" }
                            }
                        }
                    };
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(msg2))
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            }
        };

        var httpClient = new HttpClient(handler);
        var gmailClient = new GmailClient(httpClient, vault);
        var command = new GmailListRecentCommand(gmailClient);

        var result = await command.ExecuteAsync(new Dictionary<string, object?> { ["limit"] = 5 });

        Assert.True(result.IsSuccess);
        var messages = Assert.IsAssignableFrom<IReadOnlyList<GmailMessageSummary>>(result.Data);
        Assert.Equal(2, messages.Count);

        Assert.Equal("msg_1", messages[0].Id);
        Assert.Equal("Informe Semanal", messages[0].Subject);
        Assert.Equal("Martin <martin@example.com>", messages[0].From);
        Assert.Equal("Hola Álvaro, acá te paso el informe & avance.", messages[0].Snippet); // HTML unescaped
    }

    [Fact]
    public async Task ExchangeCodeAndSaveTokensAsync_SavesRefreshTokenToVault()
    {
        var vault = new InMemorySecretVault();
        vault.SetSecret(SecretKeys.GmailClientId, "test-client-id");
        vault.SetSecret(SecretKeys.GmailClientSecret, "test-client-secret");

        var handler = new MockHttpMessageHandler
        {
            Handler = req =>
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"new-access-token\",\"refresh_token\":\"new-refresh-token\",\"expires_in\":3600}")
                };
            }
        };

        var httpClient = new HttpClient(handler);
        var gmailClient = new GmailClient(httpClient, vault);

        var token = await gmailClient.ExchangeCodeAndSaveTokensAsync("auth-code-123", "http://localhost:5001/oauth2callback");

        Assert.Equal("new-access-token", token);
        Assert.Equal("new-refresh-token", vault.GetSecret(SecretKeys.GmailRefreshToken));
    }
}
