using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
namespace Teyemer.App;
public partial class ReminderWindow : Window
{
 private const int GwlExStyle = -20;
 private const int WsExNoActivate = 0x08000000;
 public event EventHandler? StartRequested; public event EventHandler? SnoozeRequested; public event EventHandler? SkipRequested;
 private readonly DispatcherTimer _dismissTimer;
 private readonly Storyboard _emphasisStoryboard = new();
 private bool _isDismissing;
 public ReminderWindow(bool isPreview = false, int dismissSeconds = 30) { InitializeComponent(); PreviewLabel.Visibility = isPreview ? Visibility.Visible : Visibility.Collapsed; ConfigureBorderHighlight(); ConfigureEmphasisAnimation(); _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(dismissSeconds) }; _dismissTimer.Tick += OnDismiss; Loaded += OnLoaded; Closed += OnClosed; }
 public ReminderWindow(string content, int dismissSeconds) : this(false, dismissSeconds) { ReminderTitle.Text = "알람 시간입니다"; ReminderContent.Text = content; ExerciseActions.Visibility = Visibility.Collapsed; AlarmCloseButton.Visibility = Visibility.Visible; }

 protected override void OnSourceInitialized(EventArgs e)
 {
  base.OnSourceInitialized(e);
  var handle = new WindowInteropHelper(this).Handle;
  var extendedStyle = GetWindowLong(handle, GwlExStyle);
  SetWindowLong(handle, GwlExStyle, extendedStyle | WsExNoActivate);
 }
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
  if (ThemeService.IsDark)
  {
   _emphasisStoryboard.Children.Add(CreateGlowMovement(AmbientGlow, 0, 330, 4.8, 0.07, 0.14));
   _emphasisStoryboard.Children.Add(CreateGlowMovement(HighlightGlow, 310, -20, 6.2, 0.04, 0.09));
   _emphasisStoryboard.Children.Add(CreateBorderPulse(AnimatedBorder, 0.48, 0.9, 1.8));
   return;
  }

  AmbientGlow.Opacity = 0.045;
  HighlightGlow.Opacity = 0.025;
  _emphasisStoryboard.Children.Add(CreateOpacityPulse(AmbientGlow, 0.025, 0.065, 4.6));
  _emphasisStoryboard.Children.Add(CreateOpacityPulse(HighlightGlow, 0.015, 0.045, 5.8));
  _emphasisStoryboard.Children.Add(CreateBorderPulse(AnimatedBorder, 0.28, 0.58, 3.4));
 }

 private void GlowClipHost_SizeChanged(object sender, SizeChangedEventArgs e)
 {
  if (sender is not FrameworkElement host || host.ActualWidth <= 0 || host.ActualHeight <= 0) return;
  host.Clip = new RectangleGeometry(new Rect(0, 0, host.ActualWidth, host.ActualHeight), 17, 17);
 }

 private void ConfigureBorderHighlight()
 {
  var dark = ThemeService.IsDark;
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
    new GradientStop(System.Windows.Media.Color.FromArgb(dark ? (byte)38 : (byte)22, accent.R, accent.G, accent.B), 0.20),
    new GradientStop(System.Windows.Media.Color.FromArgb(dark ? (byte)230 : (byte)150, accent.R, accent.G, accent.B), 0.46),
    new GradientStop(System.Windows.Media.Color.FromArgb(dark ? (byte)80 : (byte)42, accent.R, accent.G, accent.B), 0.60),
    new GradientStop(System.Windows.Media.Color.FromArgb(0, accent.R, accent.G, accent.B), 1.00)
   }
  };
  AnimatedBorder.Effect = new DropShadowEffect { BlurRadius = dark ? 9 : 7, ShadowDepth = 0, Opacity = dark ? 0.34 : 0.18, Color = accent };

  var orbit = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(dark ? 5.6 : 12)) { RepeatBehavior = RepeatBehavior.Forever };
  Storyboard.SetTarget(orbit, rotation);
  Storyboard.SetTargetProperty(orbit, new PropertyPath(RotateTransform.AngleProperty));
  _emphasisStoryboard.Children.Add(orbit);
 }

 private static Timeline CreateBorderPulse(FrameworkElement target, double minimumOpacity, double maximumOpacity, double seconds)
 {
  var pulse = new DoubleAnimation(minimumOpacity, maximumOpacity, TimeSpan.FromSeconds(seconds)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };
  Storyboard.SetTarget(pulse, target);
  Storyboard.SetTargetProperty(pulse, new PropertyPath(UIElement.OpacityProperty));
  return pulse;
 }

 private static Timeline CreateOpacityPulse(FrameworkElement target, double minimumOpacity, double maximumOpacity, double seconds)
 {
  var pulse = new DoubleAnimation(minimumOpacity, maximumOpacity, TimeSpan.FromSeconds(seconds))
  {
   AutoReverse = true,
   RepeatBehavior = RepeatBehavior.Forever,
   EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
  };
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

 [DllImport("user32.dll", SetLastError = true)]
 private static extern int GetWindowLong(IntPtr window, int index);

 [DllImport("user32.dll", SetLastError = true)]
 private static extern int SetWindowLong(IntPtr window, int index, int newStyle);
}
