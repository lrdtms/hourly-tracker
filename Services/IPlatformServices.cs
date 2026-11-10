namespace HourlyTracker.Services;

/// <summary>Android-specific capabilities the shared UI needs.</summary>
public interface IPlatformServices
{
    bool NotificationsAllowed { get; }
    bool ExactAlarmsAllowed { get; }
    bool OverlayAllowed { get; }
    bool FullScreenIntentAllowed { get; }

    void RequestNotifications();
    void OpenExactAlarmSettings();
    void OpenOverlaySettings();
    void OpenFullScreenIntentSettings();

    /// <summary>Re-computes and (re)registers the next alarm from the current settings.</summary>
    void Reschedule();

    void ShowTestPrompt();
}
