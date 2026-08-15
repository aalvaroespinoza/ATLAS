using ATLAS.Core.Entities;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

namespace ATLAS.Core.Tests;

public class GoalsRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;

    public GoalsRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_goals_test_{Guid.NewGuid():N}.db");
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

    private async Task<GoalsRepository> CreateRepositoryAsync()
    {
        var initializer = new DatabaseInitializer(_connectionString);
        await initializer.InitializeAsync();
        return new GoalsRepository(_connectionString);
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistGoal_AndBeRetrievableById()
    {
        // Arrange
        var repository = await CreateRepositoryAsync();
        var targetDate = DateTimeOffset.UtcNow.AddMonths(3);

        var goal = new Goal
        {
            Id = "goal-marathon",
            Title = "Correr maratón 42k",
            Description = "Plan de entrenamiento sub-4 horas",
            Status = "active",
            Progress = 15,
            TargetDate = targetDate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        var created = await repository.CreateAsync(goal);
        var retrieved = await repository.GetByIdAsync("goal-marathon");

        // Assert
        Assert.NotNull(created);
        Assert.NotNull(retrieved);
        Assert.Equal("goal-marathon", retrieved.Id);
        Assert.Equal("Correr maratón 42k", retrieved.Title);
        Assert.Equal("Plan de entrenamiento sub-4 horas", retrieved.Description);
        Assert.Equal("active", retrieved.Status);
        Assert.Equal(15, retrieved.Progress);
        Assert.NotNull(retrieved.TargetDate);
        Assert.Equal(targetDate.Date, retrieved.TargetDate.Value.Date);
    }

    [Fact]
    public async Task GetAllAsync_ShouldFilterByStatus_AndOrderByDateDesc()
    {
        // Arrange
        var repository = await CreateRepositoryAsync();
        var now = DateTimeOffset.UtcNow;

        await repository.CreateAsync(new Goal
        {
            Id = "g-1",
            Title = "Aprender Rust",
            Status = "active",
            Progress = 30,
            CreatedAt = now.AddDays(-2)
        });

        await repository.CreateAsync(new Goal
        {
            Id = "g-2",
            Title = "Lanzar v1",
            Status = "completed",
            Progress = 100,
            CreatedAt = now.AddDays(-1)
        });

        await repository.CreateAsync(new Goal
        {
            Id = "g-3",
            Title = "Mejorar postura",
            Status = "active",
            Progress = 50,
            CreatedAt = now
        });

        // Act
        var allActive = await repository.GetAllAsync("active");
        var allGoals = await repository.GetAllAsync();

        // Assert
        Assert.Equal(2, allActive.Count);
        Assert.Equal("g-3", allActive[0].Id); // más reciente primero
        Assert.Equal("g-1", allActive[1].Id);

        Assert.Equal(3, allGoals.Count);
        Assert.Equal("g-3", allGoals[0].Id);
        Assert.Equal("g-2", allGoals[1].Id);
        Assert.Equal("g-1", allGoals[2].Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProgressAndStatus()
    {
        // Arrange
        var repository = await CreateRepositoryAsync();
        var goal = new Goal
        {
            Id = "goal-update-test",
            Title = "Leer 12 libros",
            Status = "active",
            Progress = 10,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await repository.CreateAsync(goal);

        // Act
        var updatedGoal = new Goal
        {
            Id = "goal-update-test",
            Title = "Leer 12 libros en el año",
            Description = "Modificado",
            Status = "completed",
            Progress = 100,
            CreatedAt = goal.CreatedAt
        };
        await repository.UpdateAsync(updatedGoal);

        var result = await repository.GetByIdAsync("goal-update-test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Leer 12 libros en el año", result.Title);
        Assert.Equal("Modificado", result.Description);
        Assert.Equal("completed", result.Status);
        Assert.Equal(100, result.Progress);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotExists_ReturnsNull()
    {
        // Arrange
        var repository = await CreateRepositoryAsync();

        // Act
        var result = await repository.GetByIdAsync("non-existing-id");

        // Assert
        Assert.Null(result);
    }
}
