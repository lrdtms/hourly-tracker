using Android.App;
using Android.Content;
using HourlyTracker.Services;

namespace HourlyTracker;

/// <summary>Fires when the scheduled alarm goes off. Re-arms the next alarm, then shows the prompt.</summary>
[BroadcastReceiver(Enabled = true, Exported = false)]
public class AlarmReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null) return;

        long endTicks = intent?.GetLongExtra(PromptActivity.ExtraSlotEndTicks, 0) ?? 0;
        // Use the intended boundary (not "now") so a slightly late delivery still logs the right slot.
        var slotEnd = endTicks > 0 ? new DateTime(endTicks) : DateTime.Now;
        var slotStart = slotEnd.AddMinutes(-TrackerSettings.IntervalMinutes);

        // Re-arm first so a crash while showing the prompt can't stop future prompts.
        AlarmScheduler.Schedule(context);
        PromptLauncher.Show(context, slotStart, slotEnd);
    }
}

/// <summary>
/// Alarms are wiped on reboot, app update, and when the exact-alarm permission is revoked.
/// Re-register after any of those.
/// </summary>
[BroadcastReceiver(Enabled = true, Exported = true, DirectBootAware = false)]
[IntentFilter(new[]
{
    Intent.ActionBootCompleted,
    Intent.ActionMyPackageReplaced,
    AlarmManager.ActionScheduleExactAlarmPermissionStateChanged,
})]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null) return;
        AlarmScheduler.Schedule(context);
    }
}
