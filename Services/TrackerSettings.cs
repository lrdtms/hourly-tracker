namespace HourlyTracker.Services;

/// <summary>User-adjustable schedule settings, stored in Android SharedPreferences.</summary>
public static class TrackerSettings
{
    public static readonly int[] IntervalChoices = { 15, 30, 45, 60, 90, 120, 180 };

    public static bool Enabled
    {
        get => Preferences.Default.Get(nameof(Enabled), true);
        set => Preferences.Default.Set(nameof(Enabled), value);
    }

    public static int IntervalMinutes
    {
        get => Preferences.Default.Get(nameof(IntervalMinutes), 60);
        set => Preferences.Default.Set(nameof(IntervalMinutes), Math.Max(5, value));
    }

    /// <summary>Minutes after midnight. Default 07:00.</summary>
    public static int ActiveStartMinutes
    {
        get => Preferences.Default.Get(nameof(ActiveStartMinutes), 7 * 60);
        set => Preferences.Default.Set(nameof(ActiveStartMinutes), value);
    }

    /// <summary>Minutes after midnight. Default 22:00.</summary>
    public static int ActiveEndMinutes
    {
        get => Preferences.Default.Get(nameof(ActiveEndMinutes), 22 * 60);
        set => Preferences.Default.Set(nameof(ActiveEndMinutes), value);
    }

    public static TimeSpan ActiveStart => TimeSpan.FromMinutes(ActiveStartMinutes);
    public static TimeSpan ActiveEnd => TimeSpan.FromMinutes(ActiveEndMinutes);
}
