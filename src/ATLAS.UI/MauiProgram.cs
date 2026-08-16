using ATLAS.Core.Ai;
using ATLAS.Core.Commands;
using ATLAS.Core.Extensions;
using ATLAS.Core.Integrations.Telegram;
using ATLAS.Core.Security;
using ATLAS.Storage.Database;
using ATLAS.Storage.Extensions;
using ATLAS.UI.Services;
using Microsoft.Extensions.Logging;

namespace ATLAS.UI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        // Security & AI
        builder.Services.AddSingleton<ISecretVault, WindowsPasswordVault>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddSingleton<IAiProvider, GeminiProvider>();

        // Core, Storage & Context Services
        builder.Services.AddAtlasCore();
        builder.Services.AddAtlasStorage();
        builder.Services.AddSingleton<IContextActionService, ContextActionService>();

        var app = builder.Build();

        // Initialize SQLite & Register Commands
        InitializeSystem(app.Services);

        return app;
    }

    private static void InitializeSystem(IServiceProvider services)
    {
        // 1. Initialize SQLite Database Schema
        var dbInitializer = services.GetRequiredService<DatabaseInitializer>();
        dbInitializer.InitializeAsync().GetAwaiter().GetResult();

        // 2. Register Commands into CommandRegistry
        var commandRegistry = services.GetRequiredService<ICommandRegistry>();
        commandRegistry.Register(services.GetRequiredService<CaptureNoteCommand>());
        commandRegistry.Register(services.GetRequiredService<KnowledgeSearchCommand>());
        commandRegistry.Register(services.GetRequiredService<AiSummarizeCommand>());
        commandRegistry.Register(services.GetRequiredService<AiAskCommand>());
        commandRegistry.Register(services.GetRequiredService<GoalCreateCommand>());
        commandRegistry.Register(services.GetRequiredService<GoalUpdateProgressCommand>());
        commandRegistry.Register(services.GetRequiredService<HabitCreateCommand>());
        commandRegistry.Register(services.GetRequiredService<HabitCompleteCommand>());
        commandRegistry.Register(services.GetRequiredService<FinanceAddTransactionCommand>());
        commandRegistry.Register(services.GetRequiredService<FinanceSyncMercadoPagoCommand>());
        commandRegistry.Register(services.GetRequiredService<GmailListRecentCommand>());
        commandRegistry.Register(services.GetRequiredService<RoadmapCreateCommand>());
        commandRegistry.Register(services.GetRequiredService<RoadmapAddMilestoneCommand>());
        commandRegistry.Register(services.GetRequiredService<RoadmapCompleteMilestoneCommand>());

        // 3. Start Telegram Listener in background
        var telegramListener = services.GetRequiredService<ITelegramListenerService>();
        _ = telegramListener.StartAsync();
    }
}
