using Teyemer.App;
using Teyemer.Core;
using Teyemer.Infrastructure;
using System.Windows.Data;
using System.IO;
using System.Threading;

var tests = new (string, Func<Task>)[]
{
    ("일반 활성 판정", () => Sync(NormalSchedule)),
    ("활성 시간 경계", () => Sync(Boundaries)),
    ("매일 동일 시간", () => Sync(EveryDay)),
    ("하루 종일 활성", () => Sync(AllDay)),
    ("자정 통과 활성", () => Sync(Overnight)),
    ("주기 변경", () => Sync(IntervalChange)),
    ("다시 알림", () => Sync(Snooze)),
    ("일시정지", () => Sync(Pause)),
    ("비활성 중 미누적", () => Sync(NoBacklog)),
    ("세션 복귀", () => Sync(SessionResume)),
    ("손상 설정", CorruptSettings),
    ("비활성 일정 변환", InactiveScheduleMigration),
    ("레거시 활성 일정 변환", ActiveScheduleMigration),
    ("알림 항상 사용", AlwaysEnabledMigration),
    ("경로 인용", () => Sync(StartupQuote)),
    ("자동 닫힘 범위", () => Sync(PopupRange)),
    ("기본 다크 모드", () => Sync(() => A.True(D().UseDarkMode))),
    ("적응형 폴링", () => Sync(AdaptivePolling))
    ,("주기 사용자 알람", () => Sync(PeriodicCustomAlarm))
    ,("특정 시간 사용자 알람", () => Sync(DailyCustomAlarm))
    ,("사용자 알람 미누적", () => Sync(CustomAlarmNoBacklog))
    ,("Daily 현재 분 저장", () => Sync(DailyCurrentMinute))
    ,("사용자 알람 유형 시간 저장", CustomAlarmSettingsRoundTrip)
    ,("사용자 알람 Picker 바인딩", CustomAlarmPickerBinding)
    ,("초 시계 강조 구간", () => Sync(SecondsClockEmphasis))
};

var failed = 0;
foreach (var test in tests)
{
    try { await test.Item2(); Console.WriteLine($"PASS {test.Item1}"); }
    catch (Exception ex) { failed++; Console.Error.WriteLine($"FAIL {test.Item1}: {ex.Message}"); }
}
Console.WriteLine($"{tests.Length - failed}/{tests.Length} tests passed");
return failed == 0 ? 0 : 1;

static Task Sync(Action action) { action(); return Task.CompletedTask; }
static AppSettings D() => AppSettings.CreateDefault();
static DateTimeOffset At(int day, int hour, int minute = 0) => new(2026, 8, day, hour, minute, 0, TimeSpan.FromHours(9));
static ReminderEngine E(DateTimeOffset now, int minutes = 20)
{
    var settings = D();
    settings.ActiveScheduleEnabled = false;
    settings.ReminderIntervalMinutes = minutes;
    return new(settings, now);
}

static void NormalSchedule()
{
    A.False(ScheduleEvaluator.Evaluate(At(17, 8, 59), D()).IsActive);
    A.True(ScheduleEvaluator.Evaluate(At(17, 10), D()).IsActive);
    A.False(ScheduleEvaluator.Evaluate(At(17, 20), D()).IsActive);
}

static void Boundaries()
{
    var settings = D();
    var before = ScheduleEvaluator.Evaluate(At(17, 8, 59), settings);
    A.False(before.IsActive);
    A.Eq(At(17, 9), before.NextActiveStart);
    A.True(ScheduleEvaluator.Evaluate(At(17, 9), settings).IsActive);
    A.False(ScheduleEvaluator.Evaluate(At(17, 18), settings).IsActive);
}

static void EveryDay()
{
    A.True(ScheduleEvaluator.Evaluate(At(16, 10), D()).IsActive);
    A.True(ScheduleEvaluator.Evaluate(At(17, 10), D()).IsActive);
    A.True(ScheduleEvaluator.Evaluate(At(18, 10), D()).IsActive);
}

static void AllDay()
{
    var settings = D();
    settings.ActiveStart = settings.ActiveEnd = new TimeOnly(9, 0);
    A.True(ScheduleEvaluator.Evaluate(At(17, 3), settings).IsActive);
    A.True(ScheduleEvaluator.Evaluate(At(17, 20), settings).IsActive);
}

