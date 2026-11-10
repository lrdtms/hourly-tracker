using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HourlyTracker.Models;
using HourlyTracker.Services;

namespace HourlyTracker.ViewModels;

public partial class ActivitiesViewModel : ObservableObject
{
    private readonly TrackerDb _db = TrackerDb.Instance;

    [ObservableProperty] private string _newName = "";
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(HasMessage))] private string _message = "";
    [ObservableProperty] private bool _isEmpty = true;
    [ObservableProperty] private string _countText = "Your list";

    public bool HasMessage => Message.Length > 0;

    public ObservableCollection<ActivityType> Items { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        Items.Clear();
        foreach (var a in await _db.GetActivitiesAsync()) Items.Add(a);
        IsEmpty = Items.Count == 0;
        CountText = Items.Count == 0 ? "Your list" : Items.Count == 1 ? "1 activity" : $"{Items.Count} activities";
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        var name = NewName.Trim();
        if (name.Length == 0) return;

        if (await _db.AddActivityAsync(name))
        {
            NewName = "";
            Message = "";
            await LoadAsync();
        }
        else
        {
            Message = $"“{name}” is already in your list.";
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(ActivityType activity)
    {
        // Past log entries keep their activity name, so history is unaffected.
        await _db.DeleteActivityAsync(activity.Id);
        await LoadAsync();
    }
}
