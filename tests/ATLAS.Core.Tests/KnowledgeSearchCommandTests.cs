using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

namespace ATLAS.Core.Tests;

public class KnowledgeSearchCommandTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;

    public KnowledgeSearchCommandTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_search_test_{Guid.NewGuid():N}.db");
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

    private async Task<(CommandRegistry Registry, NotesRepository Repository)> SetupEnvironmentWithSeedNotesAsync()
    {
        var initializer = new DatabaseInitializer(_connectionString);
        await initializer.InitializeAsync();

        var repository = new NotesRepository(_connectionString);
        var registry = new CommandRegistry();
        var searchCommand = new KnowledgeSearchCommand(repository);
        registry.Register(searchCommand);

        var now = DateTimeOffset.UtcNow;

        // Seed 4 distinct notes
        await repository.CreateAsync(new Note
        {
            Id = "note-arch",
            Title = "Arquitectura de ATLAS",
            Content = "El núcleo del sistema desacoplado de la UI.",
            Type = "doc",
            Tags = "arquitectura, winui",
            CreatedAt = now.AddMinutes(-30),
            Source = "docs"
        });

        await repository.CreateAsync(new Note
        {
            Id = "note-rust",
            Title = "Aprendizaje de Rust",
            Content = "Conceptos de Ownership, Borrowing y memoria segura sin Garbage Collector.",
            Type = "note",
            Tags = "programacion, sistemas",
            CreatedAt = now.AddMinutes(-20),
            Source = "study"
        });

        await repository.CreateAsync(new Note
        {
            Id = "note-recipe",
            Title = "Receta Pizza Napolitana",
            Content = "Harina 00, agua 65%, levadura fresca, sal marina.",
            Type = "note",
            Tags = "cocina, italia",
            CreatedAt = now.AddMinutes(-10),
            Source = "quick_capture"
        });

        await repository.CreateAsync(new Note
        {
            Id = "note-docker",
            Title = "Infraestructura Local",
            Content = "Docker compose para servicios auxiliares.",
            Type = "guide",
            Tags = "devops, docker, contenedores",
            CreatedAt = now,
            Source = "work"
        });

        return (registry, repository);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMatchByTitle()
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentWithSeedNotesAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["query"] = "Arquitectura"
        };

        // Act
        var result = await registry.ExecuteAsync(KnowledgeSearchCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var notes = Assert.IsAssignableFrom<IReadOnlyList<Note>>(result.Data);
        Assert.Single(notes);
        Assert.Equal("note-arch", notes[0].Id);
        Assert.Equal("Arquitectura de ATLAS", notes[0].Title);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMatchByTag()
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentWithSeedNotesAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["query"] = "cocina"
        };

        // Act
        var result = await registry.ExecuteAsync(KnowledgeSearchCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var notes = Assert.IsAssignableFrom<IReadOnlyList<Note>>(result.Data);
        Assert.Single(notes);
        Assert.Equal("note-recipe", notes[0].Id);
        Assert.Equal("cocina, italia", notes[0].Tags);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMatchByContent()
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentWithSeedNotesAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["query"] = "Borrowing"
        };

        // Act
        var result = await registry.ExecuteAsync(KnowledgeSearchCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var notes = Assert.IsAssignableFrom<IReadOnlyList<Note>>(result.Data);
        Assert.Single(notes);
        Assert.Equal("note-rust", notes[0].Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentWithSeedNotesAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["query"] = "dOcKeR"
        };

        // Act
        var result = await registry.ExecuteAsync(KnowledgeSearchCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var notes = Assert.IsAssignableFrom<IReadOnlyList<Note>>(result.Data);
        Assert.Single(notes);
        Assert.Equal("note-docker", notes[0].Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnRecentNotes_WhenQueryIsEmptyOrNull()
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentWithSeedNotesAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["query"] = ""
        };

        // Act
        var result = await registry.ExecuteAsync(KnowledgeSearchCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var notes = Assert.IsAssignableFrom<IReadOnlyList<Note>>(result.Data);
        Assert.Equal(4, notes.Count);
        // Debe venir ordenado cronológicamente descendente
        Assert.Equal("note-docker", notes[0].Id);
        Assert.Equal("note-recipe", notes[1].Id);
        Assert.Equal("note-rust", notes[2].Id);
        Assert.Equal("note-arch", notes[3].Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRespectLimit()
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentWithSeedNotesAsync();
        var parameters = new Dictionary<string, object?>
        {
            ["query"] = "",
            ["limit"] = 2
        };

        // Act
        var result = await registry.ExecuteAsync(KnowledgeSearchCommand.CommandId, parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess, result.ErrorMessage);
        var notes = Assert.IsAssignableFrom<IReadOnlyList<Note>>(result.Data);
        Assert.Equal(2, notes.Count);
        Assert.Equal("note-docker", notes[0].Id);
        Assert.Equal("note-recipe", notes[1].Id);
    }

    [Fact]
    public async Task KnowledgeSearchCommand_Metadata_ShouldBeCorrect()
    {
        // Arrange
        var (registry, _) = await SetupEnvironmentWithSeedNotesAsync();

        // Act
        var found = registry.TryGetCommand(KnowledgeSearchCommand.CommandId, out var command);

        // Assert
        Assert.True(found);
        Assert.NotNull(command);
        Assert.Equal("knowledge.search", command.Id);
        Assert.Equal("Buscar Conocimiento", command.Name);
        Assert.False(string.IsNullOrWhiteSpace(command.Description));
        Assert.Contains(command.InputSchema, p => p.Name == "query");
        Assert.Contains(command.InputSchema, p => p.Name == "limit");
    }
}
