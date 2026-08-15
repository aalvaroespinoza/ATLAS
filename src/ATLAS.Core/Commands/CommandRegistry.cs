using System.Collections.Concurrent;

namespace ATLAS.Core.Commands;

/// <summary>
/// Thread-safe in-memory registry and execution dispatcher for commands.
/// </summary>
public class CommandRegistry : ICommandRegistry
{
    private readonly ConcurrentDictionary<string, ICommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ICommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Id))
        {
            throw new ArgumentException("Command ID cannot be null or whitespace.", nameof(command));
        }

        if (!_commands.TryAdd(command.Id, command))
        {
            throw new InvalidOperationException($"A command with ID '{command.Id}' is already registered.");
        }
    }

    public bool TryGetCommand(string id, out ICommand? command)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            command = null;
            return false;
        }

        return _commands.TryGetValue(id, out command);
    }

    public IReadOnlyCollection<ICommand> GetAllCommands()
    {
        return _commands.Values.ToList().AsReadOnly();
    }

    public async Task<CommandResult> ExecuteAsync(
        string commandId,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            return CommandResult.Failure("Command ID cannot be null or empty.");
        }

        if (!TryGetCommand(commandId, out var command) || command is null)
        {
            return CommandResult.Failure($"Command '{commandId}' not found.");
        }

        try
        {
            return await command.ExecuteAsync(parameters, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return CommandResult.Failure($"Command '{commandId}' execution was cancelled.");
        }
        catch (Exception ex)
        {
            return CommandResult.Failure($"Error executing command '{commandId}': {ex.Message}");
        }
    }
}
