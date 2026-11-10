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
				fonts.AddFont("InstrumentSans-Regular.ttf", "Sans");
				fonts.AddFont("InstrumentSans-Medium.ttf", "SansMedium");
				fonts.AddFont("InstrumentSans-SemiBold.ttf", "SansSemibold");
				fonts.AddFont("InstrumentSerif-Regular.ttf", "Serif");
			});

		builder.Services.AddSingleton<IPlatformServices, AndroidPlatformServices>();
		builder.Services.AddSingleton<TrackerDb>(_ => TrackerDb.Instance);

		builder.Services.AddTransient<TodayViewModel>();
		builder.Services.AddTransient<HistoryViewModel>();
		builder.Services.AddTransient<ActivitiesViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<TodayPage>();
		builder.Services.AddTransient<HistoryPage>();
		builder.Services.AddTransient<ActivitiesPage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
