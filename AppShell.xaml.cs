namespace HourlyTracker;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// The splash hands over on a dark ground; fade the app in instead of cutting to white.
		Opacity = 0;
		Loaded += async (_, _) =>
		{
#if ANDROID
			SystemBars.ApplyLight(); // white status/navigation bars with dark icons (no more template purple)
#endif
			await this.FadeToAsync(1, 280, Easing.CubicOut);
		};
	}
}
