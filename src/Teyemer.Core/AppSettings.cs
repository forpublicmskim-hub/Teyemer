using System.Text.Json.Serialization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Teyemer.Core;

public sealed class AppSettings
{
    public const int MinReminderMinutes = 1;
    public const int MaxReminderMinutes = 240;
    public const int DefaultExerciseDurationSeconds = 20;
    public const int MinPopupDismissSeconds = 1;
    public const int MaxPopupDismissSeconds = 60;

    public int ReminderIntervalMinutes { get; set; } = 20;
    public bool ShowPopup { get; set; } = true;
    public bool PlaySound { get; set; } = true;
    public NotificationSound Sound { get; set; } = NotificationSound.Asterisk;
    public int PopupDismissSeconds { get; set; } = 30;
    public bool UseDarkMode { get; set; } = true;
    public bool ActiveScheduleEnabled { get; set; } = true;
    public TimeOnly ActiveStart { get; set; } = new(9, 0);
    public TimeOnly ActiveEnd { get; set; } = new(18, 0);
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; } = true;
    public ObservableCollection<CustomAlarmSetting> CustomAlarms { get; set; } = [];

    [JsonPropertyName("RemindersEnabled"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LegacyRemindersEnabled { get; set; }

    [JsonPropertyName("ExerciseDurationSeconds"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LegacyExerciseDurationSeconds { get; set; }

    [JsonPropertyName("InactiveScheduleEnabled"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? LegacyInactiveScheduleEnabled { get; set; }

    [JsonPropertyName("InactiveSchedule"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<DayOfWeek, DailySchedule>? LegacyInactiveSchedule { get; set; }

    [JsonPropertyName("Schedule"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<DayOfWeek, DailySchedule>? LegacyActiveSchedule { get; set; }

    public static AppSettings CreateDefault() => new();

    public void Normalize()
    {
        ReminderIntervalMinutes = Math.Clamp(ReminderIntervalMinutes, MinReminderMinutes, MaxReminderMinutes);
        PopupDismissSeconds = Math.Clamp(PopupDismissSeconds, MinPopupDismissSeconds, MaxPopupDismissSeconds);
        CustomAlarms ??= [];
        foreach (var alarm in CustomAlarms)
        {
            alarm.Id = alarm.Id == Guid.Empty ? Guid.NewGuid() : alarm.Id;
            alarm.IntervalMinutes = Math.Clamp(alarm.IntervalMinutes, 1, 1440);
            alarm.Content = string.IsNullOrWhiteSpace(alarm.Content) ? "알람 시간입니다." : alarm.Content.Trim()[..Math.Min(alarm.Content.Trim().Length, 200)];
        }

        if (LegacyInactiveSchedule is not null)
            MigrateInactiveSchedule();
        else if (LegacyActiveSchedule is not null)
            MigrateActiveSchedule();

        // Reminders are always enabled in the current product model.
        LegacyRemindersEnabled = null;
        LegacyExerciseDurationSeconds = null;
        LegacyInactiveScheduleEnabled = null;
        LegacyInactiveSchedule = null;
        LegacyActiveSchedule = null;
    }

    private void MigrateInactiveSchedule()
    {
        ActiveScheduleEnabled = LegacyInactiveScheduleEnabled ?? true;
        if (!ActiveScheduleEnabled || LegacyInactiveSchedule is null)
            return;

        var inactive = SelectRepresentativeSlot(LegacyInactiveSchedule);
        if (inactive is null || !inactive.IsEnabled || inactive.Start == inactive.End)
            return;

        ActiveStart = inactive.End;
        ActiveEnd = inactive.Start;
    }

    private void MigrateActiveSchedule()
    {
        if (LegacyActiveSchedule is null)
            return;

        var active = SelectRepresentativeSlot(LegacyActiveSchedule, requireEnabled: true);
        if (active is null)
            return;

        ActiveStart = active.Start;
        ActiveEnd = active.End;
    }

    private static DailySchedule? SelectRepresentativeSlot(
        IReadOnlyDictionary<DayOfWeek, DailySchedule> schedule,
        bool requireEnabled = false)
    {
        if (schedule.TryGetValue(DayOfWeek.Monday, out var monday)
            && (!requireEnabled || monday.IsEnabled))
            return monday;

        return schedule.Values.FirstOrDefault(slot => !requireEnabled || slot.IsEnabled);
    }
}

public sealed class DailySchedule
{
    public bool IsEnabled { get; set; }
    public TimeOnly Start { get; set; } = new(9, 0);
    public TimeOnly End { get; set; } = new(18, 0);
}

public enum NotificationSound { Asterisk, Exclamation, Beep }
public enum CustomAlarmType { Periodic, DailyTime }

public sealed class CustomAlarmSetting : INotifyPropertyChanged
{
    private Guid _id = Guid.NewGuid();
    private bool _isEnabled = true;
    private CustomAlarmType _type;
    private int _intervalMinutes = 60;
    private TimeOnly _time = new(9, 0);
    private string _content = "알람 시간입니다.";
    public Guid Id { get => _id; set => Set(ref _id, value); }
    public bool IsEnabled { get => _isEnabled; set => Set(ref _isEnabled, value); }
    public CustomAlarmType Type { get => _type; set => Set(ref _type, value); }
    public int IntervalMinutes { get => _intervalMinutes; set => Set(ref _intervalMinutes, value); }
    public TimeOnly Time { get => _time; set => Set(ref _time, value); }
    public string Content { get => _content; set => Set(ref _content, value); }
    [JsonIgnore] public string TypeLabel => Type == CustomAlarmType.Periodic ? "주기적" : "매일 특정 시간";
    [JsonIgnore] public string ScheduleLabel => Type == CustomAlarmType.Periodic ? $"{IntervalMinutes}분마다" : Time.ToString("HH:mm");
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        if (name is nameof(Type)) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TypeLabel)));
        if (name is nameof(Type) or nameof(IntervalMinutes) or nameof(Time))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScheduleLabel)));
    }
}
