using ATLAS.UI.ViewModels;
using ATLAS_UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ATLAS.UI.Views.Pages;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; }

    public HomePage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<HomeViewModel>();
        ViewModel.NavigateRequested += OnNavigateRequested;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadDashboardAsync();
    }

    private void OnNavigateRequested(string tag)
    {
        if (App.Current.MainWindow is MainWindow mainWindow)
        {
            mainWindow.NavigateToTag(tag);
        }
    }

    private void OnOpenLauncherClicked(object sender, RoutedEventArgs e)
    {
        var launcher = App.Current.Services.GetRequiredService<LauncherWindow>();
        launcher.ShowLauncher();
    }
}
