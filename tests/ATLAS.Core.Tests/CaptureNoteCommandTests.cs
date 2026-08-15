using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

namespace ATLAS.Core.Tests;

public class CaptureNoteCommandTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;

    public CaptureNoteCommandTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_cmd_test_{Guid.NewGuid():N}.db");
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

    private async Task<(CommandRegistry Registry, NotesRepository Repository)> SetupEnvironmentAsync()
    {
        var initializer = new DatabaseInitializer(_connectionString);
        await initializer.InitializeAsync();

        var repository = new NotesRepository(_connectionString);
        var registry = new CommandRegistry();
        var command = new CaptureNoteCommand(repository);
        registry.Register(command);

        return (registry, repository);
    }

    [Fact]
    public async Task ExecuteAsync_ViaCommandRegistry_ShouldCreateAndPersistNoteInDatabase()
    {
        // Arrange
        var (registry, repository) = await SetupEnvironmentAsync();
        var noteText = "Idea rápida capturada desde el Command System";
        var parameters = new Dictionary<string, object?>
        {
            ["content"] = noteText,
            ["source"] = "test_launcher"
        };

        // Act - Invocación a través del CommandRegistry
        var result = await registry.ExecuteAsync(CaptureNoteCommand.CommandId, parameters);

        // Assert - Resultado del Command
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.NotNull(result.Data);

        var createdNote = Assert.IsType<Note>(result.Data);
        Assert.Equal(noteText, createdNote.Content);
        Assert.Equal("test_launcher", createdNote.Source);
        Assert.False(string.IsNullOrWhiteSpace(createdNote.Id));

        // Assert - Verificación en la base de datos SQLite
        var persistedNotes = await repository.GetRecentAsync(5);
        Assert.Single(persistedNotes);
        Assert.Equal(createdNote.Id, persistedNotes[0].Id);
        Assert.Equal(noteText, persistedNotes[0].Content);
        Assert.Equal("test_launcher", persistedNotes[0].Source);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ExecuteAsync_ViaCommandRegistry_ShouldReturnFailure_WhenContentIsInvalid(string? invalidContent)
    {
        // Arrange
        var (registry, repository) = await SetupEnvironmentAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["content"] = invalidContent
        };

        // Act - Invocación a través del CommandRegistry
        var result = await registry.ExecuteAsync(CaptureNoteCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("vacío", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        // Verificamos que no se persistió nada
        var persistedNotes = await repository.GetRecentAsync(5);
        Assert.Empty(persistedNotes);
    }

    [Fact]
    public async Task CaptureNoteCommand_Metadata_ShouldBeCorrect()
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentAsync();

        // Act
        var found = registry.TryGetCommand(CaptureNoteCommand.CommandId, out var command);

        // Assert
        Assert.True(found);
        Assert.NotNull(command);
        Assert.Equal("capture.note", command.Id);
        Assert.False(string.IsNullOrWhiteSpace(command.Name));
        Assert.False(string.IsNullOrWhiteSpace(command.Description));
        Assert.Contains(command.InputSchema, p => p.Name == "content" && p.IsRequired);
    }
}
