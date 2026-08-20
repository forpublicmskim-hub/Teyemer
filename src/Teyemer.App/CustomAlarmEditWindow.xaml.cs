using System.Windows;
using Teyemer.Core;

namespace Teyemer.App;

public partial class CustomAlarmEditWindow : Window
{
    public CustomAlarmEditWindow(CustomAlarmSetting? source)
    {
        InitializeComponent();
        Result = source is null ? new CustomAlarmSetting() : Clone(source);
        DataContext = Result;
        Result.PropertyChanged += OnAlarmPropertyChanged;
        Closed += OnClosed;
        UpdateScheduleEditor();
        ThemeService.Attach(this);
    }

    public CustomAlarmSetting Result { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (Result.Type == CustomAlarmType.Periodic && Result.IntervalMinutes is < 1 or > 1440)
        { ValidationMessage.Text = "주기는 1~1440분이어야 합니다."; return; }
        if (string.IsNullOrWhiteSpace(Result.Content))
        { ValidationMessage.Text = "알람에 표시할 문구를 입력하세요."; return; }
        Result.Content = Result.Content.Trim();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnAlarmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CustomAlarmSetting.Type)) UpdateScheduleEditor();
    }

    private void UpdateScheduleEditor()
    {
        var daily = Result.Type == CustomAlarmType.DailyTime;
        ScheduleTitle.Text = daily ? "시간" : "주기";
        PeriodicEditor.Visibility = daily ? Visibility.Collapsed : Visibility.Visible;
        DailyEditor.Visibility = daily ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Result.PropertyChanged -= OnAlarmPropertyChanged;
        Closed -= OnClosed;
    }

    private static CustomAlarmSetting Clone(CustomAlarmSetting source) => new()
    {
        Id = source.Id,
        IsEnabled = source.IsEnabled,
        Type = source.Type,
        IntervalMinutes = source.IntervalMinutes,
        Time = source.Time,
        Content = source.Content
    };
}
