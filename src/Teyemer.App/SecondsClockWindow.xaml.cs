using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Input;
using System.Windows.Threading;

namespace Teyemer.App;

public partial class SecondsClockWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(50) };

    public SecondsClockWindow()
    {
        InitializeComponent();
        _timer.Tick += OnTick;
        Loaded += OnLoaded;
        Closed += OnClosed;
        ThemeService.ThemeChanged += OnThemeChanged;
        ThemeService.Attach(this);
        UpdateClock(DateTime.Now);
        ApplyThemeEffects();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _timer.Start();
        UpdateClock(DateTime.Now);
    }

    private void OnTick(object? sender, EventArgs e) => UpdateClock(DateTime.Now);

    private void UpdateClock(DateTime now)
    {
        HoursText.Text = now.ToString("HH");
        MinutesText.Text = now.ToString("mm");
        SecondsText.Text = SecondsAccentText.Text = now.ToString("ss");

        var secondProgress = CalculateSecondEmphasis(now);
        var eased = secondProgress * secondProgress * (3 - 2 * secondProgress);
        var scale = 1 + eased * 0.16;
        SecondsScale.ScaleX = SecondsScale.ScaleY = scale;
        SecondsAccentScale.ScaleX = SecondsAccentScale.ScaleY = scale;
        SecondsAccentText.Opacity = eased;
        AmbientGlow.Opacity = (ThemeService.IsDark ? 0.05 : 0.025) + eased * (ThemeService.IsDark ? 0.16 : 0.09);
        AnimatedBorder.Opacity = (ThemeService.IsDark ? 0.28 : 0.18) + eased * (ThemeService.IsDark ? 0.55 : 0.36);
        SecondsGlow.Opacity = (ThemeService.IsDark ? 0.18 : 0.10) + eased * (ThemeService.IsDark ? 0.62 : 0.34);
        SecondsGlow.BlurRadius = 10 + eased * 16;
    }

    internal static double CalculateSecondEmphasis(DateTime now)
    {
        var progress = now.Second >= 55
            ? ((now.Second - 55) + now.Millisecond / 1000d) / 5d
            : now.Second == 0 ? Math.Max(0, 1 - now.Millisecond / 650d) : 0;
        return Math.Clamp(progress, 0, 1);
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyThemeEffects();

    private void ApplyThemeEffects()
    {
        if (System.Windows.Application.Current.Resources["AccentBrush"] is SolidColorBrush accent)
            SecondsGlow.Color = accent.Color;
    }

    private void GlowClipHost_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not FrameworkElement host || host.ActualWidth <= 0 || host.ActualHeight <= 0) return;
        host.Clip = new RectangleGeometry(new Rect(0, 0, host.ActualWidth, host.ActualHeight), 17, 17);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Surface_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
        Loaded -= OnLoaded;
        Closed -= OnClosed;
        ThemeService.ThemeChanged -= OnThemeChanged;
    }
}
