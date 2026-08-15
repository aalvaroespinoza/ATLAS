using ATLAS.Core.Entities;
using ATLAS.Storage.Database;
using ATLAS.Storage.Repositories;

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
            Content = "Mi primera nota de prueba en ATLAS",
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
        Assert.Equal("Mi primera nota de prueba en ATLAS", recent[0].Content);
        Assert.Equal("quick_capture", recent[0].Source);
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
