using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Integrations;
using ATLAS.Core.Integrations.Gmail;
using ATLAS.Core.Integrations.MercadoPago;
using ATLAS.Core.Integrations.Supabase;
using ATLAS.Core.Integrations.Telegram;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;
using Xunit;

namespace ATLAS.Core.Tests;

public class IntegrationHubTests
{
    private class FakeSecretVault : ISecretVault
    {
        public Dictionary<string, string> Secrets { get; } = new();
        public void DeleteSecret(string key) => Secrets.Remove(key);
        public string? GetSecret(string key) => Secrets.TryGetValue(key, out var v) ? v : null;
        public bool HasSecret(string key) => Secrets.ContainsKey(key);
        public void SetSecret(string key, string secret) => Secrets[key] = secret;
    }

    private class FakeMercadoPagoClient : IMercadoPagoClient
    {
        public List<Transaction> ResultsToReturn { get; set; } = new();
        public bool ShouldFail { get; set; }

        public Task<IReadOnlyList<Transaction>> FetchRecentTransactionsAsync(int limit = 50, CancellationToken cancellationToken = default)
        {
            if (ShouldFail) throw new IntegrationException("mercadopago", "Simulated MP failure");
            return Task.FromResult<IReadOnlyList<Transaction>>(ResultsToReturn.Take(limit).ToList());
        }

        public Task<(bool Success, string Message, TimeSpan Latency)> PingAsync(CancellationToken cancellationToken = default)
        {
            if (ShouldFail) return Task.FromResult((false, "Ping failed", TimeSpan.FromMilliseconds(50)));
            return Task.FromResult((true, "Ping ok", TimeSpan.FromMilliseconds(20)));
        }
    }

