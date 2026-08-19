using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
namespace Teyemer.App;
public partial class ReminderWindow : Window
{
 public event EventHandler? StartRequested; public event EventHandler? SnoozeRequested; public event EventHandler? SkipRequested;
 private readonly DispatcherTimer _dismissTimer;
 private readonly Storyboard _emphasisStoryboard = new();
 private bool _isDismissing;
 public ReminderWindow(bool isPreview = false, int dismissSeconds = 30) { InitializeComponent(); PreviewLabel.Visibility = isPreview ? Visibility.Visible : Visibility.Collapsed; ConfigureBorderHighlight(); ConfigureEmphasisAnimation(); _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(dismissSeconds) }; _dismissTimer.Tick += OnDismiss; Loaded += OnLoaded; Closed += OnClosed; }
 private void OnLoaded(object sender, RoutedEventArgs e)
 {
  var area = SystemParameters.WorkArea;
  var targetLeft = area.Right - ActualWidth - 12;
  Left = targetLeft;
  Top = area.Bottom - ActualHeight - 12;
  Opacity = 1;

  if (SystemParameters.ClientAreaAnimation)
  {
   var easing = new QuarticEase { EasingMode = EasingMode.EaseOut };
   BeginAnimation(LeftProperty, new DoubleAnimation(area.Right + 8, targetLeft, TimeSpan.FromMilliseconds(420)) { EasingFunction = easing });
   BeginAnimation(OpacityProperty, new DoubleAnimation(0.72, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = easing });
   _emphasisStoryboard.Begin(this, true);
  }

  _dismissTimer.Start();
 }
 private void OnDismiss(object? sender, EventArgs e) => BeginDismiss(() => SkipRequested?.Invoke(this, EventArgs.Empty));
 private void OnClosed(object? sender, EventArgs e) { _dismissTimer.Stop(); _emphasisStoryboard.Remove(this); _dismissTimer.Tick -= OnDismiss; Loaded -= OnLoaded; Closed -= OnClosed; }
 private void Start_Click(object sender, RoutedEventArgs e) => BeginDismiss(() => StartRequested?.Invoke(this, EventArgs.Empty));
 private void Snooze_Click(object sender, RoutedEventArgs e) => BeginDismiss(() => SnoozeRequested?.Invoke(this, EventArgs.Empty));
 private void Skip_Click(object sender, RoutedEventArgs e) => BeginDismiss(() => SkipRequested?.Invoke(this, EventArgs.Empty));
 private void Close_Click(object sender, RoutedEventArgs e) => BeginDismiss(() => SkipRequested?.Invoke(this, EventArgs.Empty));
 private void BeginDismiss(Action completed)
 {
  if (_isDismissing) return;
  _isDismissing = true; _dismissTimer.Stop(); IsHitTestVisible = false;
  var fade = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(280)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
  fade.Completed += (_, _) => { _emphasisStoryboard.Remove(this); completed(); };
  BeginAnimation(OpacityProperty, fade);
 }

 private void ConfigureEmphasisAnimation()
 {
 _emphasisStoryboard.RepeatBehavior = RepeatBehavior.Forever;
  _emphasisStoryboard.Children.Add(CreateGlowMovement(AmbientGlow, 0, 330, 4.8, 0.07, 0.14));
  _emphasisStoryboard.Children.Add(CreateGlowMovement(HighlightGlow, 310, -20, 6.2, 0.04, 0.09));
  _emphasisStoryboard.Children.Add(CreateBorderPulse(AnimatedBorder));
 }

 private void ConfigureBorderHighlight()
 {
  var accent = (System.Windows.Application.Current.Resources["AccentBrush"] as SolidColorBrush)?.Color ?? System.Windows.Media.Color.FromRgb(37, 99, 169);
  var rotation = new RotateTransform(0, 0.5, 0.5);
  AnimatedBorder.BorderBrush = new LinearGradientBrush
  {
   StartPoint = new System.Windows.Point(0, 0),
   EndPoint = new System.Windows.Point(1, 1),
   RelativeTransform = rotation,
   GradientStops =
   {
    new GradientStop(System.Windows.Media.Color.FromArgb(0, accent.R, accent.G, accent.B), 0.00),
    new GradientStop(System.Windows.Media.Color.FromArgb(38, accent.R, accent.G, accent.B), 0.20),
    new GradientStop(System.Windows.Media.Color.FromArgb(230, accent.R, accent.G, accent.B), 0.46),
    new GradientStop(System.Windows.Media.Color.FromArgb(80, accent.R, accent.G, accent.B), 0.60),
    new GradientStop(System.Windows.Media.Color.FromArgb(0, accent.R, accent.G, accent.B), 1.00)
   }
  };
  AnimatedBorder.Effect = new DropShadowEffect { BlurRadius = 9, ShadowDepth = 0, Opacity = 0.34, Color = accent };

  var orbit = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(5.6)) { RepeatBehavior = RepeatBehavior.Forever };
  Storyboard.SetTarget(orbit, rotation);
  Storyboard.SetTargetProperty(orbit, new PropertyPath(RotateTransform.AngleProperty));
  _emphasisStoryboard.Children.Add(orbit);
 }

 private static Timeline CreateBorderPulse(FrameworkElement target)
 {
  var pulse = new DoubleAnimation(0.48, 0.9, TimeSpan.FromSeconds(1.8)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };
  Storyboard.SetTarget(pulse, target);
  Storyboard.SetTargetProperty(pulse, new PropertyPath(UIElement.OpacityProperty));
  return pulse;
 }

 private static Timeline CreateGlowMovement(FrameworkElement target, double from, double to, double seconds, double minimumOpacity, double maximumOpacity)
 {
  var movement = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(seconds), AutoReverse = true };
  movement.KeyFrames.Add(new EasingDoubleKeyFrame(from, KeyTime.FromPercent(0), new SineEase { EasingMode = EasingMode.EaseInOut }));
  movement.KeyFrames.Add(new EasingDoubleKeyFrame(to, KeyTime.FromPercent(1), new SineEase { EasingMode = EasingMode.EaseInOut }));
  Storyboard.SetTarget(movement, target);
  Storyboard.SetTargetProperty(movement, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));

  var pulse = new DoubleAnimation(minimumOpacity, maximumOpacity, TimeSpan.FromSeconds(seconds / 2)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };
  Storyboard.SetTarget(pulse, target);
  Storyboard.SetTargetProperty(pulse, new PropertyPath(UIElement.OpacityProperty));

  var group = new ParallelTimeline();
  group.Children.Add(movement);
  group.Children.Add(pulse);
  return group;
 }
}
