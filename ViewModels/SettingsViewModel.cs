using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HourlyTracker.Services;

namespace HourlyTracker.ViewModels;

public record IntervalOption(int Minutes, string Label);

public partial class SettingsViewModel : ObservableObject
{
    private readonly IPlatformServices _platform;
    private bool _loading;

    public SettingsViewModel(IPlatformServices platform)
    {
        _platform = platform;
        IntervalOptions = TrackerSettings.IntervalChoices.Select(m => new IntervalOption(m, LabelFor(m))).ToList();
        Load();
    }

    public IReadOnlyList<IntervalOption> IntervalOptions { get; }

    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private IntervalOption? _selectedInterval;
    [ObservableProperty] private TimeSpan _activeStart;
    [ObservableProperty] private TimeSpan _activeEnd;

    [ObservableProperty] private string _nextPromptText = "";
    [ObservableProperty] private string _windowHint = "";

    [ObservableProperty] private bool _needsNotifications;
    [ObservableProperty] private bool _needsExactAlarms;
    [ObservableProperty] private bool _needsOverlay;
    [ObservableProperty] private bool _needsFullScreen;

    public void Load()
    {
        _loading = true;
        Enabled = TrackerSettings.Enabled;
        SelectedInterval = IntervalOptions.FirstOrDefault(o => o.Minutes == TrackerSettings.IntervalMinutes)
                           ?? IntervalOptions.First(o => o.Minutes == 60);
        ActiveStart = TrackerSettings.ActiveStart;
        ActiveEnd = TrackerSettings.ActiveEnd;
        _loading = false;
        RefreshStatus();
    }

    /// <summary>Re-reads permission state (call when returning from system settings) and re-arms the alarm.</summary>
    public void RefreshStatus()
    {
        NeedsNotifications = !_platform.NotificationsAllowed;
        NeedsExactAlarms = !_platform.ExactAlarmsAllowed;
        NeedsOverlay = !_platform.OverlayAllowed;
        NeedsFullScreen = !_platform.FullScreenIntentAllowed;
        _platform.Reschedule();
        UpdateTexts();
    }

    partial void OnEnabledChanged(bool value) => Apply();
    partial void OnSelectedIntervalChanged(IntervalOption? value) => Apply();
    partial void OnActiveStartChanged(TimeSpan value) => Apply();
    partial void OnActiveEndChanged(TimeSpan value) => Apply();

    private void Apply()
    {
        if (_loading) return;
        TrackerSettings.Enabled = Enabled;
        if (SelectedInterval is not null) TrackerSettings.IntervalMinutes = SelectedInterval.Minutes;
        TrackerSettings.ActiveStartMinutes = (int)ActiveStart.TotalMinutes;
        TrackerSettings.ActiveEndMinutes = (int)ActiveEnd.TotalMinutes;
        _platform.Reschedule();
        UpdateTexts();
    }

    private void UpdateTexts()
    {
        var next = ScheduleCalculator.NextTrigger(
            DateTime.Now, TrackerSettings.IntervalMinutes, TrackerSettings.ActiveStart, TrackerSettings.ActiveEnd);

        NextPromptText = !TrackerSettings.Enabled ? "Prompts are off."
            : next is null ? "No prompt fits inside your active hours."
            : $"Next prompt: {(next.Value.Date == DateTime.Today ? "today" : next.Value.ToString("ddd"))} at {next.Value:HH:mm}";

        WindowHint = TrackerSettings.ActiveStart == TrackerSettings.ActiveEnd
            ? "Prompting around the clock."
            : $"No prompts between {TrackerSettings.ActiveEnd:hh\\:mm} and {TrackerSettings.ActiveStart:hh\\:mm}. " +
              $"The first prompt each day comes one interval after {TrackerSettings.ActiveStart:hh\\:mm}.";
    }

    [RelayCommand] private void FixNotifications() => _platform.RequestNotifications();
    [RelayCommand] private void FixExactAlarms() => _platform.OpenExactAlarmSettings();
    [RelayCommand] private void FixOverlay() => _platform.OpenOverlaySettings();
    [RelayCommand] private void FixFullScreen() => _platform.OpenFullScreenIntentSettings();
    [RelayCommand] private void TestPrompt() => _platform.ShowTestPrompt();

    public bool IsDebug =>
#if DEBUG
        true;
#else
        false;
#endif

    /// <summary>DEBUG builds only: replaces all data with 5 weeks of sample history to review the charts.</summary>
    [RelayCommand]
    private async Task LoadSampleAsync()
    {
        await SampleData.LoadAsync();
        SampleStatus = "Sample data loaded. Open Today and History.";
    }

    [ObservableProperty] private string _sampleStatus = "";

    private static string LabelFor(int minutes) => minutes switch
    {
        60 => "Every hour",
        < 60 => $"Every {minutes} minutes",
        _ when minutes % 60 == 0 => $"Every {minutes / 60} hours",
        _ => $"Every {minutes / 60.0:0.#} hours",
    };
}