static void Overnight()
{
    var settings = D();
    settings.ActiveStart = new TimeOnly(22, 0);
    settings.ActiveEnd = new TimeOnly(2, 0);
    A.True(ScheduleEvaluator.Evaluate(At(17, 23), settings).IsActive);
    A.True(ScheduleEvaluator.Evaluate(At(18, 1), settings).IsActive);
    A.False(ScheduleEvaluator.Evaluate(At(18, 2), settings).IsActive);
    A.Eq(At(18, 22), ScheduleEvaluator.Evaluate(At(18, 2), settings).NextActiveStart);
}

static void IntervalChange()
{
    var now = At(17, 10);
    var engine = E(now);
    var settings = D();
    settings.ActiveScheduleEnabled = false;
    settings.ReminderIntervalMinutes = 10;
    engine.UpdateSettings(settings, now.AddMinutes(5));
    A.Eq(ReminderState.Running, engine.Tick(now.AddMinutes(14)).State);
    A.Eq(ReminderState.ReminderDue, engine.Tick(now.AddMinutes(15)).State);
}

static void Snooze()
{
    var now = At(17, 10);
    var engine = E(now, 1);
    engine.Tick(now.AddMinutes(1));
    engine.Snooze(now.AddMinutes(1), TimeSpan.FromMinutes(5));
    A.Eq(ReminderState.Snoozed, engine.Tick(now.AddMinutes(5)).State);
    A.Eq(ReminderState.ReminderDue, engine.Tick(now.AddMinutes(6)).State);
}

static void Pause()
{
    var now = At(17, 10);
    var engine = E(now, 1);
    engine.PauseUntil(now.AddMinutes(30));
    A.Eq(ReminderState.Paused, engine.Tick(now.AddMinutes(20)).State);
    A.Eq(ReminderState.Running, engine.Tick(now.AddMinutes(30)).State);
    A.Eq(ReminderState.ReminderDue, engine.Tick(now.AddMinutes(31)).State);
}

static void NoBacklog()
{
    var settings = D();
    settings.ReminderIntervalMinutes = 1;
    var engine = new ReminderEngine(settings, At(17, 17, 59));
    A.Eq(ReminderState.InactiveSchedule, engine.Tick(At(17, 18)).State);
    A.Eq(ReminderState.Running, engine.Tick(At(18, 9)).State);
    A.Eq(ReminderState.ReminderDue, engine.Tick(At(18, 9, 1)).State);
}

static void SessionResume()
{
    var now = At(17, 10);
    var engine = E(now, 1);
    engine.SetSessionActive(false, now);
    A.Eq(ReminderState.SessionInactive, engine.Tick(now.AddMinutes(20)).State);
    engine.SetSessionActive(true, now.AddMinutes(20));
    A.Eq(ReminderState.Running, engine.Tick(now.AddMinutes(20)).State);
    A.Eq(ReminderState.ReminderDue, engine.Tick(now.AddMinutes(21)).State);
}

static async Task CorruptSettings()
{
    var path = Path.Combine(Path.GetTempPath(), $"teyemer-{Guid.NewGuid():N}.json");
    try
    {
        await File.WriteAllTextAsync(path, "{bad");
        var settings = await new JsonSettingsStore(path).LoadAsync();
        A.Eq(20, settings.ReminderIntervalMinutes);
        A.True(settings.ActiveScheduleEnabled);
        A.Eq(new TimeOnly(9, 0), settings.ActiveStart);
    }
    finally { if (File.Exists(path)) File.Delete(path); }
}

static async Task InactiveScheduleMigration()
{
    var path = Path.Combine(Path.GetTempPath(), $"teyemer-inactive-{Guid.NewGuid():N}.json");
    try
    {
        await File.WriteAllTextAsync(path, """{"RemindersEnabled":false,"ExerciseDurationSeconds":45,"InactiveScheduleEnabled":true,"InactiveSchedule":{"Monday":{"IsEnabled":true,"Start":"18:00:00","End":"09:00:00"}}}""");
        var store = new JsonSettingsStore(path);
        var settings = await store.LoadAsync();
        A.True(settings.ActiveScheduleEnabled);
        A.Eq(new TimeOnly(9, 0), settings.ActiveStart);
        A.Eq(new TimeOnly(18, 0), settings.ActiveEnd);
        await store.SaveAsync(settings);
        var json = await File.ReadAllTextAsync(path);
        A.True(json.Contains("ActiveStart"));
        A.False(json.Contains("InactiveSchedule"));
        A.False(json.Contains("RemindersEnabled"));
        A.False(json.Contains("ExerciseDurationSeconds"));
    }
    finally { DeleteSettingsFiles(path); }
}

