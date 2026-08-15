using ATLAS.UI.ViewModels;
using ATLAS_UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace ATLAS.UI.Views.Pages;

public sealed partial class CapturePage : Page
{
    public CaptureViewModel ViewModel { get; }

    public CapturePage()
    {
        InitializeComponent();
        ViewModel = App.Current.Services.GetRequiredService<CaptureViewModel>();
    }
}
