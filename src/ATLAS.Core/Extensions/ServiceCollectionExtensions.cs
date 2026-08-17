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
        services.AddSingleton<ATLAS.Core.Events.IAtlasEventBus, ATLAS.Core.Events.AtlasEventBus>();
        services.AddSingleton<ICommandRegistry, CommandRegistry>();
        services.AddSingleton<TelegramMessageProcessor>();
        services.AddSingleton<ITelegramListenerService, TelegramListenerService>();
        services.AddHostedService<ATLAS.Core.Services.ActivityEventSubscriber>();

        // Integrations Clients & Hub
        services.AddTransient<IGmailClient>(sp =>
            new GmailClient(sp.GetService<HttpClient>() ?? new HttpClient(), sp.GetRequiredService<ISecretVault>()));
        services.AddTransient<ATLAS.Core.Integrations.Gmail.IGmailSyncService, ATLAS.Core.Integrations.Gmail.GmailSyncService>();
        services.AddHostedService<ATLAS.Core.Integrations.Gmail.GmailListenerService>();
        
        services.AddTransient<ATLAS.Core.Integrations.MercadoPago.IMercadoPagoClient>(sp =>
            new ATLAS.Core.Integrations.MercadoPago.MercadoPagoClient(sp.GetService<HttpClient>() ?? new HttpClient(), sp.GetRequiredService<ISecretVault>()));

        // Supabase Integration Services
        services.AddTransient<ATLAS.Core.Integrations.Supabase.ISupabaseAuthService, ATLAS.Core.Integrations.Supabase.SupabaseAuthService>();
        services.AddTransient<ATLAS.Core.Integrations.Supabase.ISupabaseSyncService, ATLAS.Core.Integrations.Supabase.SupabaseSyncService>();

        // Integration Adapters (IAtlasIntegration)
        services.AddSingleton<ATLAS.Core.Integrations.IAtlasIntegration, ATLAS.Core.Integrations.Telegram.TelegramIntegration>();
        services.AddSingleton<ATLAS.Core.Integrations.IAtlasIntegration, ATLAS.Core.Integrations.Gmail.GmailIntegration>();
        services.AddSingleton<ATLAS.Core.Integrations.IAtlasIntegration, ATLAS.Core.Integrations.MercadoPago.MercadoPagoIntegration>();
        services.AddSingleton<ATLAS.Core.Integrations.IAtlasIntegration, ATLAS.Core.Integrations.Supabase.SupabaseIntegration>();

        // Integration Registry
        services.AddSingleton<ATLAS.Core.Integrations.IIntegrationRegistry, ATLAS.Core.Integrations.IntegrationRegistry>();

        // Context Services
        services.AddTransient<ATLAS.Core.Context.IHomeContextService, ATLAS.Core.Context.HomeContextService>();
        services.AddTransient<ATLAS.Core.Context.IAtlasContextService, ATLAS.Core.Context.AtlasContextService>();

        // Commands
        services.AddTransient<CaptureNoteCommand>();
        services.AddTransient<KnowledgeSearchCommand>();
        services.AddTransient<AiSummarizeCommand>();
        services.AddTransient<AiAskCommand>();
        services.AddTransient<AiExplainCommand>();
        services.AddTransient<AiRewriteCommand>();
        services.AddTransient<AiTranslateCommand>();
        services.AddTransient<GoalCreateCommand>();
        services.AddTransient<GoalUpdateProgressCommand>();
        services.AddTransient<HabitCreateCommand>();
        services.AddTransient<HabitCompleteCommand>();
        services.AddTransient<FinanceAddTransactionCommand>();
        services.AddTransient<FinanceCategorizeCommand>();
        services.AddTransient<FinanceSyncMercadoPagoCommand>();
        services.AddTransient<GmailListRecentCommand>();
        services.AddTransient<GmailSyncActivityCommand>();
        services.AddTransient<RoadmapCreateCommand>();
        services.AddTransient<RoadmapAddMilestoneCommand>();
        services.AddTransient<RoadmapCompleteMilestoneCommand>();
        services.AddTransient<SupabaseSyncCommand>();

        return services;
    }
}
