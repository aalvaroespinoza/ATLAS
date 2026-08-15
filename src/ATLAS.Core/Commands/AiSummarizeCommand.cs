using ATLAS.Core.Ai;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to summarize text using the registered IAiProvider.
/// </summary>
public class AiSummarizeCommand : ICommand
{
    private readonly IAiProvider _aiProvider;

    public const string CommandId = "ai.summarize";

    public AiSummarizeCommand(IAiProvider aiProvider)
    {
        _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
    }

    public string Id => CommandId;

    public string Name => "Resumir con IA";

    public string Description => "Genera un resumen conciso del texto proporcionado utilizando el proveedor de IA configurado.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("text", typeof(string), "Texto a resumir", IsRequired: true)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null ||
            !parameters.TryGetValue("text", out var rawText) ||
            rawText == null ||
            string.IsNullOrWhiteSpace(rawText.ToString()))
        {
            return CommandResult.Failure("El parámetro 'text' es obligatorio y no puede estar vacío.");
        }

        var text = rawText.ToString()!.Trim();

        try
        {
            var summary = await _aiProvider.SummarizeAsync(text, cancellationToken).ConfigureAwait(false);
            return CommandResult.Success(summary);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}
