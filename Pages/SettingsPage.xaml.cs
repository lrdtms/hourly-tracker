using HourlyTracker.ViewModels;

namespace HourlyTracker.Pages;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;

    public SettingsPage(SettingsViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    // Returning from a system settings screen lands here, so permission status refreshes automatically.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _vm.RefreshStatus();
    }
}
