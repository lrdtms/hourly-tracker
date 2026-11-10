using HourlyTracker.Models;

namespace HourlyTracker.Services;

/// <summary>
/// Developer helper: fills the app with 5 weeks of believable history so charts can be reviewed.
/// Only reachable from a button that exists in DEBUG builds (Settings tab). It REPLACES existing data.
/// </summary>
public static class SampleData
{
    public static async Task LoadAsync()
    {
        var db = TrackerDb.Instance;
        await db.ClearAllAsync();

        string[] names = { "Work", "Gym", "Cooking", "Commute", "Reading", "Friends" };
        var activities = names.Select(n => new ActivityType { Name = n }).ToList();

        var rng = new Random(42);
        var entries = new List<LogEntry>();
        var today = DateTime.Today;
        var now = DateTime.Now;

        for (int d = 34; d >= 0; d--)
        {
            var day = today.AddDays(-d);
            bool weekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            for (int hour = 8; hour <= 22; hour++)
            {
                var end = day.AddHours(hour);
                if (end > now) break;
                if (rng.NextDouble() < 0.07) continue; // a missed check-in now and then

                string name;
                string comment = "";
                if (weekend)
                {
                    name = hour switch
                    {
                        <= 9 => "Cooking",
                        <= 12 => rng.NextDouble() < .55 ? "Gym" : "Reading",
                        <= 16 => rng.NextDouble() < .6 ? "Friends" : "Reading",
                        <= 19 => rng.NextDouble() < .5 ? "Friends" : "Cooking",
                        _ => "Reading",
                    };
                }
                else
                {
                    name = hour switch
                    {
                        8 or 18 => "Commute",
                        9 or 10 or 11 or 13 or 14 or 15 or 16 or 17 => rng.NextDouble() < .93 ? "Work" : "Commute",
                        12 => "Cooking",
                        19 => day.DayOfWeek is DayOfWeek.Tuesday or DayOfWeek.Thursday ? "Gym" : "Cooking",
                        20 => "Friends",
                        _ => "Reading",
                    };
                }

                if (name == "Work" && rng.NextDouble() < .08) comment = "Planning and review";
                if (name == "Gym") comment = "Legs";

                entries.Add(new LogEntry
                {
                    SlotStart = end.AddHours(-1),
                    SlotEnd = end,
                    ActivityName = name,
                    ActivityId = null,
                    Comment = comment,
                    CreatedAt = end,
                });
            }
        }

        await db.InsertManyAsync(activities, entries);
    }
}
