using ATLAS.Core.Ai;

namespace ATLAS.Core.Commands;

public class AiTranslateCommand : ICommand
{
    private readonly IAiProvider _aiProvider;

    public const string CommandId = "ai.translate";

    public AiTranslateCommand(IAiProvider aiProvider)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
    }

    public string Id => CommandId;

    public string Name => "Traducir con IA";

    public string Description => "Traduce un texto al español o al idioma que se le solicite explícitamente en el texto.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("text", typeof(string), "Texto a traducir", IsRequired: true)
    ];

    public Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null || !parameters.TryGetValue("text", out var rawText) || string.IsNullOrWhiteSpace(rawText?.ToString()))
        {
            return Task.FromResult(CommandResult.Failure("El parámetro 'text' es obligatorio para traducir."));
        }

        var text = rawText.ToString()!.Trim();
        var prompt = $"Traducí el siguiente texto al español de forma natural. Si el texto ya está en español, traducilo al inglés. Mantené el formato original:\n\n{text}";

        var stream = _aiProvider.AskStreamAsync(prompt, cancellationToken);
        return Task.FromResult(CommandResult.Success((object)stream));
    }
}
