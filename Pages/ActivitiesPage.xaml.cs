using HourlyTracker.ViewModels;

namespace HourlyTracker.Pages;

public partial class ActivitiesPage : ContentPage
{
    private readonly ActivitiesViewModel _vm;

    public ActivitiesPage(ActivitiesViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }
}
