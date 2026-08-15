using ATLAS.Core.Commands;

namespace ATLAS.Core.Tests;

public class CommandRegistryTests
{
    private sealed class MockCommand : ICommand
    {
        public string Id { get; init; } = "test.mock";
        public string Name { get; init; } = "Mock Command";
        public string Description { get; init; } = "A mock command for testing.";
        public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; init; } =
        [
            new("message", typeof(string), "A test message parameter", IsRequired: true)
        ];

        public Func<IReadOnlyDictionary<string, object?>?, CancellationToken, Task<CommandResult>>? Handler { get; init; }

        public Task<CommandResult> ExecuteAsync(
            IReadOnlyDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default)
        {
            if (Handler != null)
            {
                return Handler(parameters, cancellationToken);
            }

            var message = parameters != null && parameters.TryGetValue("message", out var val)
                ? val?.ToString()
                : "default";

            return CommandResult.SuccessTask($"Executed: {message}");
        }
    }

    [Fact]
    public void Register_ShouldAddCommand_WhenValid()
    {
        // Arrange
        var registry = new CommandRegistry();
        var mockCommand = new MockCommand();

        // Act
        registry.Register(mockCommand);

        // Assert
        var found = registry.TryGetCommand("test.mock", out var retrieved);
        Assert.True(found);
        Assert.NotNull(retrieved);
        Assert.Equal("test.mock", retrieved.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_ShouldThrowArgumentException_WhenIdIsNullOrWhitespace(string invalidId)
    {
        // Arrange
        var registry = new CommandRegistry();
        var mockCommand = new MockCommand { Id = invalidId };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => registry.Register(mockCommand));
    }

    [Fact]
    public void Register_ShouldThrowInvalidOperationException_WhenDuplicateId()
    {
        // Arrange
        var registry = new CommandRegistry();
        var mockCommand1 = new MockCommand { Id = "test.duplicate" };
        var mockCommand2 = new MockCommand { Id = "test.duplicate" };

        // Act
        registry.Register(mockCommand1);

        // Assert
        Assert.Throws<InvalidOperationException>(() => registry.Register(mockCommand2));
    }

    [Fact]
    public void TryGetCommand_ShouldBeCaseInsensitive()
    {
        // Arrange
        var registry = new CommandRegistry();
        var mockCommand = new MockCommand { Id = "Test.Mock.Case" };
        registry.Register(mockCommand);

        // Act
        var found = registry.TryGetCommand("test.mock.case", out var retrieved);

        // Assert
        Assert.True(found);
        Assert.NotNull(retrieved);
        Assert.Equal("Test.Mock.Case", retrieved.Id);
    }

    [Fact]
    public void GetAllCommands_ShouldReturnAllRegisteredCommands()
    {
        // Arrange
        var registry = new CommandRegistry();
        registry.Register(new MockCommand { Id = "cmd.1" });
        registry.Register(new MockCommand { Id = "cmd.2" });

        // Act
        var commands = registry.GetAllCommands();

        // Assert
        Assert.Equal(2, commands.Count);
        Assert.Contains(commands, c => c.Id == "cmd.1");
        Assert.Contains(commands, c => c.Id == "cmd.2");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldExecuteCommandSuccessfully()
    {
        // Arrange
        var registry = new CommandRegistry();
        var mockCommand = new MockCommand { Id = "test.echo" };
        registry.Register(mockCommand);

        var parameters = new Dictionary<string, object?>
        {
            ["message"] = "Hello ATLAS"
        };

        // Act
        var result = await registry.ExecuteAsync("test.echo", parameters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccess);
        Assert.Equal("Executed: Hello ATLAS", result.Data);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenCommandNotFound()
    {
        // Arrange
        var registry = new CommandRegistry();

        // Act
        var result = await registry.ExecuteAsync("non.existent.command");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenCommandThrowsException()
    {
        // Arrange
        var registry = new CommandRegistry();
        var failingCommand = new MockCommand
        {
            Id = "test.failing",
            Handler = (_, _) => throw new InvalidOperationException("Simulated failure inside command.")
        };
        registry.Register(failingCommand);

        // Act
        var result = await registry.ExecuteAsync("test.failing");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("Simulated failure inside command.", result.ErrorMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenExecutionIsCancelled()
    {
        // Arrange
        var registry = new CommandRegistry();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var cancellableCommand = new MockCommand
        {
            Id = "test.cancellable",
            Handler = (_, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(CommandResult.Success());
            }
        };
        registry.Register(cancellableCommand);

        // Act
        var result = await registry.ExecuteAsync("test.cancellable", cancellationToken: cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsSuccess);
        Assert.Contains("cancelled", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
