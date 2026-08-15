namespace ATLAS.Core.Commands;

/// <summary>
/// Contract for executable actions across the ATLAS system.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Unique identifier for the command (e.g. "core.capture.note").
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Human-readable title of the command.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Brief explanation of what the command does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Schema describing expected input parameters.
    /// </summary>
    IReadOnlyList<CommandParameterDescriptor> InputSchema { get; }

    /// <summary>
    /// Executes the command with the provided parameters.
    /// </summary>
    Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);
}
