namespace ATLAS.Core.Commands;

/// <summary>
/// Registry for discovering, registering and executing commands.
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// Registers a new command.
    /// </summary>
    void Register(ICommand command);

    /// <summary>
    /// Attempts to retrieve a command by its unique ID.
    /// </summary>
    bool TryGetCommand(string id, out ICommand? command);

    /// <summary>
    /// Retrieves all currently registered commands.
    /// </summary>
    IReadOnlyCollection<ICommand> GetAllCommands();

    /// <summary>
    /// Executes a registered command by its ID.
    /// </summary>
    Task<CommandResult> ExecuteAsync(
        string commandId,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);
}
