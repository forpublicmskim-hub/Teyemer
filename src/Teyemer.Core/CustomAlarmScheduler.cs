namespace Teyemer.Core;

public sealed record CustomAlarmOccurrence(Guid AlarmId, string Content);

public sealed class CustomAlarmScheduler
{
    private readonly Dictionary<Guid, DateTimeOffset> _nextDue = [];

    public void Reset(IEnumerable<CustomAlarmSetting> alarms, DateTimeOffset now)
    {
        _nextDue.Clear();
        foreach (var alarm in alarms.Where(alarm => alarm.IsEnabled))
            _nextDue[alarm.Id] = CalculateNext(alarm, now, includeCurrentMinute: true);
    }

    public IReadOnlyList<CustomAlarmOccurrence> Tick(IEnumerable<CustomAlarmSetting> alarms, DateTimeOffset now, bool sessionActive)
    {
        var enabled = alarms.Where(alarm => alarm.IsEnabled).ToDictionary(alarm => alarm.Id);
        foreach (var removed in _nextDue.Keys.Where(id => !enabled.ContainsKey(id)).ToArray()) _nextDue.Remove(removed);
        if (!sessionActive) { Reset(enabled.Values, now); return []; }

        var due = new List<CustomAlarmOccurrence>();
        foreach (var alarm in enabled.Values)
        {
            if (!_nextDue.TryGetValue(alarm.Id, out var next)) { _nextDue[alarm.Id] = CalculateNext(alarm, now, includeCurrentMinute: true); continue; }
            if (now < next) continue;
            due.Add(new(alarm.Id, alarm.Content));
            _nextDue[alarm.Id] = CalculateNext(alarm, now, includeCurrentMinute: false);
        }
        return due;
    }

    internal static DateTimeOffset CalculateNext(CustomAlarmSetting alarm, DateTimeOffset now, bool includeCurrentMinute = true)
    {
        if (alarm.Type == CustomAlarmType.Periodic)
            return now.AddMinutes(Math.Clamp(alarm.IntervalMinutes, 1, 1440));
        var today = new DateTimeOffset(now.Date + alarm.Time.ToTimeSpan(), now.Offset);
        if (includeCurrentMinute && now >= today && now < today.AddMinutes(1))
            return now;
        return today > now ? today : today.AddDays(1);
    }
}
