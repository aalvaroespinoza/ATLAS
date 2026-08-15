using System.Net;
using ATLAS.Core.Commands;
using ATLAS.Core.Security;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

namespace ATLAS.Core.Tests;

public class FinanceSyncMercadoPagoCommandTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;
    private readonly TransactionsRepository _repository;
    private readonly CommandRegistry _registry;

    public FinanceSyncMercadoPagoCommandTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_mp_sync_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_testDbPath}";
        var initializer = new DatabaseInitializer(_connectionString);
        initializer.InitializeAsync().GetAwaiter().GetResult();

        _repository = new TransactionsRepository(_connectionString);
        _registry = new CommandRegistry();
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }
        catch
        {
            // Ignore cleanup
        }
        GC.SuppressFinalize(this);
    }

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
    public async Task ExecuteAsync_WhenTokenNotConfigured_ShouldReturnFailure()
    {
        // Arrange
        var vault = new InMemorySecretVault(); // No token set
        var command = new FinanceSyncMercadoPagoCommand(vault, _repository);
        _registry.Register(command);

        // Act
        var result = await _registry.ExecuteAsync(FinanceSyncMercadoPagoCommand.CommandId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("no está configurado", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_WhenApiReturns401Unauthorized_ShouldReturnDescriptiveFailure()
    {
        // Arrange
        var vault = new InMemorySecretVault();
        vault.SetSecret(SecretKeys.MercadoPagoAccessToken, "INVALID_TOKEN");

        var handler = new MockHttpMessageHandler(async req =>
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("""{"message": "Invalid token", "error": "unauthorized", "status": 401}""")
            };
        });

        using var httpClient = new HttpClient(handler);
        var command = new FinanceSyncMercadoPagoCommand(vault, _repository, httpClient);
        _registry.Register(command);

        // Act
        var result = await _registry.ExecuteAsync(FinanceSyncMercadoPagoCommand.CommandId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Unauthorized", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_WhenApiReturnsPayments_ShouldNormalizeAndPersistIdempotently()
    {
        // Arrange
        var vault = new InMemorySecretVault();
        vault.SetSecret(SecretKeys.MercadoPagoAccessToken, "APP_USR-VALID-TOKEN");

        HttpRequestMessage? capturedRequest = null;
        const string mockResponse = """
            {
              "paging": { "total": 2, "limit": 50, "offset": 0 },
              "results": [
                {
                  "id": 9001,
                  "date_created": "2026-08-15T12:00:00.000-03:00",
                  "date_approved": "2026-08-15T12:00:05.000-03:00",
                  "operation_type": "regular_payment",
                  "payment_type_id": "credit_card",
                  "payment_method_id": "visa",
                  "status": "approved",
                  "status_detail": "accredited",
                  "currency_id": "ARS",
                  "description": "Supermercado Carrefour",
                  "transaction_amount": 18250.50
                },
                {
                  "id": 9002,
                  "date_created": "2026-08-15T14:30:00.000-03:00",
                  "date_approved": "2026-08-15T14:30:02.000-03:00",
                  "operation_type": "regular_payment",
                  "payment_type_id": "account_money",
                  "payment_method_id": "account_money",
                  "status": "approved",
                  "status_detail": "accredited",
                  "currency_id": "ARS",
                  "description": "Café Martínez",
                  "transaction_amount": 3400.00
                }
              ]
            }
            """;

        var handler = new MockHttpMessageHandler(async req =>
        {
            capturedRequest = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponse, System.Text.Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var command = new FinanceSyncMercadoPagoCommand(vault, _repository, httpClient);
        _registry.Register(command);

        // Act 1: First sync
        var result1 = await _registry.ExecuteAsync(FinanceSyncMercadoPagoCommand.CommandId);

        // Assert 1
        Assert.NotNull(capturedRequest);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal("APP_USR-VALID-TOKEN", capturedRequest.Headers.Authorization?.Parameter);
        Assert.True(result1.IsSuccess);

        var allTransactions = await _repository.GetRecentAsync(10);
        Assert.Equal(2, allTransactions.Count);

        var first = allTransactions.First(t => t.IdExterno == "9001");
        Assert.Equal(18250.50m, first.Monto);
        Assert.Equal("Supermercado Carrefour", first.Descripcion);
        Assert.Equal("mercadopago", first.Origen);
        Assert.Null(first.Categoria); // Category must be null initially
        Assert.Equal("approved", first.Estado);
        Assert.NotNull(first.Metadata);
        Assert.Contains("credit_card", first.Metadata);

        // Act 2: Second sync with same data (idempotency check)
        var result2 = await _registry.ExecuteAsync(FinanceSyncMercadoPagoCommand.CommandId);
        Assert.True(result2.IsSuccess);

        var afterSecondSync = await _repository.GetRecentAsync(10);
        Assert.Equal(2, afterSecondSync.Count); // Count does not increase
    }

    [Fact]
    public void ParseMercadoPagoPaymentsJson_ShouldExtractFieldsCorrectly()
    {
        // Arrange
        const string json = """
            {
              "results": [
                {
                  "id": "778899",
                  "date_approved": "2026-08-15T10:15:00.000-03:00",
                  "transaction_amount": 7500,
                  "description": "Farmacity",
                  "currency_id": "ARS",
                  "status": "approved"
                }
              ]
            }
            """;

        // Act
        var list = FinanceSyncMercadoPagoCommand.ParseMercadoPagoPaymentsJson(json);

        // Assert
        Assert.Single(list);
        Assert.Equal("778899", list[0].IdExterno);
        Assert.Equal(7500m, list[0].Monto);
        Assert.Equal("Farmacity", list[0].Descripcion);
        Assert.Equal("mercadopago", list[0].Origen);
        Assert.Null(list[0].Categoria);
        Assert.Equal("ARS", list[0].Moneda);
    }
}
