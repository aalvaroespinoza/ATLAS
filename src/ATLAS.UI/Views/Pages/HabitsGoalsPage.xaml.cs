using ATLAS.UI.ViewModels;
using ATLAS_UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace ATLAS.UI.Views.Pages;

public sealed partial class HabitsGoalsPage : Page
{
    public HabitsGoalsViewModel ViewModel { get; }

    public HabitsGoalsPage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<HabitsGoalsViewModel>();
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadAllAsync();
    }

    private async void OnCompleteHabitClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string habitId })
        {
            await ViewModel.CompleteHabitAsync(habitId);
        }
    }
}
