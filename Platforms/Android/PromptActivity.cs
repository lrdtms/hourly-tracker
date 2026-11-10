using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Views;
using AView = Android.Views.View;
using AScrollView = Android.Widget.ScrollView;
using Android.Widget;
using AndroidX.AppCompat.App;
using Google.Android.Material.Button;
using Google.Android.Material.Chip;
using HourlyTracker.Models;
using HourlyTracker.Services;

namespace HourlyTracker;

/// <summary>
/// The small pop-up asking "what did you do?". It is its own Activity (not the MAUI MainActivity),
/// so it appears on top of the lock screen / other apps without launching the full app.
/// </summary>
[Activity(
    Theme = "@style/PromptTheme",
    Exported = false,
    LaunchMode = LaunchMode.SingleTop,
    TaskAffinity = "",
    ExcludeFromRecents = true,
    ShowWhenLocked = true,
    TurnScreenOn = true)]
public class PromptActivity : AppCompatActivity
{
    public const string ExtraSlotStartTicks = "slot_start_ticks";
    public const string ExtraSlotEndTicks = "slot_end_ticks";

    private DateTime _slotStart;
    private DateTime _slotEnd;
    private TextView _subtitle = null!;
    private ChipGroup _chips = null!;
    private TextView _emptyHint = null!;
    private EditText _comment = null!;
    private readonly Dictionary<int, ActivityType> _chipToActivity = new();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        ReadSlot(Intent);
        SetFinishOnTouchOutside(false); // an accidental tap outside shouldn't lose the prompt
        BuildUi();
        _ = LoadActivitiesAsync();
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        ReadSlot(intent);
        _subtitle.Text = SlotText();
        _comment.Text = "";
        _ = LoadActivitiesAsync();
    }

    private void ReadSlot(Intent? intent)
    {
        long end = intent?.GetLongExtra(ExtraSlotEndTicks, 0) ?? 0;
        long start = intent?.GetLongExtra(ExtraSlotStartTicks, 0) ?? 0;
        _slotEnd = end > 0 ? new DateTime(end) : DateTime.Now;
        _slotStart = start > 0 ? new DateTime(start) : _slotEnd.AddMinutes(-TrackerSettings.IntervalMinutes);
    }

    private string SlotText() => $"{_slotStart:HH:mm} – {_slotEnd:HH:mm}";

    private int Dp(int value) =>
        (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, value, Resources!.DisplayMetrics);

    private void BuildUi()
    {
        Window?.SetLayout(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);

        var root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        root.SetPadding(Dp(24), Dp(24), Dp(24), Dp(16));

        var title = new TextView(this) { Text = "What did you do?", TextSize = 22 };
        title.SetTypeface(title.Typeface, Android.Graphics.TypefaceStyle.Bold);
        root.AddView(title);

        _subtitle = new TextView(this) { Text = SlotText(), TextSize = 15 };
        _subtitle.SetPadding(0, Dp(2), 0, Dp(12));
        root.AddView(_subtitle);

        _chips = new ChipGroup(this) { SingleSelection = true };
        var scroll = new AScrollView(this);
        scroll.AddView(_chips);
        root.AddView(scroll, new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

        _emptyHint = new TextView(this)
        {
            Text = "You haven't added any activities yet. Open the app and add the things you usually do — " +
                   "or just type a comment below.",
            Visibility = ViewStates.Gone,
        };
        root.AddView(_emptyHint);

        _comment = new EditText(this)
        {
            Hint = "Comment (optional)",
            InputType = InputTypes.ClassText | InputTypes.TextFlagMultiLine | InputTypes.TextFlagCapSentences,
        };
        _comment.SetMinLines(2);
        var commentParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        commentParams.TopMargin = Dp(12);
        root.AddView(_comment, commentParams);

        var buttons = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        buttons.SetGravity(GravityFlags.End);

        var skip = new MaterialButton(this, null, Android.Resource.Attribute.BorderlessButtonStyle) { Text = "Skip" };
        skip.Click += (_, _) => Finish(); // notification stays so it can still be answered later

        var save = new MaterialButton(this) { Text = "Save" };
        save.Click += async (_, _) => await SaveAsync();

        buttons.AddView(skip);
        buttons.AddView(save);
        var buttonParams = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent);
        buttonParams.TopMargin = Dp(12);
        root.AddView(buttons, buttonParams);

        SetContentView(root);
    }

    private async Task LoadActivitiesAsync()
    {
        var activities = await TrackerDb.Instance.GetActivitiesAsync();
        RunOnUiThread(() =>
        {
            _chips.RemoveAllViews();
            _chipToActivity.Clear();
            foreach (var activity in activities)
            {
                var chip = new Chip(this) { Text = activity.Name, Checkable = true, Clickable = true };
                chip.Id = AView.GenerateViewId();
                _chipToActivity[chip.Id] = activity;
                _chips.AddView(chip);
            }
            _emptyHint.Visibility = activities.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
        });
    }

    private async Task SaveAsync()
    {
        var comment = _comment.Text?.Trim() ?? "";
        _chipToActivity.TryGetValue(_chips.CheckedChipId, out var chosen);

        if (chosen is null && comment.Length == 0)
        {
            Toast.MakeText(this, "Pick an activity or add a comment", ToastLength.Short)?.Show();
            return;
        }

        await TrackerDb.Instance.AddEntryAsync(new LogEntry
        {
            SlotStart = _slotStart,
            SlotEnd = _slotEnd,
            ActivityId = chosen?.Id,
            ActivityName = chosen?.Name ?? TrackerDb.UnspecifiedName,
            Comment = comment,
        });

        PromptLauncher.Dismiss(this, _slotEnd);
        Finish();
    }
}
