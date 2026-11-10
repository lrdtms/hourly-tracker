using HourlyTracker.Services;
using Microsoft.Maui.Graphics;

namespace HourlyTracker.Controls;

public record DayBucket(DateTime Date, double[] Minutes)
{
    public double Total => Minutes.Sum();
}

public record ChartData(IReadOnlyList<DayBucket> Days, IReadOnlyList<Color> SeriesColors)
{
    public double MaxMinutes => Days.Count == 0 ? 0 : Days.Max(d => d.Total);
}

/// <summary>
/// Stacked columns of time per day, stacked by activity. Built to the dataviz mark specs:
/// thin columns on one baseline, a 1.5px seam between stacked segments, 3-5 faint gridlines,
/// round hour ticks, flat x labels, nothing drawn on the bars. Tap a day to select it.
/// Emphasis mode paints one activity in the accent and everything else grey.
/// </summary>
public class StackedChartView : GraphicsView
{
    private readonly Painter _drawable = new();

    public event EventHandler<int>? DayTapped;

    public StackedChartView()
    {
        Drawable = _drawable;
        HeightRequest = 220;
        HorizontalOptions = LayoutOptions.Fill;
        StartInteraction += (_, e) =>
        {
            var p = e.Touches.FirstOrDefault();
            int idx = _drawable.IndexAt(p.X);
            if (idx >= 0) DayTapped?.Invoke(this, idx);
        };
    }

    public void Update(ChartData? data, int selectedDay, int emphasisSeries)
    {
        _drawable.Data = data;
        _drawable.Selected = selectedDay;
        _drawable.Emphasis = emphasisSeries;
        Invalidate();
    }

    private sealed class Painter : IDrawable
    {
        public ChartData? Data;
        public int Selected = -1;
        public int Emphasis = -1;

        private const float Left = 34f, Right = 2f, Top = 6f, Bottom = 22f;
        private RectF _plot;

        public int IndexAt(float x)
        {
            if (Data is null || Data.Days.Count == 0 || _plot.Width <= 0) return -1;
            float slot = _plot.Width / Data.Days.Count;
            int i = (int)Math.Floor((x - _plot.Left) / slot);
            return i >= 0 && i < Data.Days.Count ? i : -1;
        }

        /// <summary>Round, even steps: the top tick is at or above the maximum, never touching it.</summary>
        private static (double max, double step) NiceAxis(double maxHours)
        {
            if (maxHours <= 0) return (1, 1);
            double[] steps = { 0.5, 1, 2, 3, 4, 5, 6, 8, 10 };
            foreach (var s in steps)
            {
                int ticks = (int)Math.Ceiling(maxHours * 1.02 / s);
                if (ticks <= 4) return (ticks * s, s);
            }
            return (Math.Ceiling(maxHours / 10) * 10, 10);
        }

        public void Draw(ICanvas canvas, RectF rect)
        {
            if (Data is null || Data.Days.Count == 0) return;

            _plot = new RectF(rect.Left + Left, rect.Top + Top, rect.Width - Left - Right, rect.Height - Top - Bottom);
            var (axisMax, step) = NiceAxis(Data.MaxMinutes / 60.0);

            canvas.FontSize = 11;
            canvas.FontColor = ChartPalette.Muted;
            canvas.Font = new Microsoft.Maui.Graphics.Font("InstrumentSans-Regular");

            // Gridlines + y labels.
            canvas.StrokeSize = 1;
            for (double h = 0; h <= axisMax + 1e-6; h += step)
            {
                float y = _plot.Bottom - (float)(h / axisMax) * _plot.Height;
                canvas.StrokeColor = h == 0 ? ChartPalette.Baseline : ChartPalette.Grid;
                canvas.DrawLine(_plot.Left, y, _plot.Right, y);
                canvas.DrawString($"{h:0.#}h", rect.Left, y - 8, Left - 6, 16,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }

            int n = Data.Days.Count;
            float slotW = _plot.Width / n;
            float barW = Math.Clamp(slotW * 0.62f, 2f, 26f);

            for (int i = 0; i < n; i++)
            {
                float cx = _plot.Left + slotW * (i + .5f);
                float x = cx - barW / 2;

                if (i == Selected)
                {
                    canvas.FillColor = ChartPalette.Highlight;
                    canvas.FillRoundedRectangle(cx - slotW / 2, _plot.Top, slotW, _plot.Height, 4);
                }

                var day = Data.Days[i];
                float y = _plot.Bottom;
                for (int s = 0; s < day.Minutes.Length; s++)
                {
                    double mins = day.Minutes[s];
                    if (mins <= 0) continue;
                    float h = (float)(mins / 60.0 / axisMax) * _plot.Height;
                    float seam = y < _plot.Bottom ? 1.5f : 0f; // gap only between segments, not at the baseline
                    float top = y - h;

                    canvas.FillColor = Emphasis < 0
                        ? Data.SeriesColors[s]
                        : (s == Emphasis ? ChartPalette.Emphasis : ChartPalette.DeEmphasis);
                    float segH = Math.Max(1f, h - seam);
                    canvas.FillRectangle(x, top + seam, barW, segH);
                    y = top;
                }
            }

            // Flat x labels on one step.
            canvas.FontColor = ChartPalette.Muted;
            // Latest label first; a label that would touch the one to its right is dropped.
            float leftmost = float.MaxValue;
            foreach (int i in LabelIndices(n).Reverse())
            {
                float cx = _plot.Left + slotW * (i + .5f);
                string text = n <= 7 ? Data.Days[i].Date.ToString("ddd")[..1] : Data.Days[i].Date.ToString("d MMM");
                float w = n <= 7 ? slotW : 48;
                float x = Math.Clamp(cx - w / 2, _plot.Left, _plot.Right - w);
                if (x + w > leftmost - 6) continue;
                canvas.DrawString(text, x, _plot.Bottom + 4, w, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
                leftmost = x;
            }
        }

        private static IEnumerable<int> LabelIndices(int n)
        {
            if (n <= 7) return Enumerable.Range(0, n);
            int step = n <= 31 ? 7 : 14;
            // Anchor on the last day so the most recent label is always shown.
            var list = new List<int>();
            for (int i = n - 1; i >= 0; i -= step) list.Add(i);
            list.Reverse();
            return list;
        }
    }
}
