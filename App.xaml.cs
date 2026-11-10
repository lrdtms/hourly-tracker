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
		// Do the heavy work now, while Android's static splash is still on screen and nothing is animating:
		// build the shell (XAML inflation) and open/migrate the database on a background thread.
		var shell = new AppShell();
		_ = Task.Run(async () => await Services.TrackerDb.Instance.GetActivitiesAsync());

		return new Window(new Pages.SplashPage(shell));
	}
}