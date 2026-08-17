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
    public static void LogStartupError(string source, Exception ex)
    {
        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir = Path.Combine(localAppData, "ATLAS");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var logPath = Path.Combine(dir, "startup-error.log");
            var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff UTC}] [{source}]\n{ex.GetType().FullName}: {ex.Message}\nStackTrace:\n{ex.StackTrace}\nInnerException: {ex.InnerException?.ToString()}\n--------------------------------------------------\n";
            File.AppendAllText(logPath, logEntry);
        }
        catch { }
    }

    public static MauiApp CreateMauiApp()
    {
        // 2. Global unhandled exception handlers
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            LogStartupError("AppDomain.UnhandledException", e.ExceptionObject as Exception ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown AppDomain error"));
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogStartupError("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

        try
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
            builder.Services.AddSingleton<IAiBackend, GeminiProvider>();
            builder.Services.AddSingleton<IAiProvider, AiOrchestrator>();

            // Core, Storage & Context Services
            builder.Services.AddAtlasCore();
            builder.Services.AddAtlasStorage();
            builder.Services.AddSingleton<IContextActionService, ContextActionService>();

            var app = builder.Build();

            // 1. Initialize SQLite & Register Commands with try/catch
            InitializeSystem(app.Services);

            return app;
        }
        catch (Exception ex)
        {
            LogStartupError("MauiProgram.CreateMauiApp", ex);
            throw;
        }
    }

    private static void InitializeSystem(IServiceProvider services)
    {
        try
        {
            // 1. Database schema initialization moved to MainLayout (async)
            // var dbInitializer = services.GetRequiredService<DatabaseInitializer>();
            // dbInitializer.InitializeAsync().GetAwaiter().GetResult();

            // 2. Register Commands into CommandRegistry
            var commandRegistry = services.GetRequiredService<ICommandRegistry>();
            commandRegistry.Register(services.GetRequiredService<CaptureNoteCommand>());
            commandRegistry.Register(services.GetRequiredService<KnowledgeSearchCommand>());
            commandRegistry.Register(services.GetRequiredService<AiSummarizeCommand>());
            commandRegistry.Register(services.GetRequiredService<AiAskCommand>());
            commandRegistry.Register(services.GetRequiredService<AiExplainCommand>());
            commandRegistry.Register(services.GetRequiredService<AiRewriteCommand>());
            commandRegistry.Register(services.GetRequiredService<AiTranslateCommand>());
            commandRegistry.Register(services.GetRequiredService<GoalCreateCommand>());
            commandRegistry.Register(services.GetRequiredService<GoalUpdateProgressCommand>());
            commandRegistry.Register(services.GetRequiredService<HabitCreateCommand>());
            commandRegistry.Register(services.GetRequiredService<HabitCompleteCommand>());
            commandRegistry.Register(services.GetRequiredService<FinanceAddTransactionCommand>());
            commandRegistry.Register(services.GetRequiredService<FinanceCategorizeCommand>());
            commandRegistry.Register(services.GetRequiredService<FinanceSyncMercadoPagoCommand>());
            commandRegistry.Register(services.GetRequiredService<GmailListRecentCommand>());
            commandRegistry.Register(services.GetRequiredService<RoadmapCreateCommand>());
            commandRegistry.Register(services.GetRequiredService<RoadmapAddMilestoneCommand>());
            commandRegistry.Register(services.GetRequiredService<RoadmapCompleteMilestoneCommand>());
            commandRegistry.Register(services.GetRequiredService<SupabaseSyncCommand>());

            // 3. Start Telegram Listener in background thread pool
            var telegramListener = services.GetRequiredService<ITelegramListenerService>();
            _ = Task.Run(() => telegramListener.StartAsync());

            // 4. Start Activity Event Subscriber
            var hostedServices = services.GetServices<Microsoft.Extensions.Hosting.IHostedService>();
            var activitySubscriber = hostedServices.OfType<ATLAS.Core.Services.ActivityEventSubscriber>().FirstOrDefault();
            if (activitySubscriber != null)
            {
                _ = activitySubscriber.StartAsync(CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            LogStartupError("InitializeSystem", ex);
        }
    }
}
