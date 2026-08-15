namespace ATLAS.Core.Commands;

/// <summary>
/// Describes an input parameter accepted by a command.
/// </summary>
public sealed record CommandParameterDescriptor(
    string Name,
    Type ParameterType,
    string Description,
    bool IsRequired = true,
    object? DefaultValue = null
);
