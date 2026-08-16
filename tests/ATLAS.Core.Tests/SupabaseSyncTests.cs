using System.Threading;
using System.Threading.Tasks;
using ATLAS.Core.Commands;
using ATLAS.Core.Integrations.Supabase;
using Xunit;

namespace ATLAS.Core.Tests;

public class SupabaseSyncTests
{
    private class FakeSupabaseSyncService : ISupabaseSyncService
    {
        public bool IsConfiguredResult { get; set; } = true;
        public SupabaseSyncResult SyncResult { get; set; } = new(true, "OK");

        public bool IsConfigured() => IsConfiguredResult;

        public Task<SupabaseSyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(SyncResult);
        }
    }

    [Fact]
    public async Task SupabaseSyncCommand_WhenConfiguredAndSuccessful_ReturnsSuccessResult()
    {
        // Arrange
        var fakeService = new FakeSupabaseSyncService
        {
            SyncResult = new SupabaseSyncResult(
                IsSuccess: true,
                Message: "Sincronización completada.",
                NotesSynced: 5,
                GoalsSynced: 2,
                HabitsSynced: 3,
                HabitEventsSynced: 8,
                RoadmapsSynced: 1,
                MilestonesSynced: 4,
                TransactionsSynced: 10
            )
        };

        var command = new SupabaseSyncCommand(fakeService);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Contains("5 notas", result.Data.ToString());
    }

    [Fact]
    public async Task SupabaseSyncCommand_WhenFails_ReturnsFailureResult()
    {
        // Arrange
        var fakeService = new FakeSupabaseSyncService
        {
            SyncResult = new SupabaseSyncResult(
                IsSuccess: false,
                Message: "Error de red al conectar con Supabase."
            )
        };

        var command = new SupabaseSyncCommand(fakeService);

        // Act
        var result = await command.ExecuteAsync();

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Error de red al conectar con Supabase.", result.ErrorMessage);
    }
}
