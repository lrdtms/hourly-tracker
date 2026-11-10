using Microsoft.Maui.Graphics;

namespace HourlyTracker.Services;

/// <summary>Chart colours, in fixed slot order (identity follows the activity, never cycles).</summary>
public static class ChartPalette
{
    public static readonly Color[] Series =
    {
        Color.FromArgb("#2A78D6"), // blue
        Color.FromArgb("#EB6834"), // orange
        Color.FromArgb("#1BAF7A"), // aqua
        Color.FromArgb("#EDA100"), // yellow
        Color.FromArgb("#E87BA4"), // magenta
    };

    public static readonly Color Other = Color.FromArgb("#8C8C88");
    public static readonly Color Emphasis = Color.FromArgb("#1B6E66");
    public static readonly Color DeEmphasis = Color.FromArgb("#D9D9D6");
    public static readonly Color Grid = Color.FromArgb("#EAEAEA");
    public static readonly Color Baseline = Color.FromArgb("#D9D9D6");
    public static readonly Color Muted = Color.FromArgb("#636361");
    public static readonly Color Ink = Color.FromArgb("#1F201D");
    public static readonly Color Highlight = Color.FromArgb("#F2F2EF");

    public const int MaxSeries = 5;

    public static Color ForSlot(int slot) => slot >= 0 && slot < Series.Length ? Series[slot] : Other;
}
