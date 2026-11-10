using Android.App;
using AndroidX.Core.View;
using AColor = Android.Graphics.Color;

namespace HourlyTracker;

/// <summary>Status-bar and navigation-bar colours, and whether their icons are dark or light.</summary>
public static class SystemBars
{
    public static readonly AColor White = AColor.ParseColor("#FFFFFF");
    public static readonly AColor Ink = AColor.ParseColor("#1F201D");

    /// <param name="background">Bar colour (ignored on Android 15+, where bars are transparent over the page).</param>
    /// <param name="darkIcons">true = dark icons for a light bar; false = light icons for a dark bar.</param>
    public static void Set(Activity? activity, AColor background, bool darkIcons)
    {
        var window = activity?.Window;
        if (window is null) return;

#pragma warning disable CA1422 // Obsolete from API 35, but still what controls the bars before edge-to-edge
        if (!OperatingSystem.IsAndroidVersionAtLeast(35))
        {
            window.SetStatusBarColor(background);
            window.SetNavigationBarColor(background);
        }
#pragma warning restore CA1422

        var controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller is not null)
        {
            controller.AppearanceLightStatusBars = darkIcons;
            controller.AppearanceLightNavigationBars = darkIcons;
        }
    }

    /// <summary>The app proper: white bars with dark icons.</summary>
    public static void ApplyLight() =>
        Set(Microsoft.Maui.ApplicationModel.Platform.CurrentActivity, White, darkIcons: true);
}
