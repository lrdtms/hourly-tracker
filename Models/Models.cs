using SQLite;

namespace HourlyTracker.Models;

/// <summary>Something the user usually does (Work, Gym, Commute...). Created by the user in the app.</summary>
public class ActivityType
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

/// <summary>One answered prompt: what was done during [SlotStart, SlotEnd].</summary>
public class LogEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime SlotStart { get; set; }

    [Indexed]
    public DateTime SlotEnd { get; set; }

    public int? ActivityId { get; set; }

    /// <summary>Denormalised so history survives deleting an activity from the list.</summary>
    public string ActivityName { get; set; } = "";

    public string Comment { get; set; } = "";

    public DateTime CreatedAt { get; set; }

    [Ignore]
    public double Minutes => (SlotEnd - SlotStart).TotalMinutes;
}
