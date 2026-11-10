# Assets

| Asset | Method | Chosen | Status |
|---|---|---|---|
| App icon (adaptive: background + foreground) | hand-written SVG, rendered with headless Edge | **B: Stacked day** (owner's pick) | in app: `Resources/AppIcon/appicon.svg` + `appiconfg.svg` |
| Native splash (static, Android can't animate it) | same mark, `MauiSplashScreen`, colour `#1F201D` | matches icon B | in app: `Resources/Splash/splash.svg` |
| Animated splash | coded SVG frames → headless Edge → ffmpeg palette GIF | **2: Rebuild** (owner's pick) | in app: `Resources/Images/splash_rebuild.gif`, played by `Pages/SplashPage` |
| Tab icons | hand-written 24px stroke SVGs | n/a | in app: `Resources/Images/tab_*.svg` |

Rejected options kept in `options/` (A "Check-in ring" icon + Ripple splash, C "H for hour" icon + Crossbar splash).
`bash build-options.sh` regenerates every option; edit `build-options.mjs` to change a mark.

## Verified
- GIF background is exactly `#1F201D` in first, middle and last frame, so it melts into the splash page.
- GIF is 25 KB, 36 frames, 1.8 s, first frame == last frame == the resting mark (seamless loop and hand-off).
- Launch sequence (native splash → GIF page → app) and the launcher icon checked on an Android 16 emulator.

## Not verified
- Real-device launchers with themed/monochrome icons (no monochrome layer was supplied).
- Play Store listing graphics (not requested).
