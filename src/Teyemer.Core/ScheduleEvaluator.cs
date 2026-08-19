namespace Teyemer.Core;

public sealed record ScheduleStatus(bool IsActive, DateTimeOffset? NextActiveStart);

public static class ScheduleEvaluator
{
    public static ScheduleStatus Evaluate(DateTimeOffset now, AppSettings settings)
    {
        if (!settings.ActiveScheduleEnabled || IsActive(now, settings.ActiveStart, settings.ActiveEnd))
            return new(true, null);

        var todayStart = new DateTimeOffset(now.Date + settings.ActiveStart.ToTimeSpan(), now.Offset);
        var nextStart = todayStart > now ? todayStart : todayStart.AddDays(1);
        return new(false, nextStart);
    }

    public static bool IsActive(DateTimeOffset now, TimeOnly start, TimeOnly end)
    {
        if (start == end)
            return true;

        var time = TimeOnly.FromDateTime(now.LocalDateTime);
        return start < end
            ? time >= start && time < end
            : time >= start || time < end;
    }
}
