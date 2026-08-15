using ATLAS.Core.Entities;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to capture a quick note and persist it to storage.
/// </summary>
public class CaptureNoteCommand : ICommand
{
    private readonly INoteRepository _noteRepository;

    public const string CommandId = "capture.note";

    public CaptureNoteCommand(INoteRepository noteRepository)
    {
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
    }

    public string Id => CommandId;

    public string Name => "Capturar Nota";

    public string Description => "Guarda una nota rápida en el almacenamiento local.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("content", typeof(string), "Contenido de la nota", IsRequired: true),
        new("source", typeof(string), "Origen de la captura", IsRequired: false, DefaultValue: "quick_capture")
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        string? content = null;
        string source = "quick_capture";

        if (parameters != null)
        {
            if (parameters.TryGetValue("content", out var rawContent) && rawContent != null)
            {
                content = rawContent.ToString();
            }

            if (parameters.TryGetValue("source", out var rawSource) && rawSource != null && !string.IsNullOrWhiteSpace(rawSource.ToString()))
            {
                source = rawSource.ToString()!;
            }
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return CommandResult.Failure("El contenido de la nota no puede estar vacío.");
        }

        var note = new Note
        {
            Id = Guid.NewGuid().ToString("N"),
            Content = content.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            Source = source.Trim()
        };

        var created = await _noteRepository.CreateAsync(note, cancellationToken).ConfigureAwait(false);
        return CommandResult.Success(created);
    }
}
