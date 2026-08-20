using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Teyemer.Core;

namespace Teyemer.App;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly AppController _controller;
    private readonly AppSettings _settings;
    private string _statusText = "준비 중";
    private string? _message;
    private CustomAlarmSetting? _selectedAlarm;

    public MainViewModel(AppController controller, AppSettings settings)
    {
        _controller = controller;
        _settings = settings;
        SaveCommand = new RelayCommand(Save);
        PreviewSoundCommand = new RelayCommand(controller.PlaySelectedSound);
        PreviewReminderCommand = new RelayCommand(controller.PreviewReminder);
        AddAlarmCommand = new RelayCommand(AddAlarm);
        EditAlarmCommand = new RelayCommand(EditAlarm);
        DeleteAlarmCommand = new RelayCommand(DeleteAlarm);
    }

    public AppSettings Settings => _settings;
    public int ReminderIntervalMinutes { get => _settings.ReminderIntervalMinutes; set { _settings.ReminderIntervalMinutes = value; Changed(); } }
    public int PopupDismissSeconds { get => _settings.PopupDismissSeconds; set { _settings.PopupDismissSeconds = value; Changed(); } }

    public bool UseDarkMode
    {
        get => _settings.UseDarkMode;
        set
        {
            if (_settings.UseDarkMode == value) return;
            _settings.UseDarkMode = value;
            _controller.PreviewTheme(value);
            Changed();
        }
    }

    public Array Sounds => Enum.GetValues<NotificationSound>();
    public CustomAlarmSetting? SelectedAlarm { get => _selectedAlarm; set { _selectedAlarm = value; Changed(); } }
    public string StatusText { get => _statusText; set { _statusText = value; Changed(); } }
    public string? Message { get => _message; set { _message = value; Changed(); } }
    public ICommand SaveCommand { get; }
    public ICommand PreviewSoundCommand { get; }
    public ICommand PreviewReminderCommand { get; }
    public ICommand AddAlarmCommand { get; }
    public ICommand EditAlarmCommand { get; }
    public ICommand DeleteAlarmCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    private async void Save()
    {
        if (ReminderIntervalMinutes is < AppSettings.MinReminderMinutes or > AppSettings.MaxReminderMinutes)
        { Message = $"알림 주기는 {AppSettings.MinReminderMinutes}~{AppSettings.MaxReminderMinutes}분이어야 합니다."; return; }
        if (PopupDismissSeconds is < AppSettings.MinPopupDismissSeconds or > AppSettings.MaxPopupDismissSeconds)
        { Message = $"알림 자동 닫힘 시간은 {AppSettings.MinPopupDismissSeconds}~{AppSettings.MaxPopupDismissSeconds}초여야 합니다."; return; }
        if (_settings.CustomAlarms.Any(alarm => alarm.Type == CustomAlarmType.Periodic && alarm.IntervalMinutes is < 1 or > 1440))
        { Message = "사용자 알람 주기는 1~1440분이어야 합니다."; return; }
        if (_settings.CustomAlarms.Any(alarm => string.IsNullOrWhiteSpace(alarm.Content)))
        { Message = "사용자 알람에 표시할 문구를 입력하세요."; return; }
        try { await _controller.SaveSettingsAsync(_settings); Message = "설정을 저장했습니다."; }
        catch (Exception ex) { Message = $"저장하지 못했습니다: {ex.Message}"; }
    }

    private async void AddAlarm()
    {
        var alarm = _controller.ShowCustomAlarmEditor(null);
        if (alarm is null) return;
        _settings.CustomAlarms.Add(alarm);
        SelectedAlarm = alarm;
        try { await _controller.SaveSettingsAsync(_settings); Message = "사용자 알람을 추가했습니다."; }
        catch (Exception ex) { _settings.CustomAlarms.Remove(alarm); SelectedAlarm = null; Message = $"알람을 저장하지 못했습니다: {ex.Message}"; }
    }

    private async void EditAlarm()
    {
        if (SelectedAlarm is null) { Message = "설정할 알람을 선택하세요."; return; }
        var index = _settings.CustomAlarms.IndexOf(SelectedAlarm);
        var edited = _controller.ShowCustomAlarmEditor(SelectedAlarm);
        if (edited is null || index < 0) return;
        var original = SelectedAlarm;
        _settings.CustomAlarms[index] = edited;
        SelectedAlarm = edited;
        try { await _controller.SaveSettingsAsync(_settings); Message = "사용자 알람 설정을 저장했습니다."; }
        catch (Exception ex) { _settings.CustomAlarms[index] = original; SelectedAlarm = original; Message = $"알람을 저장하지 못했습니다: {ex.Message}"; }
    }

    private async void DeleteAlarm()
    {
        if (SelectedAlarm is null) return;
        var alarm = SelectedAlarm;
        var index = _settings.CustomAlarms.IndexOf(alarm);
        _settings.CustomAlarms.Remove(alarm);
        SelectedAlarm = null;
        try { await _controller.SaveSettingsAsync(_settings); Message = "사용자 알람을 삭제했습니다."; }
        catch (Exception ex)
        {
            if (index >= 0 && index <= _settings.CustomAlarms.Count) _settings.CustomAlarms.Insert(index, alarm);
            else _settings.CustomAlarms.Add(alarm);
            SelectedAlarm = alarm;
            Message = $"알람을 삭제하지 못했습니다: {ex.Message}";
        }
    }

    public async Task SetAlarmEnabledAsync(CustomAlarmSetting alarm, bool enabled)
    {
        if (!_settings.CustomAlarms.Contains(alarm)) return;
        var previous = !enabled;
        alarm.IsEnabled = enabled;
        try
        {
            await _controller.SaveSettingsAsync(_settings);
            Message = enabled ? "사용자 알람을 활성화했습니다." : "사용자 알람을 비활성화했습니다.";
        }
        catch (Exception ex)
        {
            alarm.IsEnabled = previous;
            Message = $"알람 상태를 저장하지 못했습니다: {ex.Message}";
        }
    }

    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
