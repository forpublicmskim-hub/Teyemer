using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using Teyemer.Core;

namespace Teyemer.App;

public sealed class AppController : IDisposable
{
    private readonly ISettingsStore _store; private readonly IStartupRegistrationService _startup;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly Forms.NotifyIcon _tray = new() { Icon = SystemIcons.Information, Text = "Teyemer", Visible = true };
    private AppSettings _settings; private ReminderEngine _engine; private MainWindow? _main; private MainViewModel? _viewModel;
    private ReminderWindow? _reminder; private ExerciseWindow? _exercise; private bool _dueHandled;
    public static bool IsExiting { get; private set; }
    public AppController(AppSettings settings, ISettingsStore store, IStartupRegistrationService startup)
    { _settings = settings; _store = store; _startup = startup; _settings.StartWithWindows = startup.IsRegistered(); _engine = new(settings, DateTimeOffset.Now); BuildTrayMenu(); _timer.Tick += OnTick; _tray.DoubleClick += (_, _) => ShowMain(); }
    public void Initialize(bool commandLineMinimized) { _timer.Start(); if (!commandLineMinimized) ShowMain(); OnTick(null, EventArgs.Empty); }

    private void BuildTrayMenu()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("상태 확인 중…").Name = "status"; menu.Items[0].Enabled = false;
        menu.Items.Add("지금 눈 운동하기", null, (_, _) => StartExercise());
        menu.Items.Add("30분 동안 일시정지", null, (_, _) => { _engine.PauseUntil(DateTimeOffset.Now.AddMinutes(30)); _dueHandled = false; });
        menu.Items.Add("오늘 하루 일시정지", null, (_, _) => { var now = DateTimeOffset.Now; _engine.PauseUntil(new DateTimeOffset(now.Date.AddDays(1), now.Offset)); _dueHandled = false; });
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("설정 열기", null, (_, _) => ShowMain());
        menu.Items.Add("눈 운동 안내 열기", null, (_, _) => ShowMain());
        menu.Items.Add(new Forms.ToolStripSeparator()); menu.Items.Add("종료", null, (_, _) => Exit());
        _tray.ContextMenuStrip = menu;
        ThemeService.ApplyTo(menu, _settings.UseDarkMode);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTimeOffset.Now;
        if (GetIdleTime() > TimeSpan.FromMinutes(10) && _engine.State != ReminderState.SessionInactive) _engine.SetSessionActive(false, now);
        else if (GetIdleTime() <= TimeSpan.FromMinutes(1) && _engine.State == ReminderState.SessionInactive) _engine.SetSessionActive(true, now);
        var snapshot = _engine.Tick(now); UpdateStatus(snapshot);
        if (snapshot.State == ReminderState.ReminderDue && !_dueHandled) { _dueHandled = true; ShowReminder(); }
        if (snapshot.State != ReminderState.ReminderDue) _dueHandled = false;
    }

    private void UpdateStatus(ReminderSnapshot s)
    {
        var text = s.State switch
        {
            ReminderState.Running => $"실행 중 · 다음 알림 {Format(s.Remaining)}",
            ReminderState.Paused => s.ResumeAt is null ? "알림 꺼짐" : $"일시정지 · {s.ResumeAt:HH:mm} 재개",
            ReminderState.InactiveSchedule => s.NextActiveStart is null ? "비활성 시간" : $"비활성 · {s.NextActiveStart:ddd HH:mm} 시작",
            ReminderState.SessionInactive => "세션 비활성 · 복귀 후 새 주기 시작", ReminderState.ReminderDue => "눈 운동 시간입니다",
            ReminderState.Exercising => "눈 운동 중", ReminderState.Snoozed => $"다시 알림 · {Format(s.Remaining)}", _ => s.State.ToString()
        };
        if (_tray.ContextMenuStrip?.Items[0] is Forms.ToolStripItem item) item.Text = text;
        _tray.Text = text.Length > 63 ? text[..63] : text; if (_viewModel is not null) _viewModel.StatusText = text;
    }
    private static string Format(TimeSpan? value) => value is null ? "-" : value.Value.TotalHours >= 1 ? $"{(int)value.Value.TotalHours}시간 {value.Value.Minutes}분" : $"{Math.Max(0, value.Value.Minutes)}분 {Math.Max(0, value.Value.Seconds)}초";

    private void ShowReminder(bool isPreview = false)
    {
        if (_settings.PlaySound) PlaySelectedSound();
        if (!isPreview && !_settings.ShowPopup) { _tray.ShowBalloonTip(5000, "Teyemer", "눈 운동 시간입니다. 트레이 메뉴에서 운동을 시작하세요.", Forms.ToolTipIcon.Info); return; }
        if (_reminder is not null) { _reminder.Activate(); return; }
        var dismissSeconds = Math.Clamp(_settings.PopupDismissSeconds, AppSettings.MinPopupDismissSeconds, AppSettings.MaxPopupDismissSeconds);
        _reminder = new ReminderWindow(isPreview, dismissSeconds);
        _reminder.StartRequested += (_, _) => { _reminder.Close(); StartExercise(); };
        _reminder.SnoozeRequested += (_, _) => { if (!isPreview) _engine.Snooze(DateTimeOffset.Now, TimeSpan.FromMinutes(5)); _reminder.Close(); };
        _reminder.SkipRequested += (_, _) => { if (!isPreview) _engine.Skip(DateTimeOffset.Now); _reminder.Close(); };
        _reminder.Closed += (_, _) => _reminder = null; _reminder.Show();
    }
    public void PreviewReminder() => ShowReminder(true);
    public void StartExercise()
    {
        if (_exercise is not null) { _exercise.Activate(); return; }
        _engine.StartExercise(); _exercise = new ExerciseWindow(_settings.ExerciseDurationSeconds);
        _exercise.Finished += (_, completed) => { if (completed) _engine.CompleteExercise(DateTimeOffset.Now); else _engine.Skip(DateTimeOffset.Now); _exercise.Close(); };
        _exercise.Closed += (_, _) => { if (_engine.State == ReminderState.Exercising) _engine.Skip(DateTimeOffset.Now); _exercise = null; }; _exercise.Show();
    }
    public void PlaySelectedSound() { if (!_settings.PlaySound) return; (_settings.Sound switch { NotificationSound.Exclamation => SystemSounds.Exclamation, NotificationSound.Beep => SystemSounds.Beep, _ => SystemSounds.Asterisk }).Play(); }
    public void PreviewTheme(bool dark) { ThemeService.Apply(dark); ThemeService.ApplyTo(_tray.ContextMenuStrip, dark); }
    public void ShowMain()
    {
        if (_main is null)
        {
            _viewModel = new MainViewModel(this, _settings);
            _main = new MainWindow(_viewModel);
            _main.Closed += OnMainClosed;
        }
        _main.Show();
        _main.WindowState = WindowState.Normal;
        _main.Activate();
    }

    private void OnMainClosed(object? sender, EventArgs e)
    {
        if (sender is MainWindow window) window.Closed -= OnMainClosed;
        if (!ReferenceEquals(sender, _main)) return;
        _main = null;
        _viewModel = null;
    }
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        var oldRegistered = _startup.IsRegistered();
        try { _startup.SetRegistered(settings.StartWithWindows, Environment.ProcessPath ?? throw new InvalidOperationException("실행 경로를 확인할 수 없습니다."), settings.StartMinimized); }
        catch { settings.StartWithWindows = oldRegistered; throw; }
        await _store.SaveAsync(settings); _settings = settings; ThemeService.Apply(settings.UseDarkMode); ThemeService.ApplyTo(_tray.ContextMenuStrip, settings.UseDarkMode); _engine.UpdateSettings(settings, DateTimeOffset.Now); _dueHandled = false;
    }
    public void SetSessionActive(bool active) { _engine.SetSessionActive(active, DateTimeOffset.Now); _dueHandled = false; }
    private void Exit() { IsExiting = true; _timer.Stop(); _main?.Close(); _reminder?.Close(); _exercise?.Close(); System.Windows.Application.Current.Shutdown(); }
    public void Dispose() { _timer.Stop(); _timer.Tick -= OnTick; _tray.Visible = false; _tray.Dispose(); }

    [StructLayout(LayoutKind.Sequential)] private struct LastInputInfo { public uint Size; public uint Time; }
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LastInputInfo info);
    private static TimeSpan GetIdleTime() { var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() }; return GetLastInputInfo(ref info) ? TimeSpan.FromMilliseconds(unchecked((uint)Environment.TickCount - info.Time)) : TimeSpan.Zero; }
}
