using Microsoft.Maui.Graphics;

namespace HourlyTracker.Controls;

/// <summary>
/// The "Rebuild" splash animation, drawn in code instead of played from a GIF.
/// Why: MAUI's Image only supports "small" animated GIFs, decodes every frame on the device, and does not
/// cache or resize them (Microsoft Learn: Image, "Improve app performance"). This is three rounded rectangles,
/// so it is drawn on a GraphicsView and driven by MAUI's Animation class: no decoding, no frame memory,
/// crisp at any density, and it can be stopped instantly.
/// Geometry and timing are the same as assets-work/build-options.mjs (markB), so it matches the static splash.
/// </summary>
public class SplashMarkView : GraphicsView
{
    private const string AnimationName = "rebuild";
    private readonly Painter _painter = new();

    public SplashMarkView()
    {
        Drawable = _painter; // t = 0 is the resting mark, identical to Android's static splash
        WidthRequest = 192; // mark height ~105dp, same as Android's native splash icon
        HeightRequest = 192;
    }

    /// <summary>Plays one 1.8 s pass, then invokes <paramref name="finished"/> on the UI thread.</summary>
    public void Play(Action? finished = null, uint lengthMs = 1800)
    {
        this.AbortAnimation(AnimationName);
        var animation = new Animation(t => { _painter.T = (float)t; Invalidate(); }, 0, 1);
        animation.Commit(this, AnimationName, rate: 16, length: lengthMs, easing: Easing.Linear,
            finished: (_, cancelled) =>
            {
                _painter.T = 0; // end on the resting mark
                Invalidate();
                if (!cancelled) finished?.Invoke();
            });
    }

    public void Stop() => this.AbortAnimation(AnimationName);

    private sealed class Painter : IDrawable
    {
        public float T;

        private static readonly Color Mint = Color.FromArgb("#5CC4B4");
        private static readonly Color Orange = Color.FromArgb("#EB6834");
        private static readonly Color Paper = Color.FromArgb("#F6F5F1");

        // Bottom to top: height, colour, opacity (in the 1024 design space).
        private static readonly (float H, Color C, float A)[] Segments =
        {
            (230, Mint, 1f),
            (150, Orange, 1f),
            (90, Paper, 1f),
            (48, Paper, 0.38f),
        };

        public void Draw(ICanvas canvas, RectF rect)
        {
            float s = Math.Min(rect.Width, rect.Height) / 1024f;
            float ox = rect.Left + (rect.Width - 1024f * s) / 2f;
            float oy = rect.Top + (rect.Height - 1024f * s) / 2f;

            const float gap = 16f, w = 300f;
            float x = 512f - w / 2f;
            float total = Segments.Sum(g => g.H) + gap * (Segments.Length - 1);
            float y = 512f + total / 2f;

            for (int i = 0; i < Segments.Length; i++)
            {
                var seg = Segments[i];
                float u = Math.Clamp((T - i * 0.11f) / 0.6f, 0f, 1f);
                float k = 1f - MathF.Sin(MathF.PI * u);     // each chunk collapses, then regrows from the bottom
                float h = MathF.Max(0f, seg.H * k);
                float top = y - h;

                if (h >= 1f)
                {
                    canvas.FillColor = seg.C.WithAlpha(seg.A);
                    canvas.FillRoundedRectangle(ox + x * s, oy + top * s, w * s, h * s, MathF.Min(46f, h / 2f) * s);
                }
                y = top - (h < 1f ? 0f : gap);
            }
        }
    }
}
