namespace HourlyTracker.Services;

/// <summary>
/// Pure scheduling maths (no Android dependencies, so it is easy to test).
/// Prompts fire on clock boundaries that are multiples of the interval after midnight
/// (every 60 min = on the hour). A prompt at time t asks about the slot [t - interval, t],
/// so it is only sent when t is inside the active window (start, end]. This is how the
/// "sleep period" works: nothing fires overnight, and the first prompt of the day comes
/// one interval after the window opens.
/// </summary>
public static class ScheduleCalculator
{
    /// <summary>Window is (start, end]. start == end means "always active". start &gt; end wraps midnight.</summary>
    public static bool IsInWindow(TimeSpan timeOfDay, TimeSpan start, TimeSpan end)
    {
        if (start == end) return true;
        return start < end
            ? timeOfDay > start && timeOfDay <= end
            : timeOfDay > start || timeOfDay <= end;
    }

    /// <summary>Next clock boundary strictly after <paramref name="after"/>.</summary>
    public static DateTime NextBoundary(DateTime after, int intervalMinutes)
    {
        intervalMinutes = Math.Max(5, intervalMinutes);
        var midnight = after.Date;
        long minutesSinceMidnight = (long)after.TimeOfDay.TotalMinutes;
        long next = (minutesSinceMidnight / intervalMinutes + 1) * intervalMinutes;
        return next >= 24 * 60 ? midnight.AddDays(1) : midnight.AddMinutes(next);
    }

    /// <summary>The next time a prompt should fire, or null if the window can never match.</summary>
    public static DateTime? NextTrigger(DateTime now, int intervalMinutes, TimeSpan activeStart, TimeSpan activeEnd)
    {
        var t = NextBoundary(now, intervalMinutes);
        // Two days of boundaries at the shortest interval (5 min) is 576 steps; 1000 is ample.
        for (var i = 0; i < 1000; i++)
        {
            if (IsInWindow(t.TimeOfDay, activeStart, activeEnd)) return t;
            t = NextBoundary(t, intervalMinutes);
        }
        return null;
    }
}
