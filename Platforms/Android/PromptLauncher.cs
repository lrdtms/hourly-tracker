using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace HourlyTracker;

/// <summary>
/// Shows the "what did you do?" prompt. Tries to put the pop-up straight on screen (works when the
/// user granted "display over other apps"), and always posts a high-priority / full-screen-intent
/// notification as a backup that stays until the prompt is answered.
/// </summary>
public static class PromptLauncher
{
    public const string ChannelId = "hourly_prompts";

    public static void Show(Context context, DateTime slotStart, DateTime slotEnd)
    {
        EnsureChannel(context);

        var intent = BuildActivityIntent(context, slotStart, slotEnd);
        int notificationId = NotificationIdFor(slotEnd);

        // Backup / always-visible path.
        var contentIntent = PendingIntent.GetActivity(
            context, notificationId, intent, PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable)!;

        var notification = new NotificationCompat.Builder(context, ChannelId)
            .SetSmallIcon(Android.Resource.Drawable.IcDialogInfo)!
            .SetContentTitle("What did you do?")!
            .SetContentText($"{slotStart:HH:mm} – {slotEnd:HH:mm} · tap to log it")!
            .SetPriority(NotificationCompat.PriorityHigh)!
            .SetCategory(NotificationCompat.CategoryReminder)!
            .SetVisibility(NotificationCompat.VisibilityPublic)!
            .SetContentIntent(contentIntent)!
            .SetFullScreenIntent(contentIntent, true)!
            .SetAutoCancel(true)!
            .Build()!;

        NotificationManagerCompat.From(context)!.Notify(notificationId, notification);

        // Direct pop-up. Android only allows a background app to start an Activity in limited cases;
        // holding the overlay permission is one of them. Failure is non-fatal: the notification above remains.
        try
        {
            if (Android.Provider.Settings.CanDrawOverlays(context))
                context.StartActivity(intent);
        }
        catch (Exception ex)
        {
            Android.Util.Log.Warn("HourlyTracker", $"Direct pop-up blocked: {ex.Message}");
        }
    }

    public static void Dismiss(Context context, DateTime slotEnd) =>
        NotificationManagerCompat.From(context)!.Cancel(NotificationIdFor(slotEnd));

    private static Intent BuildActivityIntent(Context context, DateTime slotStart, DateTime slotEnd)
    {
        var intent = new Intent(context, typeof(PromptActivity));
        intent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
        intent.PutExtra(PromptActivity.ExtraSlotStartTicks, slotStart.Ticks);
        intent.PutExtra(PromptActivity.ExtraSlotEndTicks, slotEnd.Ticks);
        return intent;
    }

    private static int NotificationIdFor(DateTime slotEnd) =>
        (int)(slotEnd.Ticks / TimeSpan.TicksPerMinute % int.MaxValue);

    private static void EnsureChannel(Context context)
    {
        var manager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
        if (manager is null || manager.GetNotificationChannel(ChannelId) is not null) return;

        var channel = new NotificationChannel(ChannelId, "Activity prompts", NotificationImportance.High)
        {
            Description = "Asks what you did during the last interval."
        };
        channel.EnableVibration(true);
        channel.LockscreenVisibility = NotificationVisibility.Public;
        manager.CreateNotificationChannel(channel);
    }
}
