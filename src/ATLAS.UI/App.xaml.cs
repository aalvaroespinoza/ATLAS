using ATLAS.Core.Commands;
using ATLAS.Core.Extensions;
using ATLAS.Storage.Database;
using ATLAS.Storage.Extensions;
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

        // Core and Storage services
        services.AddAtlasCore();
        services.AddAtlasStorage();

        // UI ViewModels and Views
        services.AddTransient<LauncherViewModel>();
        services.AddSingleton<LauncherWindow>();

        return services.BuildServiceProvider();
    }

    private void InitializeSystem()
    {
        // 1. Initialize SQLite database schema
        var dbInitializer = Services.GetRequiredService<DatabaseInitializer>();
        dbInitializer.InitializeAsync().GetAwaiter().GetResult();

        // 2. Register startup commands into CommandRegistry
        var commandRegistry = Services.GetRequiredService<ICommandRegistry>();
        var captureNoteCommand = Services.GetRequiredService<CaptureNoteCommand>();
        commandRegistry.Register(captureNoteCommand);
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // 1. Instantiate Launcher Window
        _launcherWindow = Services.GetRequiredService<LauncherWindow>();

        // 2. Setup Global Hotkey on the Launcher Window handle
        var hwnd = WindowNative.GetWindowHandle(_launcherWindow);
        _hotKeyService = new HotKeyService(hwnd);
        _hotKeyService.HotKeyPressed += OnGlobalHotKeyPressed;
        _hotKeyService.Register();

        // 3. Keep launcher hidden in background initially (ready for Ctrl+Space)
        _launcherWindow.HideLauncher();
    }

    private void OnGlobalHotKeyPressed()
    {
        if (_launcherWindow == null) return;

        _launcherWindow.DispatcherQueue.TryEnqueue(() =>
        {
            _launcherWindow.ToggleLauncher();
        });
    }
}
