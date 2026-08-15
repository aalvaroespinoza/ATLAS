using ATLAS.Core.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace ATLAS.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ATLAS Core services and commands into the DI container.
    /// </summary>
    public static IServiceCollection AddAtlasCore(this IServiceCollection services)
    {
        services.AddSingleton<ICommandRegistry, CommandRegistry>();
        services.AddTransient<CaptureNoteCommand>();
        services.AddTransient<KnowledgeSearchCommand>();
        services.AddTransient<AiSummarizeCommand>();
        services.AddTransient<AiAskCommand>();
        services.AddTransient<GoalCreateCommand>();
        services.AddTransient<GoalUpdateProgressCommand>();
        services.AddTransient<HabitCreateCommand>();
        services.AddTransient<HabitCompleteCommand>();
        return services;
    }
}
