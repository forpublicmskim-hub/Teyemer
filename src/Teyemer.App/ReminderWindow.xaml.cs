using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
namespace Teyemer.App;
public partial class ReminderWindow : Window
{
 public event EventHandler? StartRequested; public event EventHandler? SnoozeRequested; public event EventHandler? SkipRequested;
 private readonly DispatcherTimer _dismissTimer;
 private bool _isDismissing;
 public ReminderWindow(bool isPreview = false, int dismissSeconds = 30) { InitializeComponent(); PreviewLabel.Visibility = isPreview ? Visibility.Visible : Visibility.Collapsed; _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(dismissSeconds) }; _dismissTimer.Tick += OnDismiss; Loaded += OnLoaded; Closed += OnClosed; }
 private void OnLoaded(object sender, RoutedEventArgs e) { var area = SystemParameters.WorkArea; Left = area.Right - ActualWidth - 12; Top = area.Bottom - ActualHeight - 12; BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180))); _dismissTimer.Start(); }
 private void OnDismiss(object? sender, EventArgs e) => BeginDismiss(() => SkipRequested?.Invoke(this, EventArgs.Empty));
 private void OnClosed(object? sender, EventArgs e) { _dismissTimer.Stop(); _dismissTimer.Tick -= OnDismiss; Loaded -= OnLoaded; Closed -= OnClosed; }
 private void Start_Click(object sender, RoutedEventArgs e) => BeginDismiss(() => StartRequested?.Invoke(this, EventArgs.Empty));
 private void Snooze_Click(object sender, RoutedEventArgs e) => BeginDismiss(() => SnoozeRequested?.Invoke(this, EventArgs.Empty));
 private void Skip_Click(object sender, RoutedEventArgs e) => BeginDismiss(() => SkipRequested?.Invoke(this, EventArgs.Empty));
 private void Close_Click(object sender, RoutedEventArgs e) => BeginDismiss(() => SkipRequested?.Invoke(this, EventArgs.Empty));
 private void BeginDismiss(Action completed)
 {
  if (_isDismissing) return;
  _isDismissing = true; _dismissTimer.Stop(); IsHitTestVisible = false;
  var fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(280)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
  fade.Completed += (_, _) => completed(); BeginAnimation(OpacityProperty, fade);
 }
}
