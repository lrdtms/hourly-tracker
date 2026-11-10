namespace HourlyTracker.Pages;

/// <summary>
/// Plays the "Rebuild" animation once after Android's static splash, then swaps in the real app.
/// The app shell is built BEFORE this page is shown (see App.CreateWindow), so the main thread is idle
/// while the animation runs; nothing competes with it for frames. Skipped when system animations are off.
/// </summary>
public partial class SplashPage : ContentPage
{
    private readonly Page _next;

    public SplashPage(Page next)
    {
        InitializeComponent();
        _next = next;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (AnimationsDisabled())
        {
            Enter();
            return;
        }

        _ = Name.FadeToAsync(1, 600, Easing.CubicOut);
        Mark.Play(finished: async () =>
        {
            await Task.Delay(250); // a beat on the resting mark
            // Swap while the finished mark is still on screen. Creating the shell's native views blocks the main
            // thread briefly (longest in Debug/JIT builds); the old frame stays visible meanwhile, so it reads as
            // the splash holding for a moment, not as a freeze on a blank screen. The shell then fades in.
            Enter();
        });
    }

    protected override void OnDisappearing()
    {
        Mark.Stop();
        base.OnDisappearing();
    }

    private void Enter()
    {
        if (Window is { } window) window.Page = _next;
    }

    private static bool AnimationsDisabled()
    {
#if ANDROID
        try
        {
            var resolver = Android.App.Application.Context.ContentResolver;
            return Android.Provider.Settings.Global.GetFloat(resolver, Android.Provider.Settings.Global.AnimatorDurationScale, 1f) == 0f;
        }
        catch { return false; }
#else
        return false;
#endif
    }
}
