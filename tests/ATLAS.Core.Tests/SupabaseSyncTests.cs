using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Integrations.Supabase;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;
using Xunit;

namespace ATLAS.Core.Tests;

public class SupabaseSyncTests
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

    private class FakeSupabaseSyncService : ISupabaseSyncService
    {
        public bool IsConfiguredResult { get; set; } = true;
        public SupabaseSyncResult SyncResult { get; set; } = new(true, "OK");

        public bool IsConfigured() => IsConfiguredResult;

        public Task<SupabaseSyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SyncResult);
        }
    }

    private class FakeNoteRepository : INoteRepository
    {
        public Task<Note> CreateAsync(Note note, CancellationToken cancellationToken = default) => Task.FromResult(note);
        public Task<IReadOnlyList<Note>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Note>>(new List<Note>
            {
                new() { Id = "note-1", Content = "Test Note 1", Title = "Title 1" }
            });
        public Task<IReadOnlyList<Note>> SearchAsync(string? query, int count = 20, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Note>>(new List<Note>());
    }

    private class FakeGoalRepository : IGoalRepository
    {
        public Task<Goal> CreateAsync(Goal goal, CancellationToken cancellationToken = default) => Task.FromResult(goal);
        public Task<Goal?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<Goal?>(null);
        public Task<IReadOnlyList<Goal>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Goal>>(new List<Goal>());
        public Task<Goal> UpdateAsync(Goal goal, CancellationToken cancellationToken = default) => Task.FromResult(goal);
    }

    private class FakeHabitRepository : IHabitRepository
    {
        public Task<Habit> CreateAsync(Habit habit, CancellationToken cancellationToken = default) => Task.FromResult(habit);
        public Task<Habit?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<Habit?>(null);
        public Task<IReadOnlyList<Habit>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Habit>>(new List<Habit>());
        public Task<HabitEvent> RecordEventAsync(HabitEvent habitEvent, CancellationToken cancellationToken = default) =>
            Task.FromResult(habitEvent);
        public Task<IReadOnlyList<HabitEvent>> GetEventsAsync(string? habitId = null, DateTimeOffset? since = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<HabitEvent>>(new List<HabitEvent>());
    }

    private class FakeRoadmapRepository : IRoadmapRepository
    {
        public Task<Roadmap?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<Roadmap?>(null);
        public Task<Roadmap?> GetByGoalIdAsync(string goalId, CancellationToken cancellationToken = default) => Task.FromResult<Roadmap?>(null);
        public Task<IReadOnlyList<Roadmap>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Roadmap>>(new List<Roadmap>());
        public Task CreateAsync(Roadmap roadmap, IEnumerable<RoadmapMilestone>? milestones = null, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task AddMilestoneAsync(RoadmapMilestone milestone, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task UpdateMilestoneStatusAsync(string milestoneId, string status, DateTimeOffset? completedAt, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task<RoadmapMilestone?> GetMilestoneByIdAsync(string milestoneId, CancellationToken cancellationToken = default) =>
            Task.FromResult<RoadmapMilestone?>(null);
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private class FakeTransactionRepository : ITransactionRepository
    {
        public Task<Transaction> CreateAsync(Transaction transaction, CancellationToken cancellationToken = default) => Task.FromResult(transaction);
        public Task<Transaction?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult<Transaction?>(null);
        public Task<Transaction?> GetByExternalIdAsync(string idExterno, CancellationToken cancellationToken = default) => Task.FromResult<Transaction?>(null);
        public Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Transaction>>(new List<Transaction>());
        public Task<int> CreateBatchAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    [Fact]
    public async Task SupabaseSyncCommand_WhenConfiguredAndSuccessful_ReturnsSuccessResult()
    {
        var fakeService = new FakeSupabaseSyncService
        {
            SyncResult = new SupabaseSyncResult(
                IsSuccess: true,
                Message: "Sincronización completada.",
                NotesSynced: 5,
                GoalsSynced: 2,
                HabitsSynced: 3,
                HabitEventsSynced: 8,
                RoadmapsSynced: 1,
                MilestonesSynced: 4,
                TransactionsSynced: 10
            )
        };

        var command = new SupabaseSyncCommand(fakeService);
        var result = await command.ExecuteAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Contains("5 notas", result.Data.ToString());
    }

    [Fact]
    public async Task SupabaseSyncCommand_WhenFails_ReturnsFailureResult()
    {
        var fakeService = new FakeSupabaseSyncService
        {
            SyncResult = new SupabaseSyncResult(
                IsSuccess: false,
                Message: "Error de red al conectar con Supabase."
            )
        };

        var command = new SupabaseSyncCommand(fakeService);
        var result = await command.ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal("Error de red al conectar con Supabase.", result.ErrorMessage);
    }

    [Fact]
    public async Task SupabaseAuthService_SignInWithPassword_StoresTokensAndUserId()
    {
        var vault = new InMemorySecretVault();
        vault.SetSecret(SecretKeys.SupabaseUrl, "https://mock.supabase.co");
        vault.SetSecret(SecretKeys.SupabaseAnonKey, "mock-anon-key");

        var responseJson = """
        {
            "access_token": "mock-jwt-token",
            "token_type": "bearer",
            "expires_in": 3600,
            "expires_at": 1893456000,
            "refresh_token": "mock-refresh-token",
            "user": {
                "id": "11111111-2222-3333-4444-555555555555",
                "email": "alvaro@test.com"
            }
        }
        """;

        var handler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal(HttpMethod.Post, req.Method);
            Assert.Contains("/auth/v1/token", req.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });
        });

        var client = new HttpClient(handler);
        var authService = new SupabaseAuthService(client, vault);

        var result = await authService.SignInWithPasswordAsync("alvaro@test.com", "password123");

        Assert.True(result.IsSuccess);
        Assert.Equal("11111111-2222-3333-4444-555555555555", result.UserId);
        Assert.True(authService.IsAuthenticated());
        Assert.Equal("alvaro@test.com", authService.GetUserEmail());
        Assert.Equal("11111111-2222-3333-4444-555555555555", authService.GetUserId());
        Assert.Equal("mock-jwt-token", vault.GetSecret(SecretKeys.SupabaseAccessToken));
        Assert.Equal("mock-refresh-token", vault.GetSecret(SecretKeys.SupabaseRefreshToken));
    }

    [Fact]
    public async Task SupabaseSyncService_WhenNotAuthenticated_ReturnsFailureAskingForLogin()
    {
        var vault = new InMemorySecretVault();
        vault.SetSecret(SecretKeys.SupabaseUrl, "https://mock.supabase.co");
        vault.SetSecret(SecretKeys.SupabaseAnonKey, "mock-anon-key");

        var client = new HttpClient();
        var authService = new SupabaseAuthService(client, vault);
        var syncService = new SupabaseSyncService(
            client,
            vault,
            authService,
            new FakeNoteRepository(),
            new FakeGoalRepository(),
            new FakeHabitRepository(),
            new FakeRoadmapRepository(),
            new FakeTransactionRepository());

        var result = await syncService.SyncAllAsync();

        Assert.False(result.IsSuccess);
        Assert.Contains("inicio de sesión", result.Message);
    }

    [Fact]
    public async Task SupabaseSyncService_WhenAuthenticated_SendsBearerTokenAndUserId()
    {
        var vault = new InMemorySecretVault();
        vault.SetSecret(SecretKeys.SupabaseUrl, "https://mock.supabase.co");
        vault.SetSecret(SecretKeys.SupabaseAnonKey, "mock-anon-key");
        vault.SetSecret(SecretKeys.SupabaseAccessToken, "valid-jwt-token");
        vault.SetSecret(SecretKeys.SupabaseRefreshToken, "valid-refresh-token");
        vault.SetSecret(SecretKeys.SupabaseUserId, "11111111-2222-3333-4444-555555555555");
        vault.SetSecret(SecretKeys.SupabaseTokenExpiresAt, (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600).ToString());

        var capturedRequests = new List<HttpRequestMessage>();

        var handler = new MockHttpMessageHandler(req =>
        {
            capturedRequests.Add(req);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("[]", Encoding.UTF8, "application/json")
            });
        });

        var client = new HttpClient(handler);
        var authService = new SupabaseAuthService(client, vault);
        var syncService = new SupabaseSyncService(
            client,
            vault,
            authService,
            new FakeNoteRepository(),
            new FakeGoalRepository(),
            new FakeHabitRepository(),
            new FakeRoadmapRepository(),
            new FakeTransactionRepository());

        var result = await syncService.SyncAllAsync();

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(capturedRequests);
        var notesReq = capturedRequests[0];
        Assert.Equal("Bearer", notesReq.Headers.Authorization?.Scheme);
        Assert.Equal("valid-jwt-token", notesReq.Headers.Authorization?.Parameter);
    }
}
