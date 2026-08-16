using ATLAS.Core.Context;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;
using Xunit;

namespace ATLAS.Core.Tests;

public class HomeContextServiceTests
{
    private class FakeHabitRepository : IHabitRepository
    {
        public List<Habit> Habits { get; } = new();
        public List<HabitEvent> Events { get; } = new();

        public Task<Habit> CreateAsync(Habit habit, CancellationToken cancellationToken = default)
        {
            Habits.Add(habit);
            return Task.FromResult(habit);
        }

        public Task<IReadOnlyList<Habit>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Habit>>(Habits);

        public Task<Habit?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Habits.FirstOrDefault(h => h.Id == id));

        public Task<IReadOnlyList<HabitEvent>> GetEventsAsync(string? habitId = null, DateTimeOffset? since = null, CancellationToken cancellationToken = default)
        {
            var query = Events.AsEnumerable();
            if (!string.IsNullOrEmpty(habitId)) query = query.Where(e => e.HabitId == habitId);
            if (since.HasValue) query = query.Where(e => e.CompletedAt >= since.Value);
            return Task.FromResult<IReadOnlyList<HabitEvent>>(query.ToList());
        }

        public Task<HabitEvent> RecordEventAsync(HabitEvent habitEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(habitEvent);
            return Task.FromResult(habitEvent);
        }
    }

    private class FakeRoadmapRepository : IRoadmapRepository
    {
        public List<Roadmap> Roadmaps { get; } = new();

        public Task AddMilestoneAsync(RoadmapMilestone milestone, CancellationToken cancellationToken = default)
        {
            var rm = Roadmaps.FirstOrDefault(r => r.Id == milestone.RoadmapId);
            rm?.Milestones.Add(milestone);
            return Task.CompletedTask;
        }

        public Task CreateAsync(Roadmap roadmap, IEnumerable<RoadmapMilestone>? milestones = null, CancellationToken cancellationToken = default)
        {
            if (milestones != null)
            {
                foreach (var m in milestones) roadmap.Milestones.Add(m);
            }
            Roadmaps.Add(roadmap);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            Roadmaps.RemoveAll(r => r.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Roadmap>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
        {
            var query = Roadmaps.AsEnumerable();
            if (!string.IsNullOrEmpty(status)) query = query.Where(r => r.Status == status);
            return Task.FromResult<IReadOnlyList<Roadmap>>(query.ToList());
        }

        public Task<Roadmap?> GetByGoalIdAsync(string goalId, CancellationToken cancellationToken = default)
            => Task.FromResult(Roadmaps.FirstOrDefault(r => r.GoalId == goalId));

        public Task<Roadmap?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Roadmaps.FirstOrDefault(r => r.Id == id));

        public Task<RoadmapMilestone?> GetMilestoneByIdAsync(string milestoneId, CancellationToken cancellationToken = default)
            => Task.FromResult(Roadmaps.SelectMany(r => r.Milestones).FirstOrDefault(m => m.Id == milestoneId));

        public Task UpdateMilestoneStatusAsync(string milestoneId, string status, DateTimeOffset? completedAt, CancellationToken cancellationToken = default)
        {
            var m = Roadmaps.SelectMany(r => r.Milestones).FirstOrDefault(x => x.Id == milestoneId);
            if (m != null)
            {
                m.Status = status;
                m.CompletedAt = completedAt;
            }
            return Task.CompletedTask;
        }
    }

    private class FakeNoteRepository : INoteRepository
    {
        public List<Note> Notes { get; } = new();

        public Task<Note> CreateAsync(Note note, CancellationToken cancellationToken = default)
        {
            Notes.Add(note);
            return Task.FromResult(note);
        }

        public Task<IReadOnlyList<Note>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Note>>(Notes.Take(count).ToList());

        public Task<Note?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Notes.FirstOrDefault(n => n.Id == id));

        public Task<IReadOnlyList<Note>> SearchAsync(string? query, int count = 20, CancellationToken cancellationToken = default)
        {
            var results = string.IsNullOrEmpty(query) 
                ? Notes 
                : Notes.Where(n => (n.Title != null && n.Title.Contains(query, StringComparison.OrdinalIgnoreCase)) || n.Content.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult<IReadOnlyList<Note>>(results.Take(count).ToList());
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

        public Task<int> CreateBatchAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
        {
            int count = 0;
            foreach (var tx in transactions)
            {
                Transactions.Add(tx);
                count++;
            }
            return Task.FromResult(count);
        }

        public Task<Transaction?> GetByExternalIdAsync(string idExterno, CancellationToken cancellationToken = default)
            => Task.FromResult(Transactions.FirstOrDefault(t => t.IdExterno == idExterno));

        public Task<Transaction?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Transactions.FirstOrDefault(t => t.Id == id));

        public Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Transaction>>(Transactions.Take(limit).ToList());

        public Task<bool> UpdateCategoryAsync(string id, string? categoria, string? subcategoria = null, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
    }

    private class FakeSecretVault : ISecretVault
    {
        private readonly Dictionary<string, string> _secrets = new();

        public void DeleteSecret(string key) => _secrets.Remove(key);
        public string? GetSecret(string key) => _secrets.TryGetValue(key, out var val) ? val : null;
        public bool HasSecret(string key) => _secrets.ContainsKey(key);
        public void SetSecret(string key, string secret) => _secrets[key] = secret;
    }

    [Fact]
    public async Task LoadHomeContextAsync_WithEmptyDatabase_ReturnsGracefulDefaults()
    {
        // Arrange
        var habitRepo = new FakeHabitRepository();
        var roadmapRepo = new FakeRoadmapRepository();
        var noteRepo = new FakeNoteRepository();
        var txRepo = new FakeTransactionRepository();
        var vault = new FakeSecretVault();

        var service = new HomeContextService(habitRepo, roadmapRepo, noteRepo, txRepo, vault);

        // Act
        var result = await service.LoadHomeContextAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Metrics.MaxHabitStreak);
        Assert.Equal(100, result.Metrics.FocusPercentage);
        Assert.Equal(0, result.Metrics.NetBalanceThisMonth);
        Assert.Empty(result.AgendaItems);
        Assert.Empty(result.Habits);
        Assert.Empty(result.Roadmaps);
        Assert.Empty(result.RecentActivity);
    }

    [Fact]
    public async Task LoadHomeContextAsync_WithPopulatedData_CalculatesRealMetrics()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var today = now.Date;

        var habitRepo = new FakeHabitRepository();
        habitRepo.Habits.Add(new Habit { Id = "h1", Name = "Entrenar", Description = "Fuerza", Frequency = "daily", CreatedAt = now.AddDays(-10) });
        habitRepo.Habits.Add(new Habit { Id = "h2", Name = "Leer", Description = "Libros", Frequency = "daily", CreatedAt = now.AddDays(-10) });

        habitRepo.Events.Add(new HabitEvent { Id = "e1", HabitId = "h1", CompletedAt = today, Note = "done" });
        habitRepo.Events.Add(new HabitEvent { Id = "e2", HabitId = "h1", CompletedAt = today.AddDays(-1), Note = "done" });
        habitRepo.Events.Add(new HabitEvent { Id = "e3", HabitId = "h1", CompletedAt = today.AddDays(-2), Note = "done" });

        var roadmapRepo = new FakeRoadmapRepository();
        var roadmap = new Roadmap
        {
            Id = "rm1",
            GoalId = "g1",
            Title = "Proyecto ATLAS",
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now.AddMonths(1),
            Milestones = new List<RoadmapMilestone>
            {
                new() { Id = "m1", RoadmapId = "rm1", Title = "Fase 1", Notes = "Diseño", Status = "completed", CompletedAt = now.AddDays(-1), OrderIndex = 1 },
                new() { Id = "m2", RoadmapId = "rm1", Title = "Fase 2", Notes = "Core", Status = "pending", OrderIndex = 2 }
            }
        };
        roadmapRepo.Roadmaps.Add(roadmap);

        var noteRepo = new FakeNoteRepository();
        noteRepo.Notes.Add(new Note { Id = "n1", Title = "Idea Principal", Content = "Contenido clave", Type = "idea", Tags = "#atlas", CreatedAt = now.AddMinutes(-10), Source = "manual" });

        var txRepo = new FakeTransactionRepository();
        txRepo.Transactions.Add(new Transaction { Id = "tx1", Fecha = now.AddHours(-1), Monto = 15000m, Tipo = "expense", Origen = "mercadopago", Descripcion = "Supermercado", Moneda = "ARS", Categoria = "Alimentos" });
        txRepo.Transactions.Add(new Transaction { Id = "tx2", Fecha = now.AddDays(-2), Monto = 50000m, Tipo = "income", Origen = "banco", Descripcion = "Honorarios", Moneda = "ARS", Categoria = "Ingresos" });

        var vault = new FakeSecretVault();
        vault.SetSecret(SecretKeys.TelegramBotToken, "fake-telegram-token");

        var service = new HomeContextService(habitRepo, roadmapRepo, noteRepo, txRepo, vault);

        // Act
        var result = await service.LoadHomeContextAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Metrics.MaxHabitStreak); // 3 consecutive days
        Assert.Equal("Entrenar", result.Metrics.MaxStreakHabitName);
        Assert.Equal(50, result.Metrics.FocusPercentage); // 1 of 2 habits completed today
        Assert.Equal(35000m, result.Metrics.NetBalanceThisMonth); // 50000 - 15000
        Assert.Equal(2, result.Metrics.MonthlyTransactionCount);

        // Roadmaps
        Assert.Single(result.Roadmaps);
        Assert.Equal(50, result.Roadmaps[0].ProgressPercentage);

        // Agenda Items
        Assert.Equal(2, result.AgendaItems.Count);

        // Integrations
        Assert.True(result.Integrations.IsTelegramConfigured);
        Assert.False(result.Integrations.IsGmailConfigured);

        // Activity
        Assert.NotEmpty(result.RecentActivity);
    }
}
