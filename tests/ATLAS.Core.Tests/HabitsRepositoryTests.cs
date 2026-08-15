using ATLAS.Core.Entities;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

namespace ATLAS.Core.Tests;

public class HabitsRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;

    public HabitsRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_habits_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_testDbPath}";
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
            // Ignore cleanup exceptions
        }
        GC.SuppressFinalize(this);
    }

    private async Task<HabitsRepository> CreateRepositoryAsync()
    {
        var initializer = new DatabaseInitializer(_connectionString);
        await initializer.InitializeAsync();
        return new HabitsRepository(_connectionString);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistHabit_AndBeRetrievableById()
    {
        // Arrange
        var repository = await CreateRepositoryAsync();
        var habit = new Habit
        {
            Id = "habit-water",
            Name = "Tomar 2L de agua",
            Description = "Hidratación diaria",
            Frequency = "daily",
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var created = await repository.CreateAsync(habit);
        var retrieved = await repository.GetByIdAsync("habit-water");

        // Assert
        Assert.NotNull(created);
        Assert.NotNull(retrieved);
        Assert.Equal("habit-water", retrieved.Id);
        Assert.Equal("Tomar 2L de agua", retrieved.Name);
        Assert.Equal("Hidratación diaria", retrieved.Description);
        Assert.Equal("daily", retrieved.Frequency);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllHabits()
    {
        // Arrange
        var repository = await CreateRepositoryAsync();

        await repository.CreateAsync(new Habit
        {
            Id = "h-1",
            Name = "Meditar",
            Frequency = "daily"
        });

        await repository.CreateAsync(new Habit
        {
            Id = "h-2",
            Name = "Gimnasio",
            Frequency = "days:1,3,5"
        });

        // Act
        var all = await repository.GetAllAsync();

        // Assert
        Assert.Equal(2, all.Count);
        Assert.Contains(all, h => h.Id == "h-1" && h.Frequency == "daily");
        Assert.Contains(all, h => h.Id == "h-2" && h.Frequency == "days:1,3,5");
    }

    [Fact]
    public async Task RecordEventAsync_ShouldPersistHabitEvent_AndBeRetrievableByHabitId()
    {
        // Arrange
        var repository = await CreateRepositoryAsync();
        var habit = new Habit
        {
            Id = "h-read",
            Name = "Leer 20 mins",
            Frequency = "daily"
        };
        await repository.CreateAsync(habit);

        var now = DateTimeOffset.UtcNow;
        var event1 = new HabitEvent
        {
            Id = "ev-1",
            HabitId = "h-read",
            CompletedAt = now.AddHours(-2),
            Note = "Capítulo 3 terminado"
        };
        var event2 = new HabitEvent
        {
            Id = "ev-2",
            HabitId = "h-read",
            CompletedAt = now,
            Note = "Capítulo 4 terminado"
        };

        // Act
        await repository.RecordEventAsync(event1);
        await repository.RecordEventAsync(event2);

        var events = await repository.GetEventsAsync(habitId: "h-read");

        // Assert
        Assert.Equal(2, events.Count);
        Assert.Equal("ev-2", events[0].Id); // más reciente primero
        Assert.Equal("ev-1", events[1].Id);
        Assert.Equal("Capítulo 4 terminado", events[0].Note);
    }

    [Fact]
    public async Task GetEventsAsync_ShouldFilterBySinceThreshold()
    {
        // Arrange
        var repository = await CreateRepositoryAsync();
        var habit = new Habit { Id = "h-walk", Name = "Caminata diaria" };
        await repository.CreateAsync(habit);

        var now = DateTimeOffset.UtcNow;
        await repository.RecordEventAsync(new HabitEvent
        {
            Id = "ev-old",
            HabitId = "h-walk",
            CompletedAt = now.AddDays(-5)
        });

        await repository.RecordEventAsync(new HabitEvent
        {
            Id = "ev-recent",
            HabitId = "h-walk",
            CompletedAt = now.AddHours(-1)
        });

        // Act
        var eventsSinceYesterday = await repository.GetEventsAsync(since: now.AddDays(-1));

        // Assert
        Assert.Single(eventsSinceYesterday);
        Assert.Equal("ev-recent", eventsSinceYesterday[0].Id);
    }
}
