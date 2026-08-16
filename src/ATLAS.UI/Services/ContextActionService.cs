using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATLAS.Core.Commands;
using ATLAS.Core.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace ATLAS.UI.Services;

/// <summary>
/// Universal descriptor for contextual actions that can be executed on domain objects.
/// </summary>
public record ContextActionDescriptor(
    string Id,
    string Label,
    string Icon,
    string? Shortcut,
    bool IsPrimary,
    string? CommandId,
    Func<object, Dictionary<string, object?>?>? ParameterBuilder = null,
    Func<object, IServiceProvider, Task>? CustomAction = null
);

public interface IContextActionService
{
    IReadOnlyList<ContextActionDescriptor> GetActionsFor(object? target);
    Task<CommandResult> ExecuteActionAsync(ContextActionDescriptor action, object target);
}

public class ContextActionService : IContextActionService
{
    private readonly ICommandRegistry _commandRegistry;
    private readonly IServiceProvider _serviceProvider;

    public ContextActionService(ICommandRegistry commandRegistry, IServiceProvider serviceProvider)
    {
        _commandRegistry = commandRegistry;
        _serviceProvider = serviceProvider;
    }

    public IReadOnlyList<ContextActionDescriptor> GetActionsFor(object? target)
    {
        if (target == null) return Array.Empty<ContextActionDescriptor>();

        return target switch
        {
            Note note => GetNoteActions(note),
            Roadmap roadmap => GetRoadmapActions(roadmap),
            Habit habit => GetHabitActions(habit),
            Transaction tx => GetTransactionActions(tx),
            _ => Array.Empty<ContextActionDescriptor>()
        };
    }

    public async Task<CommandResult> ExecuteActionAsync(ContextActionDescriptor action, object target)
    {
        if (action.CustomAction != null)
        {
            await action.CustomAction.Invoke(target, _serviceProvider);
            return CommandResult.Success("Acción ejecutada.");
        }

        if (!string.IsNullOrWhiteSpace(action.CommandId))
        {
            var parameters = action.ParameterBuilder?.Invoke(target);
            return await _commandRegistry.ExecuteAsync(action.CommandId, parameters);
        }

        return CommandResult.Failure("Acción no configurable.");
    }

    private IReadOnlyList<ContextActionDescriptor> GetNoteActions(Note note)
    {
        return new List<ContextActionDescriptor>
        {
            new ContextActionDescriptor(
                Id: "note.summarize",
                Label: "✦ Resumir con Gemini",
                Icon: "sparkles",
                Shortcut: "↵",
                IsPrimary: true,
                CommandId: AiSummarizeCommand.CommandId,
                ParameterBuilder: obj => new Dictionary<string, object?>
                {
                    ["text"] = $"{((Note)obj).Title}\n{((Note)obj).Content}"
                }
            ),
            new ContextActionDescriptor(
                Id: "note.extract_tasks",
                Label: "✦ Extraer Tareas",
                Icon: "tasks",
                Shortcut: "⌘T",
                IsPrimary: false,
                CommandId: AiAskCommand.CommandId,
                ParameterBuilder: obj => new Dictionary<string, object?>
                {
                    ["query"] = $"Analizá la siguiente nota y extraé únicamente una lista concisa de tareas o acciones pendientes:\n\n{((Note)obj).Content}"
                }
            ),
            new ContextActionDescriptor(
                Id: "note.open",
                Label: "↗ Abrir en Segundo Cerebro",
                Icon: "external",
                Shortcut: "⌘O",
                IsPrimary: false,
                CommandId: null,
                CustomAction: (obj, sp) =>
                {
                    var nav = sp.GetRequiredService<NavigationManager>();
                    nav.NavigateTo("search");
                    return Task.CompletedTask;
                }
            )
        };
    }

