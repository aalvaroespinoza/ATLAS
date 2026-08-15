using ATLAS.UI.ViewModels;
using ATLAS_UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ATLAS.UI.Views.Pages;

public sealed partial class FinancePage : Page
{
    public FinanceViewModel ViewModel { get; }

    public FinancePage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<FinanceViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadTransactionsAsync();
    }
}
