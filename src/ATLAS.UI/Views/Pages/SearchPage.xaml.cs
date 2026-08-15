using ATLAS.UI.ViewModels;
using ATLAS_UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ATLAS.UI.Views.Pages;

public sealed partial class SearchPage : Page
{
    public SearchViewModel ViewModel { get; }

    public SearchPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<SearchViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadInitialNotesAsync();
    }
}