static async Task ActiveScheduleMigration()
{
    var path = Path.Combine(Path.GetTempPath(), $"teyemer-active-{Guid.NewGuid():N}.json");
    try
    {
        await File.WriteAllTextAsync(path, """{"ActiveScheduleEnabled":true,"Schedule":{"Monday":{"IsEnabled":true,"Start":"08:30:00","End":"17:30:00"}}}""");
        var settings = await new JsonSettingsStore(path).LoadAsync();
        A.True(settings.ActiveScheduleEnabled);
        A.Eq(new TimeOnly(8, 30), settings.ActiveStart);
        A.Eq(new TimeOnly(17, 30), settings.ActiveEnd);
    }
    finally { DeleteSettingsFiles(path); }
}

static async Task AlwaysEnabledMigration()
{
    var path = Path.Combine(Path.GetTempPath(), $"teyemer-enabled-{Guid.NewGuid():N}.json");
    try
    {
        await File.WriteAllTextAsync(path, """{"RemindersEnabled":false,"ShowPopup":false,"ActiveScheduleEnabled":false,"ReminderIntervalMinutes":1}""");
        var store = new JsonSettingsStore(path);
        var settings = await store.LoadAsync();
        var engine = new ReminderEngine(settings, At(17, 10));
        A.Eq(ReminderState.ReminderDue, engine.Tick(At(17, 10, 1)).State);
        await store.SaveAsync(settings);
        A.False((await File.ReadAllTextAsync(path)).Contains("ShowPopup"));
    }
    finally { DeleteSettingsFiles(path); }
}

static void DeleteSettingsFiles(string path)
{
    if (File.Exists(path)) File.Delete(path);
    if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
}

static void StartupQuote()
{
    var command = StartupRegistrationService.BuildCommand(@"C:\Program Files\Teyemer\Teyemer.App.exe", true);
    A.True(command.StartsWith('"'));
    A.True(command.EndsWith(" --minimized"));
    A.True(command.Contains("Program Files"));
}

static void PopupRange()
{
    var settings = D();
    settings.PopupDismissSeconds = 0;
    settings.Normalize();
    A.Eq(1, settings.PopupDismissSeconds);
    settings.PopupDismissSeconds = 61;
    settings.Normalize();
    A.Eq(60, settings.PopupDismissSeconds);
}

static void AdaptivePolling()
{
    var now = At(17, 10);
    A.Eq(TimeSpan.FromSeconds(1), AppController.GetNextTickInterval(new(ReminderState.Running, now, now.AddMinutes(20), null, null), true));
    A.Eq(TimeSpan.FromSeconds(15), AppController.GetNextTickInterval(new(ReminderState.Running, now, now.AddMinutes(20), null, null), false));
    A.Eq(TimeSpan.FromSeconds(5), AppController.GetNextTickInterval(new(ReminderState.Running, now, now.AddMinutes(3), null, null), false));
    A.Eq(TimeSpan.FromSeconds(1), AppController.GetNextTickInterval(new(ReminderState.Running, now, now.AddSeconds(45), null, null), false));
    A.Eq(TimeSpan.FromSeconds(5), AppController.GetNextTickInterval(new(ReminderState.SessionInactive, now, null, null, null), false));
}

static void PeriodicCustomAlarm()
{
    var now = At(17, 10);
    var alarm = new CustomAlarmSetting { Type = CustomAlarmType.Periodic, IntervalMinutes = 10, Content = "물 마시기" };
    var scheduler = new CustomAlarmScheduler();
    scheduler.Reset([alarm], now);
    A.Eq(0, scheduler.Tick([alarm], now.AddMinutes(9), true).Count);
    var due = scheduler.Tick([alarm], now.AddMinutes(10), true);
    A.Eq(1, due.Count);
    A.Eq("물 마시기", due[0].Content);
    A.Eq(0, scheduler.Tick([alarm], now.AddMinutes(10), true).Count);
}

