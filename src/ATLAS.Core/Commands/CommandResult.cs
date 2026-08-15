namespace ATLAS.Core.Commands;

/// <summary>
/// Represents the result of a command execution.
/// </summary>
public class CommandResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }
    public object? Data { get; init; }

    public static CommandResult Success(object? data = null) => new()
    {
        IsSuccess = true,
        Data = data
    };

    public static CommandResult<T> Success<T>(T data) => new()
    {
        IsSuccess = true,
        Data = data,
        TypedData = data
    };

    public static CommandResult Failure(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };

    public static CommandResult<T> Failure<T>(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
    public static Task<CommandResult> SuccessTask(object? data = null) => Task.FromResult(Success(data));

    public static Task<CommandResult> FailureTask(string errorMessage) => Task.FromResult(Failure(errorMessage));
}

/// <summary>
/// Represents a strongly-typed result of a command execution.
/// </summary>
/// <typeparam name="T">Type of the returned payload data.</typeparam>
public class CommandResult<T> : CommandResult
{
    public T? TypedData { get; init; }
}
