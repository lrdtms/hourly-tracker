using Android.App;
using Android.Content.PM;
using Android.OS;

namespace HourlyTracker;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Keep the window dark behind the splash-to-app hand-off so there is no white flash.
        Window?.SetBackgroundDrawable(new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.ParseColor("#1F201D")));

        // Dark bars with light icons while the dark splash is up; AppShell switches them to white once the app shows.
        SystemBars.Set(this, SystemBars.Ink, darkIcons: false);

        // Make sure the next prompt is armed whenever the app is opened.
        AlarmScheduler.Schedule(this);
    }
}
