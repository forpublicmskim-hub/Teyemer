using System.Windows;
using System.Windows.Threading;
namespace Teyemer.App;
public partial class ExerciseWindow : Window
{
 private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(200) }; private readonly DateTimeOffset _endsAt; private bool _raised;
 public event EventHandler<bool>? Finished;
 public ExerciseWindow() { InitializeComponent(); ThemeService.Attach(this); _endsAt = DateTimeOffset.Now.AddSeconds(Teyemer.Core.AppSettings.DefaultExerciseDurationSeconds); _timer.Tick += Tick; _timer.Start(); Tick(null, EventArgs.Empty); }
 private void Tick(object? sender, EventArgs e) { var remaining = Math.Max(0, (int)Math.Ceiling((_endsAt - DateTimeOffset.Now).TotalSeconds)); Countdown.Text = remaining.ToString(); if (remaining == 0) Finish(true); }
 private void Complete_Click(object sender, RoutedEventArgs e) => Finish(true); private void Skip_Click(object sender, RoutedEventArgs e) => Finish(false);
 private void Finish(bool completed) { if (_raised) return; _raised = true; _timer.Stop(); Finished?.Invoke(this, completed); }
 protected override void OnClosed(EventArgs e) { _timer.Stop(); _timer.Tick -= Tick; base.OnClosed(e); }
}
