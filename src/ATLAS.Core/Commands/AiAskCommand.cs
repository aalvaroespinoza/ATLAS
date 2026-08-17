using ATLAS.Core.Ai;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to ask a prompt or question to the registered IAiProvider.
/// </summary>
public class AiAskCommand : ICommand
{
    private readonly IAiProvider _aiProvider;

    public const string CommandId = "ai.ask";

    public string Id => CommandId;

    public string Name => "Preguntar a la IA";

    public string Description => "Envía una pregunta o prompt a la IA y devuelve la respuesta generada.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("prompt", typeof(string), "Pregunta o instrucción para la IA", IsRequired: true),
        new("stream", typeof(bool), "Si es verdadero, devuelve un IAsyncEnumerable<string>", IsRequired: false)
    ];

    public AiAskCommand(IAiProvider aiProvider)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
    }

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null ||
            !parameters.TryGetValue("prompt", out var rawPrompt) ||
            rawPrompt == null ||
            string.IsNullOrWhiteSpace(rawPrompt.ToString()))
        {
            return CommandResult.Failure("El parámetro 'prompt' es obligatorio y no puede estar vacío.");
        }

        var prompt = rawPrompt.ToString()!.Trim();
        var stream = parameters.TryGetValue("stream", out var streamVal) && streamVal is bool b && b;

        try
        {
            if (stream)
            {
                var enumerable = _aiProvider.AskStreamAsync(prompt, cancellationToken);
                return CommandResult.Success(enumerable);
            }
            else
            {
                var answer = await _aiProvider.AskAsync(prompt, cancellationToken).ConfigureAwait(false);
                return CommandResult.Success(answer);
            }
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}
