using System.Net;
using ATLAS.Core.Integrations.Telegram;
using ATLAS.Core.Security;

namespace ATLAS.Core.Tests;

public class TelegramListenerServiceTests
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
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request);
        }
    }

    [Fact]
    public void ParseUpdatesJson_ShouldExtractMessagesCorrectly()
    {
        // Arrange
        const string sampleJson = """
            {
              "ok": true,
              "result": [
                {
                  "update_id": 5001,
                  "message": {
                    "message_id": 101,
                    "from": {
                      "id": 998877,
                      "is_bot": false,
                      "first_name": "Alvaro",
                      "username": "alvaroesp"
                    },
                    "chat": {
                      "id": 998877,
                      "type": "private"
                    },
                    "date": 1723740000,
                    "text": "Comprar libro de C#"
                  }
                },
                {
                  "update_id": 5002,
                  "message": {
                    "message_id": 102,
                    "from": {
                      "id": 998877,
                      "username": "alvaroesp"
                    },
                    "chat": {
                      "id": 998877
                    },
                    "date": 1723740060,
                    "text": "/habit agua"
                  }
                }
              ]
            }
            """;

        // Act
        var messages = TelegramListenerService.ParseUpdatesJson(sampleJson);

        // Assert
        Assert.Equal(2, messages.Count);

        Assert.Equal(5001, messages[0].UpdateId);
        Assert.Equal(101, messages[0].MessageId);
        Assert.Equal(998877, messages[0].ChatId);
        Assert.Equal("alvaroesp", messages[0].FromUsername);
        Assert.Equal("Comprar libro de C#", messages[0].Text);

        Assert.Equal(5002, messages[1].UpdateId);
        Assert.Equal(102, messages[1].MessageId);
        Assert.Equal("/habit agua", messages[1].Text);
    }

    [Fact]
    public async Task FetchUpdatesAsync_ShouldSendCorrectRequestAndReturnParsedMessages()
    {
        // Arrange
        var vault = new InMemorySecretVault();
        vault.SetSecret(SecretKeys.TelegramBotToken, "123456:TEST_TOKEN");

        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            var responseContent = """
                {
                  "ok": true,
                  "result": [
                    {
                      "update_id": 888,
                      "message": {
                        "message_id": 1,
                        "chat": { "id": 123 },
                        "from": { "username": "testuser" },
                        "date": 1723740000,
                        "text": "Nota desde Telegram"
                      }
                    }
                  ]
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var listener = new TelegramListenerService(vault, messageProcessor: null, httpClient: httpClient, pollingTimeoutSeconds: 5);

        // Act
        var updates = await listener.FetchUpdatesAsync("123456:TEST_TOKEN", offset: 100, CancellationToken.None);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains("123456:TEST_TOKEN/getUpdates", capturedRequest.RequestUri?.ToString());
        Assert.Single(updates);
        Assert.Equal(888, updates[0].UpdateId);
        Assert.Equal("Nota desde Telegram", updates[0].Text);
    }

    [Fact]
    public async Task StartAndStop_ShouldManageRunningStateGracefully()
    {
        // Arrange
        var vault = new InMemorySecretVault();
        var handler = new MockHttpMessageHandler(async req =>
        {
            await Task.Delay(50);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"ok": true, "result": []}""")
            };
        });

        using var httpClient = new HttpClient(handler);
        var listener = new TelegramListenerService(vault, messageProcessor: null, httpClient: httpClient);

        // Act
        await listener.StartAsync();
        Assert.True(listener.IsRunning);

        await listener.StopAsync();
        Assert.False(listener.IsRunning);
    }
}
