namespace Teyemer.Core;

public enum ReminderState { Running, Paused, InactiveSchedule, SessionInactive, ReminderDue, Exercising, Snoozed }

public sealed record ReminderSnapshot(ReminderState State, DateTimeOffset Now, DateTimeOffset? NextReminder,
    DateTimeOffset? ResumeAt, DateTimeOffset? NextActiveStart)
{
    public TimeSpan? Remaining => NextReminder is null ? null : NextReminder > Now ? NextReminder - Now : TimeSpan.Zero;
}

public sealed class ReminderEngine
{
    private AppSettings _settings;
    private DateTimeOffset? _cycleStartedAt;
    private DateTimeOffset? _pauseUntil;
    private DateTimeOffset? _snoozeUntil;
    private bool _sessionActive = true;
    private ReminderState _state = ReminderState.Running;

    public ReminderEngine(AppSettings settings, DateTimeOffset now)
    {
        _settings = settings;
        RestartCycle(now);
    }

    public ReminderState State => _state;

    public void UpdateSettings(AppSettings settings, DateTimeOffset now)
    {
        _settings = settings;
        RestartCycle(now);
    }

    public void PauseUntil(DateTimeOffset until) { _pauseUntil = until; _state = ReminderState.Paused; }
    public void Snooze(DateTimeOffset now, TimeSpan delay) { _snoozeUntil = now + delay; _state = ReminderState.Snoozed; }
    public void StartExercise() => _state = ReminderState.Exercising;
    public void CompleteExercise(DateTimeOffset now) { _state = ReminderState.Running; RestartCycle(now); }
    public void Skip(DateTimeOffset now) { _state = ReminderState.Running; RestartCycle(now); }

    public void SetSessionActive(bool active, DateTimeOffset now)
    {
        _sessionActive = active;
        _state = active ? ReminderState.Running : ReminderState.SessionInactive;
        _pauseUntil = null;
        _snoozeUntil = null;
        RestartCycle(now);
    }

    public ReminderSnapshot Tick(DateTimeOffset now)
    {
        if (!_settings.RemindersEnabled)
            return Snapshot(ReminderState.Paused, now, null, null, null);
        if (!_sessionActive)
            return Snapshot(ReminderState.SessionInactive, now, null, null, null);
        if (_state == ReminderState.Exercising)
            return Snapshot(_state, now, null, null, null);

        var schedule = ScheduleEvaluator.Evaluate(now, _settings);
        if (!schedule.IsActive)
        {
            _cycleStartedAt = null;
            _state = ReminderState.InactiveSchedule;
            return Snapshot(_state, now, null, null, schedule.NextActiveStart);
        }

        if (_cycleStartedAt is null)
        {
            _cycleStartedAt = now;
            _state = ReminderState.Running;
        }

        if (_pauseUntil is not null)
        {
            if (now < _pauseUntil)
                return Snapshot(ReminderState.Paused, now, null, _pauseUntil, null);
            _pauseUntil = null;
            RestartCycle(now);
        }

        if (_snoozeUntil is not null)
        {
            if (now < _snoozeUntil)
                return Snapshot(ReminderState.Snoozed, now, _snoozeUntil, null, null);
            _snoozeUntil = null;
            _state = ReminderState.ReminderDue;
            return Snapshot(_state, now, now, null, null);
        }

        var due = _cycleStartedAt + TimeSpan.FromMinutes(_settings.ReminderIntervalMinutes);
        if (now >= due && _state != ReminderState.ReminderDue)
            _state = ReminderState.ReminderDue;
        else if (_state != ReminderState.ReminderDue)
            _state = ReminderState.Running;
        return Snapshot(_state, now, due, null, null);
    }

    private void RestartCycle(DateTimeOffset now) => _cycleStartedAt = now;
    private static ReminderSnapshot Snapshot(ReminderState state, DateTimeOffset now, DateTimeOffset? next,
        DateTimeOffset? resume, DateTimeOffset? active) => new(state, now, next, resume, active);
}
