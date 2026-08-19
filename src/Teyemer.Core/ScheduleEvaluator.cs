namespace Teyemer.Core;

public sealed record ScheduleStatus(bool IsActive, DateTimeOffset? NextActiveStart);

public static class ScheduleEvaluator
{
    public static ScheduleStatus Evaluate(DateTimeOffset now, AppSettings settings)
    {
        if (!settings.ActiveScheduleEnabled)
            return new(true, null);

        if (IsActive(now, settings.Schedule))
            return new(true, null);

        for (var days = 0; days <= 7; days++)
        {
            var date = now.Date.AddDays(days);
            var day = date.DayOfWeek;
            if (!settings.Schedule.TryGetValue(day, out var slot) || !slot.IsEnabled)
                continue;

            var candidate = new DateTimeOffset(date + slot.Start.ToTimeSpan(), now.Offset);
            if (candidate > now)
                return new(false, candidate);
        }

        return new(false, null);
    }

    public static bool IsActive(DateTimeOffset now, IReadOnlyDictionary<DayOfWeek, DailySchedule> schedule)
    {
        var time = TimeOnly.FromDateTime(now.LocalDateTime);
        if (schedule.TryGetValue(now.DayOfWeek, out var today) && today.IsEnabled)
        {
            if (today.Start == today.End || (today.Start < today.End && time >= today.Start && time < today.End))
                return true;
            if (today.Start > today.End && time >= today.Start)
                return true;
        }

        var previousDay = now.AddDays(-1).DayOfWeek;
        return schedule.TryGetValue(previousDay, out var previous)
            && previous.IsEnabled
            && previous.Start > previous.End
            && time < previous.End;
    }
}
