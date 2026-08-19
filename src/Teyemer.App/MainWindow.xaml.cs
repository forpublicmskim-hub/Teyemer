using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
namespace Teyemer.App;
public partial class MainWindow : Window
{
 private readonly Storyboard _borderStoryboard = new();
 private bool _borderAnimationRunning;

 public MainWindow(MainViewModel viewModel)
 {
  InitializeComponent();
  DataContext = viewModel;
  ConfigureBorderHighlight();
  Loaded += OnLoaded;
  IsVisibleChanged += OnIsVisibleChanged;
  Closed += OnClosed;
  ThemeService.ThemeChanged += OnThemeChanged;
  ThemeService.Attach(this);
 }

 private void OnLoaded(object sender, RoutedEventArgs e) => StartBorderAnimation();
 private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) { if (IsVisible) StartBorderAnimation(); else StopBorderAnimation(); }
 private void OnThemeChanged(object? sender, EventArgs e) { StopBorderAnimation(); ConfigureBorderHighlight(); StartBorderAnimation(); }
 private void OnClosed(object? sender, EventArgs e)
 {
  StopBorderAnimation();
  ThemeService.ThemeChanged -= OnThemeChanged;
  Loaded -= OnLoaded;
  IsVisibleChanged -= OnIsVisibleChanged;
  Closed -= OnClosed;
  DataContext = null;
  AnimatedWindowBorder.BorderBrush = null;
  AnimatedWindowBorder.Effect = null;
  _borderStoryboard.Children.Clear();
 }

 private void ConfigureBorderHighlight()
 {
  _borderStoryboard.Children.Clear();
  var accent = (System.Windows.Application.Current.Resources["AccentBrush"] as SolidColorBrush)?.Color ?? System.Windows.Media.Color.FromRgb(37, 99, 169);
  var rotation = new RotateTransform(0, 0.5, 0.5);
  AnimatedWindowBorder.BorderBrush = new LinearGradientBrush
  {
   StartPoint = new System.Windows.Point(0, 0),
   EndPoint = new System.Windows.Point(1, 1),
   RelativeTransform = rotation,
   GradientStops =
   {
    new GradientStop(System.Windows.Media.Color.FromArgb(0, accent.R, accent.G, accent.B), 0.00),
    new GradientStop(System.Windows.Media.Color.FromArgb(28, accent.R, accent.G, accent.B), 0.22),
    new GradientStop(System.Windows.Media.Color.FromArgb(190, accent.R, accent.G, accent.B), 0.46),
    new GradientStop(System.Windows.Media.Color.FromArgb(58, accent.R, accent.G, accent.B), 0.60),
    new GradientStop(System.Windows.Media.Color.FromArgb(0, accent.R, accent.G, accent.B), 1.00)
   }
  };
  AnimatedWindowBorder.Effect = new DropShadowEffect { BlurRadius = 7, ShadowDepth = 0, Opacity = 0.24, Color = accent };

  var orbit = new DoubleAnimation(0, 360, TimeSpan.FromSeconds(10)) { RepeatBehavior = RepeatBehavior.Forever };
  Storyboard.SetTarget(orbit, rotation);
  Storyboard.SetTargetProperty(orbit, new PropertyPath(RotateTransform.AngleProperty));
  var pulse = new DoubleAnimation(0.28, 0.56, TimeSpan.FromSeconds(3.2)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut } };
  Storyboard.SetTarget(pulse, AnimatedWindowBorder);
  Storyboard.SetTargetProperty(pulse, new PropertyPath(OpacityProperty));
  _borderStoryboard.Children.Add(orbit);
  _borderStoryboard.Children.Add(pulse);
 }

 private void StartBorderAnimation()
 {
  if (_borderAnimationRunning || !IsVisible || !SystemParameters.ClientAreaAnimation) return;
  _borderStoryboard.Begin(this, true);
  _borderAnimationRunning = true;
 }

 private void StopBorderAnimation()
 {
  if (!_borderAnimationRunning) return;
  _borderStoryboard.Remove(this);
  _borderAnimationRunning = false;
 }
}
