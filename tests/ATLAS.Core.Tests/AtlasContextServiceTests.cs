using ATLAS.Core.Context;
using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;
using ATLAS.Core.Security;
using Xunit;

namespace ATLAS.Core.Tests;

public class AtlasContextServiceTests
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

    private class FakeGoalRepository : IGoalRepository
    {
        public List<Goal> Goals { get; } = new();

        public Task<Goal> CreateAsync(Goal goal, CancellationToken cancellationToken = default)
        {
            Goals.Add(goal);
            return Task.FromResult(goal);
        }

        public Task<IReadOnlyList<Goal>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
        {
            var q = Goals.AsEnumerable();
            if (!string.IsNullOrEmpty(status)) q = q.Where(g => g.Status == status);
            return Task.FromResult<IReadOnlyList<Goal>>(q.ToList());
        }

        public Task<Goal?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Goals.FirstOrDefault(g => g.Id == id));

        public Task<Goal> UpdateAsync(Goal goal, CancellationToken cancellationToken = default)
        {
            var g = Goals.FirstOrDefault(x => x.Id == goal.Id);
            if (g != null)
            {
                var idx = Goals.IndexOf(g);
                Goals[idx] = goal;
            }
            return Task.FromResult(goal);
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

        public Task<IReadOnlyList<Roadmap>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Roadmap>>(Roadmaps.ToList());
        }

        public Task<Roadmap?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Roadmaps.FirstOrDefault(r => r.Id == id));

        public Task<Roadmap?> GetByGoalIdAsync(string goalId, CancellationToken cancellationToken = default)
            => Task.FromResult(Roadmaps.FirstOrDefault(r => r.GoalId == goalId));

        public Task<RoadmapMilestone?> GetMilestoneByIdAsync(string milestoneId, CancellationToken cancellationToken = default)
        {
            foreach (var r in Roadmaps)
            {
                var m = r.Milestones.FirstOrDefault(x => x.Id == milestoneId);
                if (m != null) return Task.FromResult<RoadmapMilestone?>(m);
            }
            return Task.FromResult<RoadmapMilestone?>(null);
        }

        public Task UpdateMilestoneStatusAsync(string milestoneId, string status, DateTimeOffset? completedAt, CancellationToken cancellationToken = default)
        {
            foreach (var r in Roadmaps)
            {
                var m = r.Milestones.FirstOrDefault(x => x.Id == milestoneId);
                if (m != null)
                {
                    var idx = r.Milestones.IndexOf(m);
                    r.Milestones[idx] = new RoadmapMilestone
                    {
                        Id = m.Id,
                        RoadmapId = m.RoadmapId,
                        Title = m.Title,
                        Status = status,
                        OrderIndex = m.OrderIndex,
                        CompletedAt = completedAt ?? (status == "completed" ? DateTimeOffset.UtcNow : null)
                    };
                }
            }
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            Roadmaps.RemoveAll(r => r.Id == id);
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
            => Task.FromResult<IReadOnlyList<Note>>(Notes.OrderByDescending(n => n.CreatedAt).Take(count).ToList());

        public Task<IReadOnlyList<Note>> SearchAsync(string? query, int count = 20, CancellationToken cancellationToken = default)
        {
            var q = Notes.AsEnumerable();
            if (!string.IsNullOrEmpty(query)) q = q.Where(n => (n.Content != null && n.Content.Contains(query)) || (n.Title != null && n.Title.Contains(query)));
            return Task.FromResult<IReadOnlyList<Note>>(q.Take(count).ToList());
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

        public Task<Transaction?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Transactions.FirstOrDefault(t => t.Id == id));

        public Task<Transaction?> GetByExternalIdAsync(string idExterno, CancellationToken cancellationToken = default)
            => Task.FromResult(Transactions.FirstOrDefault(t => t.IdExterno == idExterno));

        public Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit = 50, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Transaction>>(Transactions.OrderByDescending(t => t.Fecha).Take(limit).ToList());

        public Task<int> CreateBatchAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
        {
            int count = 0;
            foreach (var tx in transactions)
            {
                if (string.IsNullOrEmpty(tx.IdExterno) || !Transactions.Any(t => t.IdExterno == tx.IdExterno))
                {
                    Transactions.Add(tx);
                    count++;
                }
            }
            return Task.FromResult(count);
        }

        public Task<bool> UpdateCategoryAsync(string id, string? categoria, string? subcategoria = null, CancellationToken cancellationToken = default)
        {
            var tx = Transactions.FirstOrDefault(t => t.Id == id);
            if (tx != null)
            {
                var idx = Transactions.IndexOf(tx);
                Transactions[idx] = new Transaction
                {
                    Id = tx.Id,
                    IdExterno = tx.IdExterno,
                    Monto = tx.Monto,
                    Moneda = tx.Moneda,
                    Descripcion = tx.Descripcion,
                    Tipo = tx.Tipo,
                    Categoria = categoria,
                    Origen = tx.Origen,
                    Fecha = tx.Fecha,
                    Metadata = tx.Metadata,
                    CreatedAt = tx.CreatedAt
                };
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }

    private class FakeSecretVault : ISecretVault
    {
        public Dictionary<string, string> Secrets { get; } = new();

        public void DeleteSecret(string key) => Secrets.Remove(key);

        public string? GetSecret(string key) => Secrets.TryGetValue(key, out var v) ? v : null;

        public bool HasSecret(string key) => Secrets.ContainsKey(key);

        public void SetSecret(string key, string secret) => Secrets[key] = secret;
    }

    [Fact]
    public async Task GetCurrentContextAsync_EmptyData_ReturnsValidSnapshot()
    {
        var service = new AtlasContextService(
            new FakeHabitRepository(),
            new FakeGoalRepository(),
            new FakeRoadmapRepository(),
            new FakeNoteRepository(),
            new FakeTransactionRepository(),
            new FakeSecretVault()
        );

        var snapshot = await service.GetCurrentContextAsync();

        Assert.NotNull(snapshot);
        Assert.NotNull(snapshot.TimeOfDayGreeting);
        Assert.Equal(0, snapshot.Habits.TotalCount);
        Assert.Null(snapshot.NextMilestone);
        Assert.Empty(snapshot.GoalsInFocus);
        Assert.Empty(snapshot.RecentActivity);
        Assert.False(snapshot.Integrations.HasAi);
    }

    [Fact]
    public async Task GetCurrentContextAsync_WithHabitsGoalsRoadmaps_ReturnsCorrectContext()
    {
        var habitRepo = new FakeHabitRepository();
        var goalRepo = new FakeGoalRepository();
        var roadmapRepo = new FakeRoadmapRepository();
        var noteRepo = new FakeNoteRepository();
        var txRepo = new FakeTransactionRepository();
        var vault = new FakeSecretVault();

        vault.Secrets[SecretKeys.GeminiApiKey] = "gemini-test-key";

        var habit = new Habit { Id = "h1", Name = "Lectura Diaria", Frequency = "daily" };
        await habitRepo.CreateAsync(habit);
        await habitRepo.RecordEventAsync(new HabitEvent { HabitId = "h1", CompletedAt = DateTimeOffset.UtcNow });

        var goal = new Goal { Id = "g1", Title = "Aprender Rust", Status = "active", Progress = 40, TargetDate = DateTimeOffset.UtcNow.AddDays(10) };
        await goalRepo.CreateAsync(goal);

        var roadmap = new Roadmap { Id = "rm1", GoalId = "g1", Title = "Mastering Rust" };
        var m1 = new RoadmapMilestone { Id = "m1", RoadmapId = "rm1", Title = "Capítulo 1", Status = "completed", OrderIndex = 0 };
        var m2 = new RoadmapMilestone { Id = "m2", RoadmapId = "rm1", Title = "Capítulo 2", Status = "pending", OrderIndex = 1 };
        await roadmapRepo.CreateAsync(roadmap, new[] { m1, m2 });

        await noteRepo.CreateAsync(new Note { Id = "n1", Content = "Nota de prueba #rust", Tags = "#rust" });
        await txRepo.CreateAsync(new Transaction { Id = "t1", Descripcion = "Libro", Monto = 5000, Tipo = "expense", Fecha = DateTimeOffset.UtcNow });

        var service = new AtlasContextService(habitRepo, goalRepo, roadmapRepo, noteRepo, txRepo, vault);
        var snapshot = await service.GetCurrentContextAsync();

        Assert.Equal(1, snapshot.Habits.TotalCount);
        Assert.Equal(1, snapshot.Habits.CompletedTodayCount);
        Assert.Equal(0, snapshot.Habits.PendingTodayCount);

        Assert.NotNull(snapshot.NextMilestone);
        Assert.Equal("Capítulo 2", snapshot.NextMilestone.MilestoneTitle);
        Assert.Equal("Mastering Rust", snapshot.NextMilestone.RoadmapTitle);

        Assert.Single(snapshot.GoalsInFocus);
        Assert.Equal("Aprender Rust", snapshot.GoalsInFocus[0].Title);

        Assert.True(snapshot.Finance.MonthlyExpenses == 5000);
        Assert.True(snapshot.Integrations.HasAi);
        Assert.True(snapshot.RecentActivity.Count >= 3);
    }

    [Fact]
    public async Task GetReducedContextAsync_ReturnsEssentialSummary()
    {
        var habitRepo = new FakeHabitRepository();
        var goalRepo = new FakeGoalRepository();
        var roadmapRepo = new FakeRoadmapRepository();
        var noteRepo = new FakeNoteRepository();
        var txRepo = new FakeTransactionRepository();
        var vault = new FakeSecretVault();

        var habit = new Habit { Id = "h1", Name = "Entrenamiento", Frequency = "daily" };
        await habitRepo.CreateAsync(habit); // Pending today

        var service = new AtlasContextService(habitRepo, goalRepo, roadmapRepo, noteRepo, txRepo, vault);
        var reduced = await service.GetReducedContextAsync();

        Assert.NotNull(reduced);
        Assert.Equal(1, reduced.HabitsPendingCount);
        Assert.Equal(0, reduced.HabitsCompletedCount);
        Assert.NotEmpty(reduced.PrioritySignals);
    }

    [Fact]
    public async Task GetEntityContextAsync_WithEntities_ReturnsAccurateDetails()
    {
        var service = new AtlasContextService(
            new FakeHabitRepository(),
            new FakeGoalRepository(),
            new FakeRoadmapRepository(),
            new FakeNoteRepository(),
            new FakeTransactionRepository(),
            new FakeSecretVault()
        );

        var note = new Note { Id = "n1", Content = "Contenido de la nota", Tags = "#idea #tech" };
        var noteCtx = await service.GetEntityContextAsync(note);
        Assert.Equal("Note", noteCtx.EntityType);
        Assert.Contains("#idea", noteCtx.Tags);

        var goal = new Goal { Id = "g1", Title = "Comprar auto", Status = "active", Progress = 50 };
        var goalCtx = await service.GetEntityContextAsync(goal);
        Assert.Equal("Goal", goalCtx.EntityType);
        Assert.Equal("active", goalCtx.Status);
    }

    [Fact]
    public async Task BuildAiSystemContextPromptAsync_GeneratesStructuredPrompt()
    {
        var habitRepo = new FakeHabitRepository();
        await habitRepo.CreateAsync(new Habit { Id = "h1", Name = "Meditar", Frequency = "daily" });

        var service = new AtlasContextService(
            habitRepo,
            new FakeGoalRepository(),
            new FakeRoadmapRepository(),
            new FakeNoteRepository(),
            new FakeTransactionRepository(),
            new FakeSecretVault()
        );

        var prompt = await service.BuildAiSystemContextPromptAsync();

        Assert.Contains("CONTEXTO OPERATIVO DEL USUARIO", prompt);
        Assert.Contains("Meditar", prompt);
        Assert.Contains("Finanzas del Mes", prompt);
    }
}
