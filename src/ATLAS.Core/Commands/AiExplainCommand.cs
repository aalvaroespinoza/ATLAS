using ATLAS.Core.Ai;

namespace ATLAS.Core.Commands;

public class AiExplainCommand : ICommand
{
    private readonly IAiProvider _aiProvider;

    public const string CommandId = "ai.explain";

    public AiExplainCommand(IAiProvider aiProvider)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
    }

    public string Id => CommandId;

    public string Name => "Explicar con IA";

    public string Description => "Explica un concepto o texto de manera clara y detallada.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("text", typeof(string), "Texto a explicar", IsRequired: true)
    ];

    public Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.TryGetValue("text", out var rawText) || string.IsNullOrWhiteSpace(rawText?.ToString()))
        {
            return Task.FromResult(CommandResult.Failure("El parámetro 'text' es obligatorio para explicar."));
        }

        var text = rawText.ToString()!.Trim();
        var prompt = $"Explicá de forma clara, didáctica y detallada el siguiente concepto o texto:\n\n{text}";

        var stream = _aiProvider.AskStreamAsync(prompt, cancellationToken);
        return Task.FromResult(CommandResult.Success((object)stream));
    }
}
