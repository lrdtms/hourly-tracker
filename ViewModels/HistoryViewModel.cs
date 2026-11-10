using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HourlyTracker.Controls;
using HourlyTracker.Services;

namespace HourlyTracker.ViewModels;

public record RangeOption(string Label, int Days);

public record LegendItem(string Name, Color Color);

public partial class ActivityRow : ObservableObject
{
    public ActivityRow(string name, int series, string duration, string share, double fraction, Color color)
    {
        Name = name; Series = series; Duration = duration; Share = share; Fraction = fraction; BarColor = color;
    }

    public string Name { get; }
    public int Series { get; }
    public string Duration { get; }
    public string Share { get; }
    public double Fraction { get; }
    public Color BarColor { get; }

    [ObservableProperty] private bool _isSelected;
}

public partial class HistoryViewModel : ObservableObject
{
    private readonly TrackerDb _db = TrackerDb.Instance;
    private const string OtherName = "Other";

    public IReadOnlyList<RangeOption> Ranges { get; } = new[]
    {
        new RangeOption("7 days", 7),
        new RangeOption("30 days", 30),
        new RangeOption("90 days", 90),
    };

    [ObservableProperty] private RangeOption _selectedRange;
    [ObservableProperty] private string _headline = "";
    [ObservableProperty] private string _subline = "";
    [ObservableProperty] private string _finding = "";
    [ObservableProperty] private string _detail = "";
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private ChartData? _chart;
    [ObservableProperty] private int _selectedDay = -1;
    [ObservableProperty] private int _emphasis = -1;

    public ObservableCollection<ActivityRow> Rows { get; } = new();
    public ObservableCollection<LegendItem> Legend { get; } = new();

    public HistoryViewModel() => _selectedRange = Ranges[0];

    // Names of the series in slot order (top 5 by total, then "Other").
    private List<string> _seriesNames = new();

    partial void OnSelectedRangeChanged(RangeOption value) => _ = LoadAsync();
    partial void OnSelectedDayChanged(int value) => UpdateDetail();

    [RelayCommand]
    public async Task LoadAsync()
    {
        int days = SelectedRange.Days;
        var to = DateTime.Today;
        var from = to.AddDays(-(days - 1));
        var entries = await _db.GetEntriesRangeAsync(from, to);

        // Don't draw a wall of empty days before the first entry: start at the first logged day (min. 7 days).
        if (entries.Count > 0)
        {
            int sinceFirst = (to - entries.Min(e => e.SlotEnd).Date).Days + 1;
            days = Math.Clamp(sinceFirst, Math.Min(7, days), days);
            from = to.AddDays(-(days - 1));
        }

        // Rank activities by total time over the range; the top five get a colour, the rest fold into Other.
        var totals = entries.GroupBy(e => e.ActivityName)
            .Select(g => (Name: g.Key, Minutes: g.Sum(e => e.Minutes)))
            .OrderByDescending(g => g.Minutes)
            .ToList();
        var top = totals.Take(ChartPalette.MaxSeries).Select(t => t.Name).ToList();
        bool hasOther = totals.Count > ChartPalette.MaxSeries;

        _seriesNames = new List<string>(top);
        if (hasOther) _seriesNames.Add(OtherName);
        int seriesCount = _seriesNames.Count;
        int SlotOf(string name) { int i = top.IndexOf(name); return i >= 0 ? i : seriesCount - 1; }

        var buckets = new List<DayBucket>();
        for (int d = 0; d < days; d++)
        {
            var date = from.AddDays(d);
            var mins = new double[Math.Max(seriesCount, 1)];
            foreach (var e in entries.Where(e => e.SlotEnd > date && e.SlotEnd <= date.AddDays(1)))
                mins[SlotOf(e.ActivityName)] += e.Minutes;
            buckets.Add(new DayBucket(date, mins));
        }

        var colors = _seriesNames.Select((n, i) => n == OtherName && hasOther && i == seriesCount - 1
            ? ChartPalette.Other : ChartPalette.ForSlot(i)).ToList();
        if (colors.Count == 0) colors.Add(ChartPalette.Other);

        Legend.Clear();
        for (int i = 0; i < _seriesNames.Count; i++) Legend.Add(new LegendItem(_seriesNames[i], colors[i]));

        double grand = totals.Sum(t => t.Minutes);
        Rows.Clear();
        double max = totals.Count == 0 ? 1 : totals.Max(t => t.Minutes);
        foreach (var t in totals)
        {
            int slot = SlotOf(t.Name);
            Rows.Add(new ActivityRow(t.Name, slot,
                TodayViewModel.FormatDuration(t.Minutes),
                $"{t.Minutes / grand * 100:0}%",
                t.Minutes / max, colors[Math.Min(slot, colors.Count - 1)]));
        }

        Emphasis = -1;
        IsEmpty = entries.Count == 0;
        int daysWithData = buckets.Count(b => b.Total > 0);
        Headline = TodayViewModel.FormatDuration(grand);
        Subline = entries.Count == 0
            ? $"Nothing logged in the last {days} days."
            : $"logged in {days} days  ·  {TodayViewModel.FormatDuration(grand / Math.Max(1, daysWithData))} on a day you logged";
        Finding = totals.Count == 0 ? ""
            : $"{totals[0].Name} took {totals[0].Minutes / grand * 100:0}% of your logged time.";

        Chart = new ChartData(buckets, colors);
        SelectedDay = days - 1; // today
        UpdateDetail();
    }

    private void UpdateDetail()
    {
        if (Chart is null || SelectedDay < 0 || SelectedDay >= Chart.Days.Count) { Detail = ""; return; }
        var day = Chart.Days[SelectedDay];
        var label = day.Date == DateTime.Today ? "Today" : day.Date.ToString("ddd d MMM");
        if (day.Total <= 0) { Detail = $"{label}  ·  nothing logged"; return; }

        var parts = day.Minutes
            .Select((m, i) => (m, i))
            .Where(x => x.m > 0)
            .OrderByDescending(x => x.m)
            .Select(x => $"{_seriesNames[x.i]} {TodayViewModel.FormatDuration(x.m)}");
        Detail = $"{label}  ·  {TodayViewModel.FormatDuration(day.Total)}  —  {string.Join(", ", parts)}";
    }

    [RelayCommand]
    private void SelectDay(int index) => SelectedDay = index;

    [RelayCommand]
    private void SelectRange(RangeOption option) => SelectedRange = option;

    /// <summary>Tapping an activity highlights it in the chart (accent) and greys the rest; tap again to clear.</summary>
    [RelayCommand]
    private void ToggleEmphasis(ActivityRow row)
    {
        bool clear = Emphasis == row.Series;
        Emphasis = clear ? -1 : row.Series;
        foreach (var r in Rows) r.IsSelected = !clear && r.Series == row.Series;
    }
}
