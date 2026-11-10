using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HourlyTracker.Services;

namespace HourlyTracker.ViewModels;

public record ActivityTotal(string Name, double Minutes, double Fraction, string Text);

public record EntryItem(int Id, string TimeText, string Name, string Comment, bool HasComment, string Duration);

public partial class TodayViewModel : ObservableObject
{
    private readonly TrackerDb _db = TrackerDb.Instance;

    [ObservableProperty] private DateTime _day = DateTime.Today;
    [ObservableProperty] private string _dayTitle = "";
    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private bool _canGoNext;

    public ObservableCollection<ActivityTotal> Totals { get; } = new();
    public ObservableCollection<EntryItem> Entries { get; } = new();

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var entries = await _db.GetEntriesForDayAsync(Day);

        var groups = entries
            .GroupBy(e => e.ActivityName)
            .Select(g => (Name: g.Key, Minutes: g.Sum(e => e.Minutes)))
            .OrderByDescending(g => g.Minutes)
            .ToList();
        double total = groups.Sum(g => g.Minutes);
        double max = groups.Count == 0 ? 1 : groups.Max(g => g.Minutes);

        Totals.Clear();
        foreach (var g in groups)
        {
            double pct = total == 0 ? 0 : g.Minutes / total * 100;
            Totals.Add(new ActivityTotal(g.Name, g.Minutes, g.Minutes / max, $"{FormatDuration(g.Minutes)} · {pct:0}%"));
        }

        Entries.Clear();
        foreach (var e in entries.OrderByDescending(e => e.SlotEnd))
        {
            Entries.Add(new EntryItem(
                e.Id,
                $"{e.SlotStart:HH:mm} – {e.SlotEnd:HH:mm}",
                e.ActivityName,
                e.Comment,
                !string.IsNullOrWhiteSpace(e.Comment),
                FormatDuration(e.Minutes)));
        }

        IsEmpty = entries.Count == 0;
        CanGoNext = Day.Date < DateTime.Today;
        DayTitle = Day.Date == DateTime.Today ? "Today" :
                   Day.Date == DateTime.Today.AddDays(-1) ? "Yesterday" :
                   Day.ToString("dddd, d MMM");
        SummaryText = entries.Count == 0
            ? "Nothing logged yet."
            : $"{FormatDuration(total)} logged across {groups.Count} {(groups.Count == 1 ? "activity" : "activities")}";
    }

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

    public static string FormatDuration(double minutes)
    {
        int m = (int)Math.Round(minutes);
        int h = m / 60;
        m %= 60;
        return h == 0 ? $"{m}m" : m == 0 ? $"{h}h" : $"{h}h {m}m";
    }
}