    private IReadOnlyList<ContextActionDescriptor> GetRoadmapActions(Roadmap roadmap)
    {
        var actions = new List<ContextActionDescriptor>();
        var nextMilestone = roadmap.Milestones?.FirstOrDefault(m => m.Status == "pending");

        if (nextMilestone != null)
        {
            actions.Add(new ContextActionDescriptor(
                Id: "roadmap.complete_next",
                Label: $"✓ Completar: {nextMilestone.Title}",
                Icon: "check",
                Shortcut: "↵",
                IsPrimary: true,
                CommandId: RoadmapCompleteMilestoneCommand.CommandId,
                ParameterBuilder: obj => new Dictionary<string, object?>
                {
                    ["roadmap_id"] = ((Roadmap)obj).Id,
                    ["milestone_id"] = nextMilestone.Id
                }
            ));
        }

        actions.Add(new ContextActionDescriptor(
            Id: "roadmap.analyze",
            Label: "✦ Analizar Ruta con Gemini",
            Icon: "sparkles",
            Shortcut: "⌘A",
            IsPrimary: false,
            CommandId: AiAskCommand.CommandId,
            ParameterBuilder: obj => new Dictionary<string, object?>
            {
                ["query"] = $"Analizá el siguiente roadmap y recomendá el mejor enfoque para avanzar de forma efectiva:\n\nTítulo: {((Roadmap)obj).Title}\nDescripción: {((Roadmap)obj).Description}"
            }
        ));

        actions.Add(new ContextActionDescriptor(
            Id: "roadmap.manage",
            Label: "↗ Gestionar en Metas & Roadmaps",
            Icon: "external",
            Shortcut: "⌘G",
            IsPrimary: false,
            CommandId: null,
            CustomAction: (obj, sp) =>
            {
                var nav = sp.GetRequiredService<NavigationManager>();
                nav.NavigateTo("habits-goals");
                return Task.CompletedTask;
            }
        ));

        return actions;
    }

    private IReadOnlyList<ContextActionDescriptor> GetHabitActions(Habit habit)
    {
        return new List<ContextActionDescriptor>
        {
            new ContextActionDescriptor(
                Id: "habit.complete",
                Label: "✓ Marcar como Completado Hoy",
                Icon: "check",
                Shortcut: "↵",
                IsPrimary: true,
                CommandId: HabitCompleteCommand.CommandId,
                ParameterBuilder: obj => new Dictionary<string, object?>
                {
                    ["habit_id"] = ((Habit)obj).Id
                }
            ),
            new ContextActionDescriptor(
                Id: "habit.analyze",
                Label: "✦ Analizar Constancia con Gemini",
                Icon: "sparkles",
                Shortcut: "⌘A",
                IsPrimary: false,
                CommandId: AiAskCommand.CommandId,
                ParameterBuilder: obj => new Dictionary<string, object?>
                {
                    ["query"] = $"Dame un consejo práctico y breve para mantener la constancia en este hábito:\n\nHábito: {((Habit)obj).Name} (Frecuencia: {((Habit)obj).Frequency})"
                }
            ),
            new ContextActionDescriptor(
                Id: "habit.manage",
                Label: "↗ Ver Hábitos",
                Icon: "external",
                Shortcut: "⌘H",
                IsPrimary: false,
                CommandId: null,
                CustomAction: (obj, sp) =>
                {
                    var nav = sp.GetRequiredService<NavigationManager>();
                    nav.NavigateTo("habits-goals");
                    return Task.CompletedTask;
                }
            )
        };
    }

    private IReadOnlyList<ContextActionDescriptor> GetTransactionActions(Transaction tx)
    {
        return new List<ContextActionDescriptor>
        {
            new ContextActionDescriptor(
                Id: "finance.open",
                Label: "↗ Ver en Finanzas",
                Icon: "external",
                Shortcut: "↵",
                IsPrimary: true,
                CommandId: null,
                CustomAction: (obj, sp) =>
                {
                    var nav = sp.GetRequiredService<NavigationManager>();
                    nav.NavigateTo("finance");
                    return Task.CompletedTask;
                }
            ),
            new ContextActionDescriptor(
                Id: "finance.analyze",
                Label: "✦ Analizar Gasto con Gemini",
                Icon: "sparkles",
                Shortcut: "⌘A",
                IsPrimary: false,
                CommandId: AiAskCommand.CommandId,
                ParameterBuilder: obj => new Dictionary<string, object?>
                {
                    ["query"] = $"Analizá este movimiento financiero y sugerí optimizaciones presupuestarias:\n\nMonto: ${((Transaction)obj).Monto:N0} {((Transaction)obj).Moneda}\nDescripción: {((Transaction)obj).Descripcion}\nTipo: {((Transaction)obj).Tipo}"
                }
            )
        };
    }
}
