using Windows.Graphics;
using Windows.System;
using ATLAS.UI.Interop;
using ATLAS.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinRT.Interop;

namespace ATLAS.UI.Views;

public sealed partial class LauncherWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly IntPtr _hwnd;

    public LauncherViewModel ViewModel { get; }

    public LauncherWindow(LauncherViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        _hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        ConfigureWindow();

        ViewModel.CloseRequested += OnCloseRequested;
        Activated += OnWindowActivated;
    }

    private void ConfigureWindow()
    {
        // 1. Remove titlebar and standard window borders
        _appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        // 2. Set backdrop
        SystemBackdrop = new DesktopAcrylicBackdrop();

        // 3. Size and Position in upper third of active monitor
        CenterInUpperThird(680, 72);
    }

    private void CenterInUpperThird(int width, int height)
    {
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary);

        if (displayArea != null)
        {
            var workArea = displayArea.WorkArea;
            var x = workArea.X + (workArea.Width - width) / 2;
            var y = workArea.Y + (int)(workArea.Height * 0.22);

            _appWindow.MoveAndResize(new RectInt32(x, y, width, height));
        }
    }

    public void ShowLauncher()
    {
        ViewModel.Reset();
        _appWindow.Show();
        Activate();
        NativeMethods.SetForegroundWindow(_hwnd);
        InputTextBox.Focus(FocusState.Programmatic);
    }

    public void HideLauncher()
    {
        ViewModel.Reset();
        _appWindow.Hide();
    }

    public void ToggleLauncher()
    {
        if (_appWindow.IsVisible)
        {
            HideLauncher();
        }
        else
        {
            ShowLauncher();
        }
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            // Auto-hide on blur (losing focus)
            HideLauncher();
        }
    }

    private void OnCloseRequested()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            HideLauncher();
        });
    }

    private async void OnInputKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            e.Handled = true;
            await ViewModel.SubmitCommand.ExecuteAsync(null);
        }
        else if (e.Key == VirtualKey.Escape)
        {
            e.Handled = true;
            ViewModel.CancelCommand.Execute(null);
        }
    }
}
