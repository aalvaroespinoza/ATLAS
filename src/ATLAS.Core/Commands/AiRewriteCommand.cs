using ATLAS.Core.Ai;

namespace ATLAS.Core.Commands;

public class AiRewriteCommand : ICommand
{
    private readonly IAiProvider _aiProvider;

    public const string CommandId = "ai.rewrite";

    public AiRewriteCommand(IAiProvider aiProvider)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
    }

    public string Id => CommandId;

    public string Name => "Reescribir con IA";

    public string Description => "Reescribe un texto para mejorar su claridad, impacto o tono.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("text", typeof(string), "Texto a reescribir", IsRequired: true)
    ];

    public Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.TryGetValue("text", out var rawText) || string.IsNullOrWhiteSpace(rawText?.ToString()))
        {
            return Task.FromResult(CommandResult.Failure("El parámetro 'text' es obligatorio para reescribir."));
        }

        var text = rawText.ToString()!.Trim();
        var prompt = $"Reescribí el siguiente texto para que sea más claro, profesional y de mayor impacto, corrigiendo cualquier error gramatical u ortográfico, pero manteniendo su intención original:\n\n{text}";

        var stream = _aiProvider.AskStreamAsync(prompt, cancellationToken);
        return Task.FromResult(CommandResult.Success((object)stream));
    }
}
