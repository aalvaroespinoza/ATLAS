using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

namespace ATLAS.Core.Tests;

public class GoalCommandsTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;

    public GoalCommandsTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_goal_cmds_test_{Guid.NewGuid():N}.db");
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

    private async Task<(CommandRegistry Registry, GoalsRepository Repository)> SetupEnvironmentAsync()
    {
        var initializer = new DatabaseInitializer(_connectionString);
        await initializer.InitializeAsync();

        var repository = new GoalsRepository(_connectionString);
        var registry = new CommandRegistry();

        registry.Register(new GoalCreateCommand(repository));
        registry.Register(new GoalUpdateProgressCommand(repository));

        return (registry, repository);
    }

    [Fact]
    public async Task GoalCreateCommand_ShouldCreateActiveGoal_WhenInvokedViaRegistry()
    {
        // Arrange
        var (registry, repository) = await SetupEnvironmentAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["title"] = "Aprender WinUI 3 y C#",
            ["description"] = "Dominar la arquitectura de apps de escritorio",
            ["target_date"] = "2026-12-31T00:00:00Z"
        };

        // Act
        var result = await registry.ExecuteAsync(GoalCreateCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var created = Assert.IsType<Goal>(result.Data);
        Assert.Equal("Aprender WinUI 3 y C#", created.Title);
        Assert.Equal("active", created.Status);
        Assert.Equal(0, created.Progress);

        // Verify in DB
        var inDb = await repository.GetByIdAsync(created.Id);
        Assert.NotNull(inDb);
        Assert.Equal("Aprender WinUI 3 y C#", inDb.Title);
        Assert.Equal("active", inDb.Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GoalCreateCommand_ShouldFail_WhenTitleIsMissing(string? emptyTitle)
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentAsync();
        var parameters = emptyTitle != null
            ? new Dictionary<string, object?> { ["title"] = emptyTitle }
            : new Dictionary<string, object?>();

        // Act
        var result = await registry.ExecuteAsync(GoalCreateCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("El parámetro 'title' es obligatorio", result.ErrorMessage);
    }

    [Fact]
    public async Task GoalUpdateProgressCommand_ShouldUpdateProgress_WhenInvokedViaRegistry()
    {
        // Arrange
        var (registry, repository) = await SetupEnvironmentAsync();
        var goal = await repository.CreateAsync(new Goal
        {
            Id = "goal-learn-csharp",
            Title = "Aprender C#",
            Status = "active",
            Progress = 10
        });

        var parameters = new Dictionary<string, object?>
        {
            ["goal_id"] = goal.Id,
            ["progress"] = 45
        };

        // Act
        var result = await registry.ExecuteAsync(GoalUpdateProgressCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var updated = Assert.IsType<Goal>(result.Data);
        Assert.Equal(45, updated.Progress);
        Assert.Equal("active", updated.Status);

        var inDb = await repository.GetByIdAsync(goal.Id);
        Assert.NotNull(inDb);
        Assert.Equal(45, inDb.Progress);
        Assert.Equal("active", inDb.Status);
    }

    [Fact]
    public async Task GoalUpdateProgressCommand_ShouldAutoComplete_WhenProgressReaches100WithoutExplicitStatus()
    {
        // Arrange
        var (registry, repository) = await SetupEnvironmentAsync();
        var goal = await repository.CreateAsync(new Goal
        {
            Id = "goal-marathon",
            Title = "Correr Maratón",
            Status = "active",
            Progress = 80
        });

        var parameters = new Dictionary<string, object?>
        {
            ["goal_id"] = goal.Id,
            ["progress"] = 100
        };

        // Act
        var result = await registry.ExecuteAsync(GoalUpdateProgressCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var updated = Assert.IsType<Goal>(result.Data);
        Assert.Equal(100, updated.Progress);
        Assert.Equal("completed", updated.Status);
    }

    [Fact]
    public async Task GoalUpdateProgressCommand_ShouldRespectExplicitStatus()
    {
        // Arrange
        var (registry, repository) = await SetupEnvironmentAsync();
        var goal = await repository.CreateAsync(new Goal
        {
            Id = "goal-pause",
            Title = "Aprender piano",
            Status = "active",
            Progress = 20
        });

        var parameters = new Dictionary<string, object?>
        {
            ["goal_id"] = goal.Id,
            ["progress"] = 25,
            ["status"] = "paused"
        };

        // Act
        var result = await registry.ExecuteAsync(GoalUpdateProgressCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var updated = Assert.IsType<Goal>(result.Data);
        Assert.Equal(25, updated.Progress);
        Assert.Equal("paused", updated.Status);
    }

    [Fact]
    public async Task GoalUpdateProgressCommand_ShouldFail_WhenGoalNotFound()
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["goal_id"] = "non-existent-id",
            ["progress"] = 50
        };

        // Act
        var result = await registry.ExecuteAsync(GoalUpdateProgressCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("No se encontró la meta", result.ErrorMessage);
    }

    [Fact]
    public void GoalCommands_Metadata_ShouldBeCorrect()
    {
        // Arrange
        var repository = new GoalsRepository(_connectionString);
        var createCmd = new GoalCreateCommand(repository);
        var updateCmd = new GoalUpdateProgressCommand(repository);

        // Assert create
        Assert.Equal("goal.create", createCmd.Id);
        Assert.Equal("Crear Meta", createCmd.Name);
        Assert.False(string.IsNullOrWhiteSpace(createCmd.Description));
        Assert.Contains(createCmd.InputSchema, p => p.Name == "title" && p.IsRequired);

        // Assert update
        Assert.Equal("goal.update_progress", updateCmd.Id);
        Assert.Equal("Actualizar Progreso de Meta", updateCmd.Name);
        Assert.False(string.IsNullOrWhiteSpace(updateCmd.Description));
        Assert.Contains(updateCmd.InputSchema, p => p.Name == "goal_id" && p.IsRequired);
        Assert.Contains(updateCmd.InputSchema, p => p.Name == "progress" && p.IsRequired);
    }
}
