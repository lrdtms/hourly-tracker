using Microsoft.Maui.Graphics;

namespace HourlyTracker.Controls;

/// <summary>A single thin horizontal bar: its fill is the true ratio (0..1) of the row's share.</summary>
public class BarView : GraphicsView
{
    public static readonly BindableProperty FractionProperty =
        BindableProperty.Create(nameof(Fraction), typeof(double), typeof(BarView), 0d, propertyChanged: (b, _, _) => ((BarView)b).Refresh());

    public static readonly BindableProperty BarColorProperty =
        BindableProperty.Create(nameof(BarColor), typeof(Color), typeof(BarView), Colors.Gray, propertyChanged: (b, _, _) => ((BarView)b).Refresh());

    private readonly Painter _drawable = new();

    public BarView()
    {
        Drawable = _drawable;
        HeightRequest = 8;
        HorizontalOptions = LayoutOptions.Fill;
    }

    public double Fraction
    {
        get => (double)GetValue(FractionProperty);
        set => SetValue(FractionProperty, value);
    }

    public Color BarColor
    {
        get => (Color)GetValue(BarColorProperty);
        set => SetValue(BarColorProperty, value);
    }

    private void Refresh()
    {
        _drawable.Fraction = (float)Math.Clamp(Fraction, 0, 1);
        _drawable.Color = BarColor;
        Invalidate();
    }

    private sealed class Painter : IDrawable
    {
        public float Fraction;
        public Color Color = Colors.Gray;

        public void Draw(ICanvas canvas, RectF rect)
        {
            if (Fraction <= 0) return; // a zero has no stub
            float w = Math.Max(rect.Height, rect.Width * Fraction);
            canvas.FillColor = Color;
            canvas.FillRoundedRectangle(rect.Left, rect.Top, w, rect.Height, rect.Height / 2);
        }
    }
}
