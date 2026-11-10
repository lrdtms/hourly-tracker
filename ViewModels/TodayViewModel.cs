using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HourlyTracker.Controls;
using HourlyTracker.Models;
using HourlyTracker.Services;

namespace HourlyTracker.ViewModels;

public record ActivityTotal(string Name, double Minutes, double Fraction, string Text, Color BarColor);

public record EntryItem(int Id, string TimeText, string Name, string Comment, bool HasComment, string Duration);

public partial class TodayViewModel : ObservableObject
{
    private readonly TrackerDb _db = TrackerDb.Instance;

    [ObservableProperty] private DateTime _day = DateTime.Today;
    [ObservableProperty] private string _greeting = "";
    [ObservableProperty] private string _finding = "";
    [ObservableProperty] private string _dayLabel = "Today";
    [ObservableProperty] private string _stripCaption = "";
    [ObservableProperty] private string _stripStart = "";
    [ObservableProperty] private string _stripEnd = "";
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private bool _canGoNext;
    [ObservableProperty] private bool _hasActivities = true;

    public ObservableCollection<ActivityTotal> Totals { get; } = new();
    public ObservableCollection<EntryItem> Entries { get; } = new();

    /// <summary>Set by the page so the strip control can be redrawn.</summary>
    public Action<IReadOnlyList<SlotState>>? StripChanged { get; set; }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var entries = await _db.GetEntriesForDayAsync(Day);
        HasActivities = (await _db.GetActivitiesAsync()).Count > 0;

        var groups = entries
            .GroupBy(e => e.ActivityName)
            .Select(g => (Name: g.Key, Minutes: g.Sum(e => e.Minutes)))
            .OrderByDescending(g => g.Minutes)
            .ToList();
        double total = groups.Sum(g => g.Minutes);
        double max = groups.Count == 0 ? 1 : groups.Max(g => g.Minutes);

        Totals.Clear();
        for (int i = 0; i < groups.Count; i++)
        {
            var g = groups[i];
            double pct = total == 0 ? 0 : g.Minutes / total * 100;
            Totals.Add(new ActivityTotal(g.Name, g.Minutes, g.Minutes / max,
                $"{FormatDuration(g.Minutes)}  ·  {pct:0}%", ChartPalette.Emphasis));
        }

        Entries.Clear();
        foreach (var e in entries.OrderByDescending(e => e.SlotEnd))
        {
            Entries.Add(new EntryItem(e.Id, $"{e.SlotStart:HH:mm} – {e.SlotEnd:HH:mm}", e.ActivityName,
                e.Comment, !string.IsNullOrWhiteSpace(e.Comment), FormatDuration(e.Minutes)));
        }

        IsEmpty = entries.Count == 0;
        CanGoNext = Day.Date < DateTime.Today;
        bool isToday = Day.Date == DateTime.Today;
        DayLabel = isToday ? "Today" : Day.Date == DateTime.Today.AddDays(-1) ? "Yesterday" : Day.ToString("ddd d MMM");
        Greeting = isToday ? GreetingFor(DateTime.Now) : Day.ToString("dddd") + ".";

        Finding = entries.Count == 0
            ? (isToday ? "Nothing logged yet. Your next check-in will ask." : "Nothing was logged this day.")
            : $"{FormatDuration(total)} logged. Most of it was {groups[0].Name} ({FormatDuration(groups[0].Minutes)}).";

        BuildStrip(entries);
    }

    private void BuildStrip(List<LogEntry> entries)
    {
        int interval = TrackerSettings.IntervalMinutes;
        var start = TrackerSettings.ActiveStart;
        var end = TrackerSettings.ActiveEnd;

        // Every boundary of this day that falls inside the active window is one check-in slot.
        var slotEnds = new List<DateTime>();
        for (var t = Day.Date.AddMinutes(interval); t <= Day.Date.AddDays(1); t = t.AddMinutes(interval))
        {
            var tod = t.Date == Day.Date ? t.TimeOfDay : TimeSpan.Zero;
            if (ScheduleCalculator.IsInWindow(tod, start, end)) slotEnds.Add(t);
        }

        var now = DateTime.Now;
        var half = TimeSpan.FromMinutes(interval / 2.0);
        var states = new List<SlotState>();
        int past = 0, answered = 0;
        foreach (var slotEnd in slotEnds)
        {
            bool logged = entries.Any(e => (e.SlotEnd - slotEnd).Duration() <= half);
            if (slotEnd > now) states.Add(SlotState.Upcoming);
            else
            {
                past++;
                if (logged) { answered++; states.Add(SlotState.Logged); }
                else states.Add(SlotState.Missed);
            }
        }

        StripChanged?.Invoke(states);
        StripStart = slotEnds.Count > 0 ? $"{slotEnds[0].AddMinutes(-interval):HH:mm}" : "";
        StripEnd = slotEnds.Count > 0 ? $"{slotEnds[^1]:HH:mm}" : "";
        StripCaption = slotEnds.Count == 0 ? "No check-ins fit your active hours."
            : past == 0 ? $"{slotEnds.Count} check-ins today"
            : $"{answered} of {past} check-ins answered";
    }

    private static string GreetingFor(DateTime now) => now.Hour switch
    {
        < 5 => "Still up.",
        < 12 => "Good morning.",
        < 18 => "Good afternoon.",
        _ => "Good evening.",
    };

    [RelayCommand]
    private async Task PreviousDayAsync()
    {
        Day = Day.AddDays(-1);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task NextDayAsync()
    {
        if (Day.Date >= DateTime.Today) return;
        Day = Day.AddDays(1);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task DeleteEntryAsync(EntryItem item)
    {
        await _db.DeleteEntryAsync(item.Id);
        await RefreshAsync();
    }

    [RelayCommand]
    private static Task OpenActivitiesAsync() => Shell.Current.GoToAsync("//activities");

    public static string FormatDuration(double minutes)
    {
        int m = (int)Math.Round(minutes);
        int h = m / 60;
        m %= 60;
        return h == 0 ? $"{m}m" : m == 0 ? $"{h}h" : $"{h}h {m}m";
    }
}
