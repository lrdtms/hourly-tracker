# Hourly Tracker

An Android app (built with .NET MAUI) that asks you **what you did** every hour and shows where your day went.

## Features

- **Your own activity list** – add the things you usually do (Work, Gym, Cooking…) in the *Activities* tab. Nothing is pre-filled.
- **Pop-up prompt on the hour** – a small dialog appears over whatever you're doing (even on the lock screen) with your activities as chips and an optional comment box. You don't have to open the app.
- **Adjustable interval** – every 15 / 30 / 45 / 60 / 90 / 120 / 180 minutes.
- **Active hours (sleep window)** – set a start and end time (default 07:00–22:00). No prompts fire outside it. Windows that cross midnight (e.g. 18:00–04:00) work too.
- **Today** – a greeting with the day's one fact, the day as a strip of check-in slots (answered / missed / still to come), where the time went, and the full log with your comments. Step back through earlier days with ‹ ›.
- **History** – 7 / 30 / 90 days as stacked columns of time per day, stacked by activity. Tap a day for its breakdown; tap an activity to see only it in the chart. A ranked list shows each activity's total and share.
- **Looks** – "Editorial greeting" app style (Instrument Serif + Instrument Sans, one deep-teal accent, tonal panels, no shadows or gradients), a custom icon, and a short animated splash.
- Survives reboots and app updates (alarms are re-registered automatically).

## How the prompt works

| Piece | What it does |
|---|---|
| `AlarmManager.SetExactAndAllowWhileIdle` | Wakes the app at the next clock boundary (falls back to an inexact alarm if exact alarms are denied). |
| `AlarmReceiver` | Re-arms the next alarm, then shows the prompt. |
| `PromptActivity` | The pop-up itself – a separate lightweight Activity (`ShowWhenLocked`), not the MAUI app. |
| Full-screen-intent notification | Always posted too, so the prompt is never lost if Android blocks the direct pop-up. |
| `BootReceiver` | Re-schedules after reboot / app update / exact-alarm permission change. |

A prompt at time *t* asks about the slot `[t − interval, t]` and only fires if *t* is inside the active window `(start, end]`, so with the defaults the first prompt of the day is 08:00 (covering 07:00–08:00) and the last is 22:00.

## Permissions

The *Settings* tab shows which of these are missing and links straight to the right system screen:

- **Notifications** – required to show anything.
- **Exact alarms** – so prompts land on the hour rather than a few minutes late.
- **Display over other apps** – lets the prompt pop up on top of the app you're using. Without it Android shows a heads-up notification instead (tap to answer).
- **Full-screen prompts** – lets the prompt take over a locked screen.

> Android restricts apps from launching screens from the background, and the rules differ between versions and manufacturers.
> Use **Settings → Send a test prompt now** on your own phone to see exactly what you get.

## Build & run

Requirements: .NET 10 SDK with the `android` and `maui` workloads, and an Android device or emulator (Android 8.0 / API 26+).

```powershell
dotnet build -f net10.0-android
dotnet build -t:Run -f net10.0-android   # deploys to the connected device/emulator
```

## Project layout

```
Models/        SQLite entities (ActivityType, LogEntry)
Controls/      Chart views drawn in code (stacked chart, bars, day strip)
Services/      TrackerDb (SQLite), TrackerSettings (prefs), ScheduleCalculator (pure logic)
ViewModels/    Today, Activities, Settings (CommunityToolkit.Mvvm)
Pages/         XAML pages
Platforms/Android/   alarms, receivers, pop-up Activity, permission helpers
```

Data is stored locally on the device only (`tracker.db3`). Nothing is uploaded.

## Charts

Drawn with `Microsoft.Maui.Graphics` (`GraphicsView` + `IDrawable`), no charting library. Colours follow the dataviz rules: activities take fixed categorical slots (blue, orange, aqua, yellow, magenta, then grey "Other"), validated for colour-blind separation; three of those sit under 3:1 contrast on white, so the chart always carries a legend and a written breakdown of the selected day.

## Reviewing the charts without waiting a month

In **Debug** builds, Settings has *Load sample history (debug)*, which replaces all data with five weeks of believable entries. It does not exist in Release builds.

## Brand assets

`assets-work/` holds the brief, style, the icon/splash options that were considered, and the script that regenerates them. See `assets-work/ASSETS.md`.
