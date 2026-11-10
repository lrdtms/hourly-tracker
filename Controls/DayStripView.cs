using Microsoft.Maui.Graphics;

namespace HourlyTracker.Controls;

public enum SlotState { Logged, Missed, Upcoming }

/// <summary>
/// The day as a row of check-in slots: filled teal = answered, grey = missed, outline = still to come.
/// This is the screen's own element: it answers "did I keep up with today's check-ins?" at a glance.
/// </summary>
public class DayStripView : GraphicsView
{
    private readonly Painter _drawable = new();

    public DayStripView()
    {
        Drawable = _drawable;
        HeightRequest = 34;
        HorizontalOptions = LayoutOptions.Fill;
    }

    public void SetSlots(IReadOnlyList<SlotState> slots)
    {
        _drawable.Slots = slots;
        Invalidate();
    }

    private sealed class Painter : IDrawable
    {
        public IReadOnlyList<SlotState> Slots = Array.Empty<SlotState>();

        private static readonly Color Logged = Color.FromArgb("#1B6E66");
        private static readonly Color Missed = Color.FromArgb("#DEDED9");
        private static readonly Color Outline = Color.FromArgb("#D9D9D6");

        public void Draw(ICanvas canvas, RectF rect)
        {
            int n = Slots.Count;
            if (n == 0) return;

            const float gap = 4f;
            float w = (rect.Width - gap * (n - 1)) / n;
            float radius = Math.Min(8f, w / 2);

            for (int i = 0; i < n; i++)
            {
                float x = rect.Left + i * (w + gap);
                switch (Slots[i])
                {
                    case SlotState.Logged:
                        canvas.FillColor = Logged;
                        canvas.FillRoundedRectangle(x, rect.Top, w, rect.Height, radius);
                        break;
                    case SlotState.Missed:
                        canvas.FillColor = Missed;
                        canvas.FillRoundedRectangle(x, rect.Top, w, rect.Height, radius);
                        break;
                    default:
                        canvas.StrokeColor = Outline;
                        canvas.StrokeSize = 1.5f;
                        canvas.DrawRoundedRectangle(x + .75f, rect.Top + .75f, w - 1.5f, rect.Height - 1.5f, radius);
                        break;
                }
            }
        }
    }
}
