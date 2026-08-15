using Windows.Graphics;
using ATLAS.UI.Interop;
using ATLAS.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace ATLAS.UI.Views;

public sealed partial class HomeWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly IntPtr _hwnd;
    private const int DefaultWidth = 780;
    private const int DefaultHeight = 640;

    public HomeViewModel ViewModel { get; }

    public HomeWindow(HomeViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        ConfigureWindow();

        Activated += OnWindowActivated;
    }

    private void ConfigureWindow()
    {
        // 1. Title bar configuration
        _appWindow.Title = "ATLAS — Inicio";
        _appWindow.TitleBar.ExtendsContentIntoTitleBar = false;

        // 2. Backdrop
        SystemBackdrop = new MicaBackdrop();

        // 3. Center Window
        CenterWindow(DefaultWidth, DefaultHeight);
    }

    private void CenterWindow(int width, int height)
    {
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);

        if (displayArea != null)
        {
            var workArea = displayArea.WorkArea;
            var x = workArea.X + (workArea.Width - width) / 2;
            var y = workArea.Y + (workArea.Height - height) / 2;

            _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
        }
    }

    public void ShowAndActivate()
    {
        _ = ViewModel.RefreshAsync();
        _appWindow.Show();
        Activate();
        NativeMethods.SetForegroundWindow(_hwnd);
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            _ = ViewModel.RefreshAsync();
        }
    }
}
