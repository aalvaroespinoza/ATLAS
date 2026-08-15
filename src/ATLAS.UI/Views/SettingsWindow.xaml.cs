using Windows.Graphics;
using ATLAS.UI.Interop;
using ATLAS.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace ATLAS.UI.Views;

public sealed partial class SettingsWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly IntPtr _hwnd;

    public SettingsViewModel ViewModel { get; }

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        ConfigureWindow();
    }

    private void ConfigureWindow()
    {
        Title = "Configuración — ATLAS";
        SystemBackdrop = new MicaBackdrop();

        const int width = 560;
        const int height = 440;

        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);

        if (displayArea != null)
        {
            var workArea = displayArea.WorkArea;
            var x = workArea.X + (workArea.Width - width) / 2;
            var y = workArea.Y + (workArea.Height - height) / 2;
            _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
        }

        _appWindow.Closing += (s, e) =>
        {
            e.Cancel = true;
            _appWindow.Hide();
        };
    }

    public void ShowAndActivate()
    {
        ViewModel.LoadKeyStatus();
        _appWindow.Show();
        Activate();
        NativeMethods.SetForegroundWindow(_hwnd);
    }
}
