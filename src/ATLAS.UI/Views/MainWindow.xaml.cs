using Windows.Graphics;
using ATLAS.UI.Interop;
using ATLAS.UI.Views.Pages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;

namespace ATLAS.UI.Views;

public sealed partial class MainWindow : Window
{
    private readonly AppWindow _appWindow;
    private readonly IntPtr _hwnd;

    public MainWindow()
    {
        InitializeComponent();

        _hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(_hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        ConfigureWindow();
    }

    private void ConfigureWindow()
    {
        Title = "ATLAS";
        SystemBackdrop = new MicaBackdrop();

        const int width = 1040;
        const int height = 700;

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

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        // Select Home by default
        if (NavView.MenuItems.Count > 0 && NavView.MenuItems[0] is NavigationViewItem homeItem)
        {
            NavView.SelectedItem = homeItem;
            NavigateToTag("home");
        }
    }

    private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            NavigateToTag("settings");
            return;
        }

        if (args.InvokedItemContainer is NavigationViewItem item && item.Tag is string tag)
        {
            NavigateToTag(tag);
        }
    }

    public void NavigateToTag(string tag)
    {
        Type pageType = tag switch
        {
            "home" => typeof(HomePage),
            "capture" => typeof(CapturePage),
            "search" => typeof(SearchPage),
            "habits_goals" => typeof(HabitsGoalsPage),
            "finance" => typeof(FinancePage),
            "settings" => typeof(SettingsPage),
            _ => typeof(HomePage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType, null, new Microsoft.UI.Xaml.Media.Animation.EntranceNavigationTransitionInfo());
        }
    }

    public void ShowAndActivate()
    {
        _appWindow.Show();
        Activate();
        NativeMethods.SetForegroundWindow(_hwnd);
    }
}
