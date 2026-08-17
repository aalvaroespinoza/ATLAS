using ATLAS.Core.Entities;
using ATLAS.Core.Events;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to capture a quick note and persist it to storage.
/// </summary>
public class CaptureNoteCommand : ICommand
{
    private readonly INoteRepository _noteRepository;
    private readonly IAtlasEventBus? _eventBus;

    public const string CommandId = "capture.note";

    public CaptureNoteCommand(INoteRepository noteRepository, IAtlasEventBus? eventBus = null)
    {
        _noteRepository = noteRepository ?? throw new ArgumentNullException(nameof(noteRepository));
        _eventBus = eventBus;
    }

    public string Id => CommandId;

    public string Name => "Capturar Nota";

    public string Description => "Guarda una nota rápida en el almacenamiento local.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("content", typeof(string), "Contenido de la nota", IsRequired: true),
        new("title", typeof(string), "Título opcional de la nota", IsRequired: false),
        new("type", typeof(string), "Tipo de nota", IsRequired: false, DefaultValue: "note"),
        new("tags", typeof(string), "Etiquetas asociadas a la nota", IsRequired: false),
        new("source", typeof(string), "Origen de la captura", IsRequired: false, DefaultValue: "quick_capture")
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        string? content = null;
        string? title = null;
        string type = "note";
        string? tags = null;
        string source = "quick_capture";

        if (parameters != null)
        {
            if (parameters.TryGetValue("content", out var rawContent) && rawContent != null)
            {
                content = rawContent.ToString();
            }

            if (parameters.TryGetValue("title", out var rawTitle) && rawTitle != null)
            {
                title = rawTitle.ToString();
            }

            if (parameters.TryGetValue("type", out var rawType) && rawType != null && !string.IsNullOrWhiteSpace(rawType.ToString()))
            {
                type = rawType.ToString()!;
            }

            if (parameters.TryGetValue("tags", out var rawTags) && rawTags != null)
            {
                tags = rawTags.ToString();
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
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            Content = content.Trim(),
            Type = type.Trim(),
            Tags = string.IsNullOrWhiteSpace(tags) ? null : tags.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
            Source = source.Trim()
        };

        var created = await _noteRepository.CreateAsync(note, cancellationToken).ConfigureAwait(false);

        if (_eventBus != null)
        {
            await _eventBus.PublishAsync(new NoteCapturedEvent(
                NoteId: created.Id,
                Title: created.Title,
                Content: created.Content,
                Tags: created.Tags,
                Source: created.Source,
                EventId: Guid.NewGuid().ToString("N"),
                OccurredAt: created.CreatedAt
            ), cancellationToken).ConfigureAwait(false);
        }

        return CommandResult.Success(created);
    }
}
