using Microsoft.Extensions.DependencyInjection;

namespace HourlyTracker;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		UserAppTheme = AppTheme.Light; // the design is light-only for now
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new Pages.SplashPage());
	}
}