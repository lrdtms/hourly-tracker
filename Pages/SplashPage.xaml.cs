namespace HourlyTracker.Pages;

/// <summary>
/// Plays the "Rebuild" GIF once, straight after Android's own static splash (same mark, same background),
/// then swaps in the real app. Skipped when the user has turned system animations off.
/// </summary>
public partial class SplashPage : ContentPage
{
    // One loop of the GIF is 1.8 s; hold a beat on the resting mark, then leave.
    private const int HoldMs = 2000;

    public SplashPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (AnimationsDisabled())
        {
            Enter();
            return;
        }

        Mark.IsAnimationPlaying = true;
        _ = Name.FadeToAsync(1, 600, Easing.CubicOut);
        await Task.Delay(HoldMs);
        await Task.WhenAll(Mark.FadeToAsync(0, 220), Name.FadeToAsync(0, 220));
        Enter();
    }

    private static void Enter()
    {
        if (Application.Current?.Windows.FirstOrDefault() is { } window)
            window.Page = new AppShell();
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
