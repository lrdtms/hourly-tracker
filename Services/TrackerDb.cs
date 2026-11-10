using HourlyTracker.Models;
using SQLite;

namespace HourlyTracker.Services;

/// <summary>
/// SQLite storage. A process-wide singleton (not DI-only) because the Android pop-up
/// Activity and alarm receivers need it without going through the MAUI page tree.
/// </summary>
public sealed class TrackerDb
{
    public const string UnspecifiedName = "Other";

    public static TrackerDb Instance { get; } = new();

    private readonly SemaphoreSlim _gate = new(1, 1);
    private SQLiteAsyncConnection? _db;

    private static string DbPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tracker.db3");

    private async Task<SQLiteAsyncConnection> Db()
    {
        if (_db is not null) return _db;
        await _gate.WaitAsync();
        try
        {
            if (_db is null)
            {
                var conn = new SQLiteAsyncConnection(DbPath,
                    SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
                await conn.CreateTableAsync<ActivityType>();
                await conn.CreateTableAsync<LogEntry>();
                _db = conn;
            }
            return _db;
        }
        finally { _gate.Release(); }
    }

    public async Task<List<ActivityType>> GetActivitiesAsync()
    {
        var db = await Db();
        return await db.Table<ActivityType>().OrderBy(a => a.Name).ToListAsync();
    }

    /// <returns>false if the name is blank or already exists (case-insensitive).</returns>
    public async Task<bool> AddActivityAsync(string name)
    {
        name = name.Trim();
        if (name.Length == 0) return false;
        var db = await Db();
        var existing = await db.Table<ActivityType>().ToListAsync();
        if (existing.Any(a => string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase))) return false;
        await db.InsertAsync(new ActivityType { Name = name });
        return true;
    }

    public async Task DeleteActivityAsync(int id)
    {
        var db = await Db();
        await db.DeleteAsync<ActivityType>(id);
    }

    public async Task AddEntryAsync(LogEntry entry)
    {
        var db = await Db();
        entry.CreatedAt = DateTime.Now;
        if (string.IsNullOrWhiteSpace(entry.ActivityName)) entry.ActivityName = UnspecifiedName;
        await db.InsertAsync(entry);
    }

    public async Task DeleteEntryAsync(int id)
    {
        var db = await Db();
        await db.DeleteAsync<LogEntry>(id);
    }

    /// <summary>Entries whose slot ends on the given calendar day.</summary>
    public async Task<List<LogEntry>> GetEntriesForDayAsync(DateTime day)
    {
        var db = await Db();
        var start = day.Date;
        var end = start.AddDays(1);
        return await db.Table<LogEntry>()
            .Where(e => e.SlotEnd > start && e.SlotEnd <= end)
            .OrderBy(e => e.SlotEnd)
            .ToListAsync();
    }
}
