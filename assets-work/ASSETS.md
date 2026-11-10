# Assets

| Asset | Method | Chosen | Status |
|---|---|---|---|
| App icon (adaptive: background + foreground) | hand-written SVG, rendered with headless Edge | **B: Stacked day** (owner's pick) | in app: `Resources/AppIcon/appicon.svg` + `appiconfg.svg` |
| Native splash (static, Android can't animate it) | same mark, `MauiSplashScreen`, colour `#1F201D` | matches icon B | in app: `Resources/Splash/splash.svg` |
| Animated splash | **2: Rebuild** (owner's pick). Prototyped as a GIF, then re-implemented as a code-drawn animation (`Controls/SplashMarkView.cs`: `GraphicsView` + MAUI `Animation`) because MAUI's Image only supports small GIFs, decodes every frame and doesn't cache or resize them, and the GIF stalled the main thread on device | in app: `Pages/SplashPage` + `Controls/SplashMarkView`. `final/splash.gif` is kept only as a reference render |
| Tab icons | hand-written 24px stroke SVGs | n/a | in app: `Resources/Images/tab_*.svg` |

Rejected options kept in `options/` (A "Check-in ring" icon + Ripple splash, C "H for hour" icon + Crossbar splash).
`bash build-options.sh` regenerates every option; edit `build-options.mjs` to change a mark.

## Verified
- Launch sequence (native splash → animated splash page → app) and the launcher icon checked on an Android 16 emulator, Debug and Release.
- Animation geometry/timing is a direct port of `markB` in `build-options.mjs`; t = 0 is the resting mark, so it matches Android's static splash.
- The animation window has no skipped-frame warnings in logcat; startup stalls happen before the first frame (behind the native splash) and at the page swap.

## Not verified
- Real-device launchers with themed/monochrome icons (no monochrome layer was supplied).
- Play Store listing graphics (not requested).
