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

    public MainViewModel(AppController controller, AppSettings settings)
    {
        _controller = controller;
        _settings = settings;
        SaveCommand = new RelayCommand(Save);
        PreviewSoundCommand = new RelayCommand(controller.PlaySelectedSound);
        PreviewReminderCommand = new RelayCommand(controller.PreviewReminder);
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
    public string StatusText { get => _statusText; set { _statusText = value; Changed(); } }
    public string? Message { get => _message; set { _message = value; Changed(); } }
    public ICommand SaveCommand { get; }
    public ICommand PreviewSoundCommand { get; }
    public ICommand PreviewReminderCommand { get; }
    public event PropertyChangedEventHandler? PropertyChanged;

    private async void Save()
    {
        if (ReminderIntervalMinutes is < AppSettings.MinReminderMinutes or > AppSettings.MaxReminderMinutes)
        { Message = $"알림 주기는 {AppSettings.MinReminderMinutes}~{AppSettings.MaxReminderMinutes}분이어야 합니다."; return; }
        if (PopupDismissSeconds is < AppSettings.MinPopupDismissSeconds or > AppSettings.MaxPopupDismissSeconds)
        { Message = $"알림 자동 닫힘 시간은 {AppSettings.MinPopupDismissSeconds}~{AppSettings.MaxPopupDismissSeconds}초여야 합니다."; return; }
        try { await _controller.SaveSettingsAsync(_settings); Message = "설정을 저장했습니다."; }
        catch (Exception ex) { Message = $"저장하지 못했습니다: {ex.Message}"; }
    }

    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
