using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;
using Microsoft.Data.Sqlite;

namespace ATLAS.Core.Tests;

public class NotesRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;

    public NotesRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"atlas_test_{Guid.NewGuid():N}.db");
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
            // Ignore temp file cleanup exceptions
        }
        GC.SuppressFinalize(this);
    }

    private async Task<NotesRepository> CreateInitializedRepositoryAsync()
    {
        var initializer = new DatabaseInitializer(_connectionString);
        await initializer.InitializeAsync();
        return new NotesRepository(_connectionString);
    }

    [Fact]
    public void DatabaseConfig_ShouldPointToLocalAppDataAtlas()
    {
        // Act
        var dbPath = DatabaseConfig.GetDefaultDatabasePath();
        var connStr = DatabaseConfig.GetDefaultConnectionString();

        // Assert
        Assert.Contains("ATLAS", dbPath);
        Assert.EndsWith("atlas.db", dbPath);
        Assert.Equal($"Data Source={dbPath}", connStr);
    }

    [Fact]
    public async Task CreateAsync_And_GetRecentAsync_ShouldSaveAndRetrieveNote()
    {
        // Arrange
        var repository = await CreateInitializedRepositoryAsync();
        var note = new Note
        {
            Id = "note-123",
            Title = "Mi Título",
            Content = "Mi primera nota de prueba en ATLAS",
            Type = "note",
            Tags = "csharp, winui",
            CreatedAt = DateTimeOffset.UtcNow,
            Source = "quick_capture"
        };

        // Act
        var created = await repository.CreateAsync(note);
        var recent = await repository.GetRecentAsync(10);

        // Assert
        Assert.NotNull(created);
        Assert.Single(recent);
        Assert.Equal("note-123", recent[0].Id);
        Assert.Equal("Mi Título", recent[0].Title);
        Assert.Equal("Mi primera nota de prueba en ATLAS", recent[0].Content);
        Assert.Equal("note", recent[0].Type);
        Assert.Equal("csharp, winui", recent[0].Tags);
        Assert.Equal("quick_capture", recent[0].Source);
    }

    [Fact]
    public async Task MigrateNotesTable_ShouldPreserveExistingDataAndAddColumnsWithDefaults()
    {
        // 1. Simular base de datos existente con el esquema antiguo (solo 4 columnas)
        await using (var conn = new SqliteConnection(_connectionString))
        {
            await conn.OpenAsync();
            const string oldSchemaSql = """
                CREATE TABLE notes (
                    id TEXT PRIMARY KEY,
                    content TEXT NOT NULL,
                    created_at TEXT NOT NULL,
                    source TEXT NOT NULL
                );
                """;
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = oldSchemaSql;
            await cmd.ExecuteNonQueryAsync();

            // Insertar fila antigua
            cmd.CommandText = """
                INSERT INTO notes (id, content, created_at, source)
                VALUES ('old-1', 'Nota existente antes de migración', '2026-08-15T12:00:00.0000000Z', 'legacy');
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        // 2. Ejecutar DatabaseInitializer (migración idempotente sin borrar datos)
        var initializer = new DatabaseInitializer(_connectionString);
        await initializer.InitializeAsync();

        var repository = new NotesRepository(_connectionString);

        // 3. Verificar que la nota antigua sobrevivió y tiene valores por defecto
        var recent = await repository.GetRecentAsync(10);
        Assert.Single(recent);
        Assert.Equal("old-1", recent[0].Id);
        Assert.Equal("Nota existente antes de migración", recent[0].Content);
        Assert.Equal("legacy", recent[0].Source);
        Assert.Null(recent[0].Title);
        Assert.Null(recent[0].Tags);
        Assert.Equal("note", recent[0].Type);

        // 4. Verificar que se pueden insertar nuevas notas con los campos extendidos
        var newNote = new Note
        {
            Id = "new-2",
            Title = "Nueva Nota",
            Content = "Contenido nuevo",
            Type = "idea",
            Tags = "arquitectura, ai",
            CreatedAt = DateTimeOffset.UtcNow,
            Source = "launcher"
        };
        await repository.CreateAsync(newNote);

        var updatedRecent = await repository.GetRecentAsync(10);
        Assert.Equal(2, updatedRecent.Count);
        Assert.Equal("new-2", updatedRecent[0].Id);
        Assert.Equal("Nueva Nota", updatedRecent[0].Title);
        Assert.Equal("idea", updatedRecent[0].Type);
        Assert.Equal("arquitectura, ai", updatedRecent[0].Tags);
    }

    [Fact]
    public async Task GetRecentAsync_ShouldReturnNotesOrderedByDateDescending()
    {
        // Arrange
        var repository = await CreateInitializedRepositoryAsync();
        var now = DateTimeOffset.UtcNow;

        var note1 = new Note { Id = "1", Content = "Nota 1 (más vieja)", CreatedAt = now.AddMinutes(-10), Source = "test" };
        var note2 = new Note { Id = "2", Content = "Nota 2 (intermedia)", CreatedAt = now.AddMinutes(-5), Source = "test" };
        var note3 = new Note { Id = "3", Content = "Nota 3 (más nueva)", CreatedAt = now, Source = "test" };

        await repository.CreateAsync(note1);
        await repository.CreateAsync(note2);
        await repository.CreateAsync(note3);

        // Act
        var recent = await repository.GetRecentAsync(10);

        // Assert
        Assert.Equal(3, recent.Count);
        Assert.Equal("3", recent[0].Id);
        Assert.Equal("2", recent[1].Id);
        Assert.Equal("1", recent[2].Id);
    }

    [Fact]
    public async Task GetRecentAsync_ShouldRespectLimit()
    {
        // Arrange
        var repository = await CreateInitializedRepositoryAsync();
        for (int i = 1; i <= 5; i++)
        {
            await repository.CreateAsync(new Note
            {
                Id = $"note-{i}",
                Content = $"Nota {i}",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i),
                Source = "test"
            });
        }

        // Act
        var recent = await repository.GetRecentAsync(2);

        // Assert
        Assert.Equal(2, recent.Count);
        Assert.Equal("note-5", recent[0].Id);
        Assert.Equal("note-4", recent[1].Id);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNoteIsNull()
    {
        // Arrange
        var repository = await CreateInitializedRepositoryAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.CreateAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_ShouldThrow_WhenNoteIdIsNullOrWhitespace(string invalidId)
    {
        // Arrange
        var repository = await CreateInitializedRepositoryAsync();
        var note = new Note { Id = invalidId, Content = "Texto" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => repository.CreateAsync(note));
    }
}
