using ATLAS.Core.Ai;
using ATLAS.Core.Commands;
using ATLAS.Core.Extensions;
using ATLAS.Core.Security;
using ATLAS.Storage.Database;
using ATLAS.Storage.Extensions;
using ATLAS.UI.Interop;
using ATLAS.UI.Services;
using ATLAS.UI.ViewModels;
using ATLAS.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace ATLAS_UI;

/// <summary>
/// Main Application entry point for ATLAS Personal OS.
/// </summary>
public partial class App : Application
{
    private LauncherWindow? _launcherWindow;
    private ActivityWindow? _activityWindow;
    private SettingsWindow? _settingsWindow;
    private HotKeyService? _hotKeyService;

    /// <summary>
    /// Gets the current App instance.
    /// </summary>
    public static new App Current => (App)Application.Current;

    /// <summary>
    /// Gets the application service provider.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Initializes the singleton application object.
    /// </summary>
    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
        InitializeSystem();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Security and AI services
        services.AddSingleton<ISecretVault, WindowsPasswordVault>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IAiProvider, GeminiProvider>();

        // Core and Storage services
        services.AddAtlasCore();
        services.AddAtlasStorage();

        // UI ViewModels and Views
        services.AddTransient<LauncherViewModel>();
        services.AddSingleton<LauncherWindow>();
        services.AddTransient<ActivityViewModel>();
        services.AddSingleton<ActivityWindow>();
        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<SettingsWindow>();

        return services.BuildServiceProvider();
    }

    private void InitializeSystem()
    {
        // 1. Initialize SQLite database schema
        var dbInitializer = Services.GetRequiredService<DatabaseInitializer>();
        dbInitializer.InitializeAsync().GetAwaiter().GetResult();

        // 2. Register startup commands into CommandRegistry
        var commandRegistry = Services.GetRequiredService<ICommandRegistry>();
        commandRegistry.Register(Services.GetRequiredService<CaptureNoteCommand>());
        commandRegistry.Register(Services.GetRequiredService<KnowledgeSearchCommand>());
        commandRegistry.Register(Services.GetRequiredService<AiSummarizeCommand>());
        commandRegistry.Register(Services.GetRequiredService<AiAskCommand>());
        commandRegistry.Register(Services.GetRequiredService<GoalCreateCommand>());
        commandRegistry.Register(Services.GetRequiredService<GoalUpdateProgressCommand>());
        commandRegistry.Register(Services.GetRequiredService<HabitCreateCommand>());
        commandRegistry.Register(Services.GetRequiredService<HabitCompleteCommand>());
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // 1. Instantiate Windows
        _launcherWindow = Services.GetRequiredService<LauncherWindow>();
        _activityWindow = Services.GetRequiredService<ActivityWindow>();
        _settingsWindow = Services.GetRequiredService<SettingsWindow>();

        // 2. Setup Global Hotkeys on the Launcher Window handle
        var hwnd = WindowNative.GetWindowHandle(_launcherWindow);
        _hotKeyService = new HotKeyService(hwnd);

        // ID 1001: Ctrl + Space -> Launcher
        _hotKeyService.Register(
            1001,
            NativeMethods.MOD_CONTROL,
            NativeMethods.VK_SPACE,
            OnLauncherHotKeyPressed);

        // ID 1002: Ctrl + Shift + Space -> Actividad
        _hotKeyService.Register(
            1002,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT,
            NativeMethods.VK_SPACE,
            OnActivityHotKeyPressed);

        // Connect launcher action to open activity window
        _launcherWindow.ViewModel.OpenActivityRequested += () =>
        {
            _launcherWindow.DispatcherQueue.TryEnqueue(() =>
            {
                _activityWindow?.ShowAndActivate();
            });
        };

        // Connect activity action to open settings window
        _activityWindow.ViewModel.OpenSettingsRequested += () =>
        {
            _activityWindow.DispatcherQueue.TryEnqueue(() =>
            {
                _settingsWindow?.ShowAndActivate();
            });
        };

        // 3. Keep launcher hidden in background initially (ready for hotkeys)
        _launcherWindow.HideLauncher();
    }

    private void OnLauncherHotKeyPressed()
    {
        if (_launcherWindow == null) return;

        _launcherWindow.DispatcherQueue.TryEnqueue(() =>
        {
            _launcherWindow.ToggleLauncher();
        });
    }

    private void OnActivityHotKeyPressed()
    {
        if (_activityWindow == null) return;

        _activityWindow.DispatcherQueue.TryEnqueue(() =>
        {
            _activityWindow.ShowAndActivate();
        });
    }
}
