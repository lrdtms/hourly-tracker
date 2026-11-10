using Android.App;
using Android.Content;
using Android.OS;
using HourlyTracker.Services;

namespace HourlyTracker;

/// <summary>Registers the single "next prompt" alarm. Each fired alarm schedules the next one.</summary>
public static class AlarmScheduler
{
    private const int RequestCode = 4001;

    public static void Schedule(Context context)
    {
        var am = (AlarmManager?)context.GetSystemService(Context.AlarmService);
        if (am is null) return;

        // Cancel whatever is pending, then register the new one (if enabled).
        using (var existing = BuildPending(context, 0, PendingIntentFlags.NoCreate))
        {
            if (existing is not null) am.Cancel(existing);
        }

        if (!TrackerSettings.Enabled) return;

        var next = ScheduleCalculator.NextTrigger(
            DateTime.Now, TrackerSettings.IntervalMinutes, TrackerSettings.ActiveStart, TrackerSettings.ActiveEnd);
        if (next is null) return;

        long triggerAtMillis = new DateTimeOffset(next.Value).ToUnixTimeMilliseconds();
        using var pending = BuildPending(context, next.Value.Ticks, PendingIntentFlags.UpdateCurrent)!;

        // Exact alarms need the user-revocable SCHEDULE_EXACT_ALARM permission on Android 12+.
        // Without it, fall back to an inexact (but still doze-tolerant) alarm.
        if (!OperatingSystem.IsAndroidVersionAtLeast(31) || am.CanScheduleExactAlarms())
            am.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pending);
        else
            am.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMillis, pending);
    }

    private static PendingIntent? BuildPending(Context context, long slotEndTicks, PendingIntentFlags extraFlags)
    {
        var intent = new Intent(context, typeof(AlarmReceiver));
        intent.PutExtra(PromptActivity.ExtraSlotEndTicks, slotEndTicks);
        return PendingIntent.GetBroadcast(context, RequestCode, intent, extraFlags | PendingIntentFlags.Immutable);
    }
}
