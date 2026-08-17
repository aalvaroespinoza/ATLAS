using System.Globalization;
using ATLAS.Core.Entities;
using ATLAS.Core.Events;
using ATLAS.Core.Repositories;

namespace ATLAS.Core.Commands;

/// <summary>
/// Command to record a completion event for a habit.
/// </summary>
public class HabitCompleteCommand : ICommand
{
    private readonly IHabitRepository _habitRepository;
    private readonly IAtlasEventBus? _eventBus;

    public const string CommandId = "habit.complete";

    public HabitCompleteCommand(IHabitRepository habitRepository, IAtlasEventBus? eventBus = null)
    {
        _habitRepository = habitRepository ?? throw new ArgumentNullException(nameof(habitRepository));
        _eventBus = eventBus;
    }

    public string Id => CommandId;

    public string Name => "Completar Hábito";

    public string Description => "Registra un evento de cumplimiento (con timestamp y nota opcional) para un hábito.";

    public IReadOnlyList<CommandParameterDescriptor> InputSchema { get; } =
    [
        new("habit_id", typeof(string), "Identificador del hábito completado", IsRequired: true),
        new("completed_at", typeof(string), "Fecha y hora del cumplimiento (opcional, por defecto momento actual)", IsRequired: false),
        new("note", typeof(string), "Nota o comentario opcional sobre el cumplimiento", IsRequired: false)
    ];

    public async Task<CommandResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters == null ||
            !parameters.TryGetValue("habit_id", out var rawHabitId) ||
            rawHabitId == null ||
            string.IsNullOrWhiteSpace(rawHabitId.ToString()))
        {
            return CommandResult.Failure("El parámetro 'habit_id' es obligatorio y no puede estar vacío.");
        }

        var habitId = rawHabitId.ToString()!.Trim();

        try
        {
            var habit = await _habitRepository.GetByIdAsync(habitId, cancellationToken).ConfigureAwait(false);
            if (habit == null)
            {
                return CommandResult.Failure($"No se encontró el hábito con ID '{habitId}'.");
            }

            var completedAt = DateTimeOffset.UtcNow;
            if (parameters.TryGetValue("completed_at", out var rawCompletedAt) && rawCompletedAt != null)
            {
                if (rawCompletedAt is DateTimeOffset dto)
                {
                    completedAt = dto;
                }
                else if (rawCompletedAt is DateTime dt)
                {
                    completedAt = new DateTimeOffset(dt);
                }
                else if (DateTimeOffset.TryParse(rawCompletedAt.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                {
                    completedAt = parsed;
                }
            }

            string? note = null;
            if (parameters.TryGetValue("note", out var rawNote) && rawNote != null)
            {
                note = rawNote.ToString()?.Trim();
            }

            var habitEvent = new HabitEvent
            {
                HabitId = habitId,
                CompletedAt = completedAt,
                Note = note
            };

            var saved = await _habitRepository.RecordEventAsync(habitEvent, cancellationToken).ConfigureAwait(false);

            if (_eventBus != null)
            {
                var source = parameters.TryGetValue("source", out var rawSource) && rawSource != null
                    ? rawSource.ToString()!
                    : "system";

                await _eventBus.PublishAsync(new HabitCompletedEvent(
                    HabitId: habit.Id,
                    HabitName: habit.Name,
                    Note: habitEvent.Note,
                    CompletedAt: habitEvent.CompletedAt,
                    Source: source,
                    EventId: Guid.NewGuid().ToString("N"),
                    OccurredAt: DateTimeOffset.UtcNow
                ), cancellationToken).ConfigureAwait(false);
            }

            return CommandResult.Success(saved);
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(ex.Message);
        }
    }
}
