using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Core.Events;
using ATLAS.Core.Repositories;
using Xunit;

namespace ATLAS.Core.Tests;

public class AtlasEventBusTests
{
    private class FakeNoteRepository : INoteRepository
    {
        public List<Note> Notes { get; } = new();

        public Task<Note> CreateAsync(Note note, CancellationToken cancellationToken = default)
        {
            Notes.Add(note);
            return Task.FromResult(note);
        }

        public Task<IReadOnlyList<Note>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Note>>(Notes);

        public Task<IReadOnlyList<Note>> SearchAsync(string? query, int count = 20, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Note>>(Notes);
    }

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
            => Task.FromResult<IReadOnlyList<HabitEvent>>(Events);

        public Task<HabitEvent> RecordEventAsync(HabitEvent habitEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(habitEvent);
            return Task.FromResult(habitEvent);
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
            => Task.FromResult<IReadOnlyList<Transaction>>(Transactions);

        public Task<int> CreateBatchAsync(IEnumerable<Transaction> transactions, CancellationToken cancellationToken = default)
        {
            Transactions.AddRange(transactions);
            return Task.FromResult(Transactions.Count);
        }

        public Task<bool> UpdateCategoryAsync(string id, string? categoria, string? subcategoria = null, CancellationToken cancellationToken = default)
            => Task.FromResult(true);
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
            => Task.FromResult<IReadOnlyList<Roadmap>>(Roadmaps);

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
                        CompletedAt = completedAt
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

    private class FakeGoalRepository : IGoalRepository
    {
        public List<Goal> Goals { get; } = new();

        public Task<Goal> CreateAsync(Goal goal, CancellationToken cancellationToken = default)
        {
            Goals.Add(goal);
            return Task.FromResult(goal);
        }

        public Task<Goal?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            => Task.FromResult(Goals.FirstOrDefault(g => g.Id == id));

        public Task<IReadOnlyList<Goal>> GetAllAsync(string? status = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Goal>>(Goals);

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

    [Fact]
    public async Task PublishAsync_WithSubscribers_DispatchesEventCorrectly()
    {
        var bus = new AtlasEventBus();
        NoteCapturedEvent? received = null;

        using var sub = bus.Subscribe<NoteCapturedEvent>(e =>
        {
            received = e;
            return Task.CompletedTask;
        });

        var noteEvent = new NoteCapturedEvent("n1", "Mi Nota", "Contenido", "#tag", "quick_capture", "ev1", DateTimeOffset.UtcNow);
        await bus.PublishAsync(noteEvent);

        Assert.NotNull(received);
        Assert.Equal("n1", received.NoteId);
        Assert.Equal("Mi Nota", received.Title);
        Assert.Equal("Contenido", received.Content);
    }

    [Fact]
    public async Task Subscribe_UnsubscribeToken_StopsReceivingEvents()
    {
        var bus = new AtlasEventBus();
        int callCount = 0;

        var token = bus.Subscribe<HabitCompletedEvent>(e =>
        {
            callCount++;
            return Task.CompletedTask;
        });

        var ev = new HabitCompletedEvent("h1", "Lectura", null, DateTimeOffset.UtcNow, "ui", "ev1", DateTimeOffset.UtcNow);
        await bus.PublishAsync(ev);
        Assert.Equal(1, callCount);

        token.Dispose();

        await bus.PublishAsync(ev);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public async Task SubscribeAll_ReceivesAllEventTypes()
    {
        var bus = new AtlasEventBus();
        var receivedEvents = new List<IAtlasEvent>();

        using var sub = bus.SubscribeAll(e =>
        {
            receivedEvents.Add(e);
            return Task.CompletedTask;
        });

        await bus.PublishAsync(new NoteCapturedEvent("n1", null, "Nota", null, "ui", "e1", DateTimeOffset.UtcNow));
        await bus.PublishAsync(new HabitCompletedEvent("h1", "Habit", null, DateTimeOffset.UtcNow, "ui", "e2", DateTimeOffset.UtcNow));
        await bus.PublishAsync(new TransactionCreatedEvent("t1", "Café", 1500, "expense", null, "manual", "e3", DateTimeOffset.UtcNow));

        Assert.Equal(3, receivedEvents.Count);
    }

    [Fact]
    public async Task PublishAsync_HandlerThrowsException_DoesNotBlockOtherSubscribers()
    {
        var bus = new AtlasEventBus();
        bool secondHandlerExecuted = false;

        bus.Subscribe<NoteCapturedEvent>(_ => throw new InvalidOperationException("Simulated error in subscriber"));
        bus.Subscribe<NoteCapturedEvent>(_ =>
        {
            secondHandlerExecuted = true;
            return Task.CompletedTask;
        });

        var ev = new NoteCapturedEvent("n1", null, "Nota", null, "ui", "e1", DateTimeOffset.UtcNow);
        await bus.PublishAsync(ev);

        Assert.True(secondHandlerExecuted);
    }

    [Fact]
    public async Task CaptureNoteCommand_PublishesNoteCapturedEvent()
    {
        var bus = new AtlasEventBus();
        NoteCapturedEvent? captured = null;
        using var sub = bus.Subscribe<NoteCapturedEvent>(e =>
        {
            captured = e;
            return Task.CompletedTask;
        });

        var repo = new FakeNoteRepository();
        var command = new CaptureNoteCommand(repo, bus);

        var result = await command.ExecuteAsync(new Dictionary<string, object?>
        {
            ["content"] = "Aprender Event Bus",
            ["title"] = "Arquitectura",
            ["tags"] = "#csharp #events"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal("Arquitectura", captured.Title);
        Assert.Equal("Aprender Event Bus", captured.Content);
        Assert.Equal("#csharp #events", captured.Tags);
    }

    [Fact]
    public async Task HabitCompleteCommand_PublishesHabitCompletedEvent()
    {
        var bus = new AtlasEventBus();
        HabitCompletedEvent? completedEvent = null;
        using var sub = bus.Subscribe<HabitCompletedEvent>(e =>
        {
            completedEvent = e;
            return Task.CompletedTask;
        });

        var repo = new FakeHabitRepository();
        await repo.CreateAsync(new Habit { Id = "h1", Name = "Meditar 10min", Frequency = "daily" });

        var command = new HabitCompleteCommand(repo, bus);
        var result = await command.ExecuteAsync(new Dictionary<string, object?>
        {
            ["habit_id"] = "h1",
            ["note"] = "Sesión matutina"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(completedEvent);
        Assert.Equal("h1", completedEvent.HabitId);
        Assert.Equal("Meditar 10min", completedEvent.HabitName);
        Assert.Equal("Sesión matutina", completedEvent.Note);
    }

    [Fact]
    public async Task FinanceAddTransactionCommand_PublishesTransactionCreatedEvent()
    {
        var bus = new AtlasEventBus();
        TransactionCreatedEvent? txEvent = null;
        using var sub = bus.Subscribe<TransactionCreatedEvent>(e =>
        {
            txEvent = e;
            return Task.CompletedTask;
        });

        var repo = new FakeTransactionRepository();
        var command = new FinanceAddTransactionCommand(repo, bus);

        var result = await command.ExecuteAsync(new Dictionary<string, object?>
        {
            ["amount"] = 3500m,
            ["description"] = "Almuerzo",
            ["category"] = "Comida",
            ["type"] = "expense"
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(txEvent);
        Assert.Equal("Almuerzo", txEvent.Description);
        Assert.Equal(3500m, txEvent.Amount);
        Assert.Equal("Comida", txEvent.Category);
    }

    [Fact]
    public async Task RoadmapCompleteMilestoneCommand_PublishesRoadmapMilestoneCompletedEvent()
    {
        var bus = new AtlasEventBus();
        RoadmapMilestoneCompletedEvent? milestoneEvent = null;
        using var sub = bus.Subscribe<RoadmapMilestoneCompletedEvent>(e =>
        {
            milestoneEvent = e;
            return Task.CompletedTask;
        });

        var roadmapRepo = new FakeRoadmapRepository();
        var goalRepo = new FakeGoalRepository();

        var rm = new Roadmap { Id = "rm1", Title = "Aprender Rust" };
        var m1 = new RoadmapMilestone { Id = "m1", RoadmapId = "rm1", Title = "Instalar Cargo", Status = "pending", OrderIndex = 0 };
        await roadmapRepo.CreateAsync(rm, new[] { m1 });

        var command = new RoadmapCompleteMilestoneCommand(roadmapRepo, goalRepo, bus);
        var result = await command.ExecuteAsync(new Dictionary<string, object?>
        {
            ["milestone_id"] = "m1",
            ["completed"] = true
        });

        Assert.True(result.IsSuccess);
        Assert.NotNull(milestoneEvent);
        Assert.Equal("rm1", milestoneEvent.RoadmapId);
        Assert.Equal("m1", milestoneEvent.MilestoneId);
        Assert.Equal("Instalar Cargo", milestoneEvent.MilestoneTitle);
        Assert.True(milestoneEvent.Completed);
    }
}
