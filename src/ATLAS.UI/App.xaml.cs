namespace ATLAS.UI;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new MainPage())
        {
            Title = "ATLAS",
            Width = 1100,
            Height = 720,
            MinimumWidth = 800,
            MinimumHeight = 550
        };
    }
}