    private class FakeTransactionRepository : ITransactionRepository
    {
        public List<Transaction> Transactions { get; } = new();
        public Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            Transactions.Add(transaction);
            return Task.FromResult(transaction);
        }
        public Task<Transaction?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.FirstOrDefault(t => t.Id == id));
        public Task<Transaction?> GetByExternalIdAsync(string idExterno, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.FirstOrDefault(t => t.IdExterno == idExterno));
        public Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Transaction>>(Transactions);
        public Task<int> CreateBatchAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
        {
            Transactions.AddRange(transactions);
            return Task.FromResult(Transactions.Count);
        }
        public Task<bool> UpdateCategoryAsync(string id, string? categoria, string? subcategoria = null, CancellationToken cancellationToken = default) => Task.FromResult(true);
    }

    private class FakeTelegramListenerService : ITelegramListenerService
    {
        public bool IsRunning { get; set; } = true;
        public event Action<TelegramMessage>? MessageReceived { add { } remove { } }
        public Task StartAsync(CancellationToken cancellationToken = default) { IsRunning = true; return Task.CompletedTask; }
        public Task StopAsync(CancellationToken cancellationToken = default) { IsRunning = false; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class FakeGmailClient : IGmailClient
    {
        public Task<string> ExchangeCodeAndSaveTokensAsync(string code, string redirectUri, CancellationToken cancellationToken = default) => Task.FromResult("fake_token");
        public Task<IReadOnlyList<GmailMessageSummary>> ListRecentMessagesAsync(int limit = 10, string? query = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<GmailMessageSummary>>(new List<GmailMessageSummary>());
    }

    private class FakeSupabaseAuthService : ISupabaseAuthService
    {
        public bool IsAuthenticated() => false;
        public string? GetUserId() => null;
        public string? GetUserEmail() => null;
        public Task<SupabaseAuthResult> SignInWithPasswordAsync(string email, string password, CancellationToken cancellationToken = default)
            => Task.FromResult(new SupabaseAuthResult(true, "ok", null, null));
        public Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
        public Task<bool> RefreshSessionAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
        public void SignOut() { }
    }

    private class FakeSupabaseSyncService : ISupabaseSyncService
    {
        public bool IsConfigured() => false;
        public Task<SupabaseSyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new SupabaseSyncResult(true, "ok"));
    }

    [Fact]
    public void IntegrationRegistry_RegistersAndRetrievesAllIntegrations()
    {
        var vault = new FakeSecretVault();
        var mpClient = new FakeMercadoPagoClient();
        var tgService = new FakeTelegramListenerService();
        var gmailClient = new FakeGmailClient();
        var supaAuth = new FakeSupabaseAuthService();
        var supaSync = new FakeSupabaseSyncService();

        var integrations = new IAtlasIntegration[]
        {
            new TelegramIntegration(tgService, vault),
            new GmailIntegration(gmailClient, vault),
            new MercadoPagoIntegration(mpClient, vault),
            new SupabaseIntegration(supaAuth, supaSync, vault)
        };

        var registry = new IntegrationRegistry(integrations);

        Assert.Equal(4, registry.GetAllIntegrations().Count);
        Assert.NotNull(registry.GetIntegration("telegram"));
        Assert.NotNull(registry.GetIntegration("gmail"));
        Assert.NotNull(registry.GetIntegration("mercadopago"));
        Assert.NotNull(registry.GetIntegration("supabase"));

        var typedMp = registry.GetIntegration<MercadoPagoIntegration>("mercadopago");
        Assert.NotNull(typedMp);
        Assert.Equal("Mercado Pago", typedMp.DisplayName);
    }

    [Fact]
    public async Task IntegrationRegistry_CheckAllHealthAsync_CollectsReportsFromAll()
    {
        var vault = new FakeSecretVault();
        var mpClient = new FakeMercadoPagoClient();
        var tgService = new FakeTelegramListenerService();
        var gmailClient = new FakeGmailClient();
        var supaAuth = new FakeSupabaseAuthService();
        var supaSync = new FakeSupabaseSyncService();

        var integrations = new IAtlasIntegration[]
        {
            new TelegramIntegration(tgService, vault),
            new GmailIntegration(gmailClient, vault),
            new MercadoPagoIntegration(mpClient, vault),
            new SupabaseIntegration(supaAuth, supaSync, vault)
        };

        var registry = new IntegrationRegistry(integrations);
        var reports = await registry.CheckAllHealthAsync();

        Assert.Equal(4, reports.Count);
        Assert.True(reports.ContainsKey("telegram"));
        Assert.True(reports.ContainsKey("gmail"));
        Assert.True(reports.ContainsKey("mercadopago"));
        Assert.True(reports.ContainsKey("supabase"));

        // All should report NotConfigured because vault has no secrets
        Assert.Equal(IntegrationHealthStatus.NotConfigured, reports["telegram"].Status);
        Assert.Equal(IntegrationHealthStatus.NotConfigured, reports["gmail"].Status);
        Assert.Equal(IntegrationHealthStatus.NotConfigured, reports["mercadopago"].Status);
        Assert.Equal(IntegrationHealthStatus.NotConfigured, reports["supabase"].Status);
    }

    [Fact]
    public async Task MercadoPagoIntegration_Healthy_ReturnsHealthyReport()
    {
        var vault = new FakeSecretVault();
        vault.SetSecret(SecretKeys.MercadoPagoAccessToken, "APP_USR-test-token");

        var mpClient = new FakeMercadoPagoClient { ShouldFail = false };
        var integration = new MercadoPagoIntegration(mpClient, vault);

        var report = await integration.CheckHealthAsync();

        Assert.Equal(IntegrationHealthStatus.Healthy, report.Status);
        Assert.NotNull(report.Latency);
        Assert.True(integration.IsConfigured);
    }

    [Fact]
    public async Task FinanceSyncMercadoPagoCommand_WithMockClient_ExecutesCleanly()
    {
        var mpClient = new FakeMercadoPagoClient
        {
            ResultsToReturn = new List<Transaction>
            {
                new() { Id = "tx1", Descripcion = "Café", Monto = 2500, Tipo = "expense", Origen = "mercadopago" }
            }
        };

        var txRepo = new FakeTransactionRepository();
        var command = new FinanceSyncMercadoPagoCommand(mpClient, txRepo);

        var result = await command.ExecuteAsync(new Dictionary<string, object?> { ["limit"] = 10 });

        Assert.True(result.IsSuccess);
        Assert.Single(txRepo.Transactions);
        Assert.Equal("Café", txRepo.Transactions[0].Descripcion);
    }
}
