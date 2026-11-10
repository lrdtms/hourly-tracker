using HourlyTracker.Pages;
using HourlyTracker.Services;
using HourlyTracker.ViewModels;
using Microsoft.Extensions.Logging;

namespace HourlyTracker;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddSingleton<IPlatformServices, AndroidPlatformServices>();
		builder.Services.AddSingleton<TrackerDb>(_ => TrackerDb.Instance);

		builder.Services.AddTransient<TodayViewModel>();
		builder.Services.AddTransient<ActivitiesViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<TodayPage>();
		builder.Services.AddTransient<ActivitiesPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
