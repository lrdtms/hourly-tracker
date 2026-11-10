using Android;
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using HourlyTracker.Services;
using Uri = Android.Net.Uri;

namespace HourlyTracker;

public class AndroidPlatformServices : IPlatformServices
{
    private static Context Ctx => Android.App.Application.Context;
    private static string PackageUri => $"package:{Ctx.PackageName}";

    public bool NotificationsAllowed => NotificationManagerCompat.From(Ctx)!.AreNotificationsEnabled();

    public bool ExactAlarmsAllowed =>
        !OperatingSystem.IsAndroidVersionAtLeast(31) ||
        ((AlarmManager?)Ctx.GetSystemService(Context.AlarmService))?.CanScheduleExactAlarms() == true;

    public bool OverlayAllowed => Android.Provider.Settings.CanDrawOverlays(Ctx);

    public bool FullScreenIntentAllowed =>
        !OperatingSystem.IsAndroidVersionAtLeast(34) ||
        ((NotificationManager?)Ctx.GetSystemService(Context.NotificationService))?.CanUseFullScreenIntent() == true;

    public void RequestNotifications()
    {
        const string asked = "notification_permission_asked";
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;

        // The system dialog only shows the first time(s); afterwards send the user to the settings page.
        if (OperatingSystem.IsAndroidVersionAtLeast(33) && activity is not null &&
            !Preferences.Default.Get(asked, false))
        {
            Preferences.Default.Set(asked, true);
            ActivityCompat.RequestPermissions(activity, new[] { Manifest.Permission.PostNotifications }, 1);
            return;
        }

        var intent = new Intent(Android.Provider.Settings.ActionAppNotificationSettings)
            .PutExtra(Android.Provider.Settings.ExtraAppPackage, Ctx.PackageName);
        Launch(intent);
    }

    public void OpenExactAlarmSettings()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
            Launch(new Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm, Uri.Parse(PackageUri)));
    }

    public void OpenOverlaySettings() =>
        Launch(new Intent(Android.Provider.Settings.ActionManageOverlayPermission, Uri.Parse(PackageUri)));

    public void OpenFullScreenIntentSettings()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(34))
            Launch(new Intent(Android.Provider.Settings.ActionManageAppUseFullScreenIntent, Uri.Parse(PackageUri)));
    }

    public void Reschedule() => AlarmScheduler.Schedule(Ctx);

    public void ShowTestPrompt()
    {
        var end = DateTime.Now;
        PromptLauncher.Show(Ctx, end.AddMinutes(-TrackerSettings.IntervalMinutes), end);
    }

    private static void Launch(Intent intent)
    {
        intent.AddFlags(ActivityFlags.NewTask);
        Ctx.StartActivity(intent);
    }
}
