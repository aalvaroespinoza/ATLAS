using ATLAS.Core.Events;
using ATLAS.Core.Repositories;
using ATLAS.Core.Entities;
using Microsoft.Extensions.Hosting;

namespace ATLAS.Core.Services;

/// <summary>
/// Background service that listens to Domain Events and converts them into normalized ActivityRecords.
/// </summary>
public class ActivityEventSubscriber : BackgroundService
{
    private readonly IAtlasEventBus _eventBus;
    private readonly IActivityRepository _activityRepository;

    public ActivityEventSubscriber(IAtlasEventBus eventBus, IActivityRepository activityRepository)
    {
        _eventBus = eventBus;
        _activityRepository = activityRepository;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _eventBus.Subscribe<NoteCapturedEvent>(async e => await HandleNoteCaptured(e, stoppingToken));
        _eventBus.Subscribe<HabitCompletedEvent>(async e => await HandleHabitCompleted(e, stoppingToken));
        _eventBus.Subscribe<TransactionCreatedEvent>(async e => await HandleTransactionCreated(e, stoppingToken));
        _eventBus.Subscribe<RoadmapMilestoneCompletedEvent>(async e => await HandleMilestoneCompleted(e, stoppingToken));

        return Task.CompletedTask;
    }

    private async Task HandleNoteCaptured(NoteCapturedEvent e, CancellationToken token)
    {
        // Regla: Notas largas (>50 chars) son más relevantes
        int score = e.Content.Length > 50 ? 5 : 2;
        
        var record = new ActivityRecord
        {
            Type = "knowledge",
            SourceId = e.NoteId,
            Title = string.IsNullOrWhiteSpace(e.Title) ? "Nota capturada" : $"Nota: {e.Title}",
            Summary = e.Content.Length > 100 ? e.Content[..97] + "..." : e.Content,
            RelevanceScore = score,
            Timestamp = e.OccurredAt
        };
        await _activityRepository.CreateAsync(record, token);
    }

    private async Task HandleHabitCompleted(HabitCompletedEvent e, CancellationToken token)
    {
        var record = new ActivityRecord
        {
            Type = "strategy",
            SourceId = e.HabitId,
            Title = $"Hábito completado: {e.HabitName}",
            Summary = e.Note,
            RelevanceScore = 3, // Regla estática simple por defecto
            Timestamp = e.OccurredAt
        };
        await _activityRepository.CreateAsync(record, token);
    }

    private async Task HandleTransactionCreated(TransactionCreatedEvent e, CancellationToken token)
    {
        // Regla: Gastos mayores a 50.000 son alta relevancia. Resto baja.
        int score = Math.Abs(e.Amount) > 50000 ? 8 : 2;

        var record = new ActivityRecord
        {
            Type = "finance",
            SourceId = e.TransactionId,
            Title = e.Type == "expense" ? $"Gasto: {e.Description}" : $"Ingreso: {e.Description}",
            Summary = $"Monto: {e.Amount:C2}. Categoría: {e.Category ?? "Sin categoría"}",
            RelevanceScore = score,
            Timestamp = e.OccurredAt
        };
        await _activityRepository.CreateAsync(record, token);
    }

    private async Task HandleMilestoneCompleted(RoadmapMilestoneCompletedEvent e, CancellationToken token)
    {
        if (!e.Completed) return;

        var record = new ActivityRecord
        {
            Type = "strategy",
            SourceId = e.MilestoneId,
            Title = $"Hito alcanzado: {e.MilestoneTitle}",
            Summary = $"Roadmap: {e.RoadmapTitle ?? e.RoadmapId}. Progreso total: {e.NewRoadmapProgress}%",
            RelevanceScore = 7, // Hitos siempre son relevantes
            Timestamp = e.OccurredAt
        };
        await _activityRepository.CreateAsync(record, token);
    }
}
