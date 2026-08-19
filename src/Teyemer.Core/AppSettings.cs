using System.Text.Json.Serialization;

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