static void DailyCustomAlarm()
{
    var now = At(17, 8);
    var alarm = new CustomAlarmSetting { Type = CustomAlarmType.DailyTime, Time = new TimeOnly(9, 30), Content = "회의" };
    var scheduler = new CustomAlarmScheduler();
    scheduler.Reset([alarm], now);
    A.Eq(0, scheduler.Tick([alarm], At(17, 9, 29), true).Count);
    A.Eq(1, scheduler.Tick([alarm], At(17, 9, 30), true).Count);
    A.Eq(0, scheduler.Tick([alarm], At(17, 9, 31), true).Count);
    A.Eq(1, scheduler.Tick([alarm], At(18, 9, 30), true).Count);
}

static void CustomAlarmNoBacklog()
{
    var now = At(17, 8);
    var alarm = new CustomAlarmSetting { Type = CustomAlarmType.Periodic, IntervalMinutes = 5 };
    var scheduler = new CustomAlarmScheduler();
    scheduler.Reset([alarm], now);
    A.Eq(0, scheduler.Tick([alarm], At(17, 9), false).Count);
    A.Eq(0, scheduler.Tick([alarm], At(17, 9, 1), true).Count);
    A.Eq(1, scheduler.Tick([alarm], At(17, 9, 5), true).Count);
}

static void DailyCurrentMinute()
{
    var alarm = new CustomAlarmSetting { Type = CustomAlarmType.DailyTime, Time = new TimeOnly(11, 30) };
    var now = At(17, 11, 30).AddSeconds(35);
    var scheduler = new CustomAlarmScheduler();
    scheduler.Reset([alarm], now);
    A.Eq(1, scheduler.Tick([alarm], now, true).Count);
    A.Eq(0, scheduler.Tick([alarm], now.AddSeconds(1), true).Count);
}

static async Task CustomAlarmSettingsRoundTrip()
{
    var path = Path.Combine(Path.GetTempPath(), $"teyemer-custom-{Guid.NewGuid():N}.json");
    try
    {
        var settings = D();
        settings.CustomAlarms.Add(new CustomAlarmSetting
        {
            Type = CustomAlarmType.DailyTime,
            Time = new TimeOnly(14, 25),
            IntervalMinutes = 90,
            Content = "저장 확인"
        });
        var store = new JsonSettingsStore(path);
        await store.SaveAsync(settings);
        var loaded = await store.LoadAsync();
        A.Eq(1, loaded.CustomAlarms.Count);
        A.Eq(CustomAlarmType.DailyTime, loaded.CustomAlarms[0].Type);
        A.Eq(new TimeOnly(14, 25), loaded.CustomAlarms[0].Time);
        A.Eq("저장 확인", loaded.CustomAlarms[0].Content);
    }
    finally { DeleteSettingsFiles(path); }
}

static Task CustomAlarmPickerBinding()
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var alarm = new CustomAlarmSetting();
            var typePicker = new AlarmTypePicker();
            BindingOperations.SetBinding(typePicker, AlarmTypePicker.SelectedTypeProperty,
                new Binding(nameof(CustomAlarmSetting.Type)) { Source = alarm, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            typePicker.TypePicker.SelectedIndex = 1;
            A.Eq(CustomAlarmType.DailyTime, alarm.Type);

            var timePicker = new TimePicker();
            BindingOperations.SetBinding(timePicker, TimePicker.SelectedTimeProperty,
                new Binding(nameof(CustomAlarmSetting.Time)) { Source = alarm, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            timePicker.HourPicker.SelectedIndex = 14;
            timePicker.MinutePicker.SelectedIndex = 25;
            A.Eq(new TimeOnly(14, 25), alarm.Time);
        }
        catch (Exception ex) { failure = ex; }
    });
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();
    if (failure is not null) throw failure;
    return Task.CompletedTask;
}

static void SecondsClockEmphasis()
{
    var minute = new DateTime(2026, 8, 20, 17, 10, 0);
    A.Eq(0d, SecondsClockWindow.CalculateSecondEmphasis(minute.AddSeconds(54)));
    A.Eq(0d, SecondsClockWindow.CalculateSecondEmphasis(minute.AddSeconds(55)));
    A.Eq(0.5d, SecondsClockWindow.CalculateSecondEmphasis(minute.AddSeconds(57.5)));
    A.Eq(1d, SecondsClockWindow.CalculateSecondEmphasis(minute.AddMinutes(1)));
    A.Eq(0d, SecondsClockWindow.CalculateSecondEmphasis(minute.AddMinutes(1).AddMilliseconds(650)));
}

static class A
{
    public static void True(bool value) { if (!value) throw new Exception("expected true"); }
    public static void False(bool value) => True(!value);
    public static void Eq<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"expected {expected}, actual {actual}");
    }
}
