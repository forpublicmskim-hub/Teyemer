using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Teyemer.Core;

namespace Teyemer.App;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AppController _controller;
    private AppSettings _settings;
    private string _statusText = "준비 중";
    private string? _message;
    public MainViewModel(AppController controller, AppSettings settings)
    {
        _controller = controller; _settings = settings;
        SaveCommand = new RelayCommand(Save); ExerciseNowCommand = new RelayCommand(controller.StartExercise);
        PreviewSoundCommand = new RelayCommand(controller.PlaySelectedSound); PreviewReminderCommand = new RelayCommand(controller.PreviewReminder); ApplyWeekdaysCommand = new RelayCommand(ApplyWeekdays);
    }
    public AppSettings Settings => _settings;
    public int ReminderIntervalMinutes { get => _settings.ReminderIntervalMinutes; set { _settings.ReminderIntervalMinutes = value; Changed(); } }
    public int ExerciseDurationSeconds { get => _settings.ExerciseDurationSeconds; set { _settings.ExerciseDurationSeconds = value; Changed(); } }
    public int PopupDismissSeconds { get => _settings.PopupDismissSeconds; set { _settings.PopupDismissSeconds = value; Changed(); } }
    public Array Sounds => Enum.GetValues<NotificationSound>();
    public IEnumerable<ScheduleRow> ScheduleRows => Enum.GetValues<DayOfWeek>().Select(d => new ScheduleRow(d, _settings.Schedule[d]));
    public string StatusText { get => _statusText; set { _statusText = value; Changed(); } }
    public string? Message { get => _message; set { _message = value; Changed(); } }
    public ICommand SaveCommand { get; } public ICommand ExerciseNowCommand { get; }
    public ICommand PreviewSoundCommand { get; } public ICommand ApplyWeekdaysCommand { get; }
    public ICommand PreviewReminderCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    private async void Save()
    {
        if (ReminderIntervalMinutes is < AppSettings.MinReminderMinutes or > AppSettings.MaxReminderMinutes)
        { Message = $"알림 주기는 {AppSettings.MinReminderMinutes}~{AppSettings.MaxReminderMinutes}분이어야 합니다."; return; }
        if (ExerciseDurationSeconds is < AppSettings.MinExerciseSeconds or > AppSettings.MaxExerciseSeconds)
        { Message = $"운동 시간은 {AppSettings.MinExerciseSeconds}~{AppSettings.MaxExerciseSeconds}초여야 합니다."; return; }
        if (PopupDismissSeconds is < AppSettings.MinPopupDismissSeconds or > AppSettings.MaxPopupDismissSeconds)
        { Message = $"알림 자동 닫힘 시간은 {AppSettings.MinPopupDismissSeconds}~{AppSettings.MaxPopupDismissSeconds}초여야 합니다."; return; }
        try { await _controller.SaveSettingsAsync(_settings); Message = "설정을 저장했습니다."; }
        catch (Exception ex) { Message = $"저장하지 못했습니다: {ex.Message}"; }
    }
    private void ApplyWeekdays()
    {
        var monday = _settings.Schedule[DayOfWeek.Monday];
        foreach (var day in new[] { DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        { var target = _settings.Schedule[day]; target.IsEnabled = monday.IsEnabled; target.Start = monday.Start; target.End = monday.End; }
        Changed(nameof(ScheduleRows));
    }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}

public sealed class ScheduleRow
{
    private readonly DailySchedule _slot;
    public ScheduleRow(DayOfWeek day, DailySchedule slot) { Day = Names[(int)day]; _slot = slot; }
    private static readonly string[] Names = ["일요일", "월요일", "화요일", "수요일", "목요일", "금요일", "토요일"];
    public string Day { get; }
    public bool IsEnabled { get => _slot.IsEnabled; set => _slot.IsEnabled = value; }
    public string Start { get => _slot.Start.ToString("HH:mm"); set { if (TimeOnly.TryParse(value, out var t)) _slot.Start = t; } }
    public string End { get => _slot.End.ToString("HH:mm"); set { if (TimeOnly.TryParse(value, out var t)) _slot.End = t; } }
}
