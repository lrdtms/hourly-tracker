using System.ComponentModel;
using HourlyTracker.ViewModels;

namespace HourlyTracker.Pages;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel _vm;
    private readonly Dictionary<RangeOption, Button> _rangeButtons = new();

    public HistoryPage(HistoryViewModel vm)
    {
        InitializeComponent();
        BindingContext = _vm = vm;

        BuildRangeButtons();
        Chart.DayTapped += (_, i) => _vm.SelectDayCommand.Execute(i);
        _vm.PropertyChanged += OnVmChanged;
        _vm.Legend.CollectionChanged += (_, _) => BuildLegend();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
    }

    private void OnVmChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(HistoryViewModel.Chart):
            case nameof(HistoryViewModel.SelectedDay):
            case nameof(HistoryViewModel.Emphasis):
                Chart.Update(_vm.Chart, _vm.SelectedDay, _vm.Emphasis);
                break;
            case nameof(HistoryViewModel.SelectedRange):
                UpdateRangeButtons();
                break;
        }
    }

    private void BuildRangeButtons()
    {
        foreach (var option in _vm.Ranges)
        {
            var button = new Button { Text = option.Label, HeightRequest = 34, Padding = new Thickness(14, 0), FontSize = 13 };
            button.Clicked += (_, _) => _vm.SelectRangeCommand.Execute(option);
            _rangeButtons[option] = button;
            RangeRow.Add(button);
        }
        UpdateRangeButtons();
    }

    private void UpdateRangeButtons()
    {
        foreach (var (option, button) in _rangeButtons)
        {
            bool active = option == _vm.SelectedRange;
            button.BackgroundColor = active ? Colors.White : Colors.Transparent;
            button.TextColor = (Color)Application.Current!.Resources[active ? "Text" : "Muted"];
            button.FontFamily = active ? "SansMedium" : "Sans";
            button.CornerRadius = 17;
        }
    }

    // Legend: a colour dot + name for each series, in slot order.
    private void BuildLegend()
    {
        LegendRow.Clear();
        foreach (var item in _vm.Legend)
        {
            var row = new HorizontalStackLayout { Spacing = 6, Margin = new Thickness(0, 0, 14, 6) };
            row.Add(new BoxView { WidthRequest = 10, HeightRequest = 10, CornerRadius = 5, Color = item.Color, VerticalOptions = LayoutOptions.Center });
            row.Add(new Label { Text = item.Name, FontSize = 12, TextColor = (Color)Application.Current!.Resources["Muted"] });
            LegendRow.Add(row);
        }
    }
}
