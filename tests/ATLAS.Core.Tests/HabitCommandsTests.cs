using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

namespace ATLAS.Core.Tests;

public class HabitCommandsTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;

    public HabitCommandsTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_habit_cmds_test_{Guid.NewGuid():N}.db");
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

    private async Task<(CommandRegistry Registry, HabitsRepository Repository)> SetupEnvironmentAsync()
    {
        var initializer = new DatabaseInitializer(_connectionString);
        await initializer.InitializeAsync();

        var repository = new HabitsRepository(_connectionString);
        var registry = new CommandRegistry();

        registry.Register(new HabitCreateCommand(repository));
        registry.Register(new HabitCompleteCommand(repository));

        return (registry, repository);
    }

    [Fact]
    public async Task HabitCreateCommand_ShouldCreateHabit_WhenInvokedViaRegistry()
    {
        // Arrange
        var (registry, repository) = await SetupEnvironmentAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["name"] = "Leer 20 páginas",
            ["description"] = "Hábito diario de lectura antes de dormir",
            ["frequency"] = "daily"
        };

        // Act
        var result = await registry.ExecuteAsync(HabitCreateCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var created = Assert.IsType<Habit>(result.Data);
        Assert.Equal("Leer 20 páginas", created.Name);
        Assert.Equal("Hábito diario de lectura antes de dormir", created.Description);
        Assert.Equal("daily", created.Frequency);

        // Verify in DB
        var inDb = await repository.GetByIdAsync(created.Id);
        Assert.NotNull(inDb);
        Assert.Equal("Leer 20 páginas", inDb.Name);
        Assert.Equal("daily", inDb.Frequency);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task HabitCreateCommand_ShouldFail_WhenNameIsMissing(string? emptyName)
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentAsync();
        var parameters = emptyName != null
            ? new Dictionary<string, object?> { ["name"] = emptyName }
            : new Dictionary<string, object?>();

        // Act
        var result = await registry.ExecuteAsync(HabitCreateCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("El parámetro 'name' es obligatorio", result.ErrorMessage);
    }

    [Fact]
    public async Task HabitCompleteCommand_ShouldRecordHabitEvent_WhenInvokedViaRegistry()
    {
        // Arrange
        var (registry, repository) = await SetupEnvironmentAsync();
        var habit = await repository.CreateAsync(new Habit
        {
            Id = "h-workout",
            Name = "Entrenamiento de fuerza",
            Frequency = "days:1,3,5"
        });

        var parameters = new Dictionary<string, object?>
        {
            ["habit_id"] = habit.Id,
            ["note"] = "Rutina pierna completada con éxito"
        };

        // Act
        var result = await registry.ExecuteAsync(HabitCompleteCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var habitEvent = Assert.IsType<HabitEvent>(result.Data);
        Assert.Equal(habit.Id, habitEvent.HabitId);
        Assert.Equal("Rutina pierna completada con éxito", habitEvent.Note);

        // Verify in DB
        var events = await repository.GetEventsAsync(habit.Id);
        Assert.Single(events);
        Assert.Equal(habitEvent.Id, events[0].Id);
        Assert.Equal("Rutina pierna completada con éxito", events[0].Note);
    }

    [Fact]
    public async Task HabitCompleteCommand_ShouldSupportMultipleCompletionsOnSameDay_WithDifferentTimestampsAndNotes()
    {
        // Decisión de diseño: Completar un hábito más de una vez al mismo día es totalmente válido (ej. tomar agua, meditar, pausas activas).
        // Cada registro es un evento atómico inmutable en la tabla habit_events.

        // Arrange
        var (registry, repository) = await SetupEnvironmentAsync();
        var habit = await repository.CreateAsync(new Habit
        {
            Id = "h-water",
            Name = "Tomar 500ml de agua",
            Frequency = "daily"
        });

        var today = DateTimeOffset.UtcNow.Date;
        var morningTime = new DateTimeOffset(today.AddHours(9), TimeSpan.Zero);
        var afternoonTime = new DateTimeOffset(today.AddHours(15), TimeSpan.Zero);

        // Act 1: Completar en la mañana
        var resultMorning = await registry.ExecuteAsync(HabitCompleteCommand.CommandId, new Dictionary<string, object?>
        {
            ["habit_id"] = habit.Id,
            ["completed_at"] = morningTime.ToString("O"),
            ["note"] = "500ml al despertar"
        });

        // Act 2: Completar en la tarde
        var resultAfternoon = await registry.ExecuteAsync(HabitCompleteCommand.CommandId, new Dictionary<string, object?>
        {
            ["habit_id"] = habit.Id,
            ["completed_at"] = afternoonTime.ToString("O"),
            ["note"] = "500ml post almuerzo"
        });

        // Assert
        Assert.True(resultMorning.IsSuccess);
        Assert.True(resultAfternoon.IsSuccess);

        var events = await repository.GetEventsAsync(habit.Id);
        Assert.Equal(2, events.Count);

        // Ordenados más recientes primero
        Assert.Equal("500ml post almuerzo", events[0].Note);
        Assert.Equal("500ml al despertar", events[1].Note);
    }

    [Fact]
    public async Task HabitCompleteCommand_ShouldFail_WhenHabitNotFound()
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["habit_id"] = "non-existent-habit-id"
        };

        // Act
        var result = await registry.ExecuteAsync(HabitCompleteCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("No se encontró el hábito", result.ErrorMessage);
    }

    [Fact]
    public void HabitCommands_Metadata_ShouldBeCorrect()
    {
        // Arrange
        var repository = new HabitsRepository(_connectionString);
        var createCmd = new HabitCreateCommand(repository);
        var completeCmd = new HabitCompleteCommand(repository);

        // Assert create
        Assert.Equal("habit.create", createCmd.Id);
        Assert.Equal("Crear Hábito", createCmd.Name);
        Assert.False(string.IsNullOrWhiteSpace(createCmd.Description));
        Assert.Contains(createCmd.InputSchema, p => p.Name == "name" && p.IsRequired);

        // Assert complete
        Assert.Equal("habit.complete", completeCmd.Id);
        Assert.Equal("Completar Hábito", completeCmd.Name);
        Assert.False(string.IsNullOrWhiteSpace(completeCmd.Description));
        Assert.Contains(completeCmd.InputSchema, p => p.Name == "habit_id" && p.IsRequired);
    }
}
