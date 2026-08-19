namespace Teyemer.Core;

public sealed class AppSettings
{
    public const int MinReminderMinutes = 1;
    public const int MaxReminderMinutes = 240;
    public const int MinExerciseSeconds = 5;
    public const int MaxExerciseSeconds = 600;
    public const int MinPopupDismissSeconds = 1;
    public const int MaxPopupDismissSeconds = 60;

    public bool RemindersEnabled { get; set; } = true;
    public int ReminderIntervalMinutes { get; set; } = 20;
    public int ExerciseDurationSeconds { get; set; } = 20;
    public bool ShowPopup { get; set; } = true;
    public bool PlaySound { get; set; } = true;
    public NotificationSound Sound { get; set; } = NotificationSound.Asterisk;
    public int PopupDismissSeconds { get; set; } = 30;
    public bool UseDarkMode { get; set; }
    public bool ActiveScheduleEnabled { get; set; } = true;
    public Dictionary<DayOfWeek, DailySchedule> Schedule { get; set; } = CreateDefaultSchedule();
    public bool StartWithWindows { get; set; }
    public bool StartMinimized { get; set; } = true;

    public static AppSettings CreateDefault() => new();

    public static Dictionary<DayOfWeek, DailySchedule> CreateDefaultSchedule() =>
        Enum.GetValues<DayOfWeek>().ToDictionary(
            day => day,
            day => new DailySchedule
            {
                IsEnabled = day is >= DayOfWeek.Monday and <= DayOfWeek.Friday,
                Start = new TimeOnly(9, 0),
                End = new TimeOnly(18, 0)
            });

    public void Normalize()
    {
        ReminderIntervalMinutes = Math.Clamp(ReminderIntervalMinutes, MinReminderMinutes, MaxReminderMinutes);
        ExerciseDurationSeconds = Math.Clamp(ExerciseDurationSeconds, MinExerciseSeconds, MaxExerciseSeconds);
        PopupDismissSeconds = Math.Clamp(PopupDismissSeconds, MinPopupDismissSeconds, MaxPopupDismissSeconds);
        Schedule ??= CreateDefaultSchedule();
        foreach (var day in Enum.GetValues<DayOfWeek>())
            Schedule.TryAdd(day, new DailySchedule());
    }
}

public sealed class DailySchedule
{
    public bool IsEnabled { get; set; }
    public TimeOnly Start { get; set; } = new(9, 0);
    public TimeOnly End { get; set; } = new(18, 0);
}

public enum NotificationSound { Asterisk, Exclamation, Beep }
