using ATLAS.Core.Commands;
using ATLAS.Core.Integrations.Gmail;
using ATLAS.Core.Integrations.Telegram;
using ATLAS.Core.Security;
using Microsoft.Extensions.DependencyInjection;

namespace ATLAS.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ATLAS Core services, integrations, and commands into the DI container.
    /// </summary>
    public static IServiceCollection AddAtlasCore(this IServiceCollection services)
    {
        services.AddSingleton<ICommandRegistry, CommandRegistry>();
        services.AddSingleton<TelegramMessageProcessor>();
        services.AddSingleton<ITelegramListenerService, TelegramListenerService>();

        // Gmail Integration
        services.AddTransient<IGmailClient>(sp =>
            new GmailClient(sp.GetService<HttpClient>() ?? new HttpClient(), sp.GetRequiredService<ISecretVault>()));

        // Supabase Integration
        services.AddTransient<ATLAS.Core.Integrations.Supabase.ISupabaseAuthService, ATLAS.Core.Integrations.Supabase.SupabaseAuthService>();
        services.AddTransient<ATLAS.Core.Integrations.Supabase.ISupabaseSyncService, ATLAS.Core.Integrations.Supabase.SupabaseSyncService>();

        // Commands
        services.AddTransient<CaptureNoteCommand>();
        services.AddTransient<KnowledgeSearchCommand>();
        services.AddTransient<AiSummarizeCommand>();
        services.AddTransient<AiAskCommand>();
        services.AddTransient<GoalCreateCommand>();
        services.AddTransient<GoalUpdateProgressCommand>();
        services.AddTransient<HabitCreateCommand>();
        services.AddTransient<HabitCompleteCommand>();
        services.AddTransient<FinanceAddTransactionCommand>();
        services.AddTransient<FinanceSyncMercadoPagoCommand>();
        services.AddTransient<GmailListRecentCommand>();
        services.AddTransient<RoadmapCreateCommand>();
        services.AddTransient<RoadmapAddMilestoneCommand>();
        services.AddTransient<RoadmapCompleteMilestoneCommand>();
        services.AddTransient<SupabaseSyncCommand>();

        return services;
    }
}
