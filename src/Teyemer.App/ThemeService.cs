using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Teyemer.App;

public static class ThemeService
{
    private static bool _isDark;
    private static bool _hasApplied;
    public static bool IsDark => _isDark;
    public static event EventHandler? ThemeChanged;
    public static void Apply(bool dark)
    {
        if (_hasApplied && _isDark == dark) return;
        _hasApplied = true;
        _isDark = dark;
        var resources = System.Windows.Application.Current.Resources;
        Set(resources, "WindowBackgroundBrush", dark ? "#B3141820" : "#C2FFF8F0");
        Set(resources, "SurfaceBrush", dark ? "#C71D232C" : "#E8FFFEFC");
        Set(resources, "ElevatedSurfaceBrush", dark ? "#DE252C37" : "#F2FFF9F2");
        Set(resources, "TextBrush", dark ? "#F8FAFC" : "#2A1F18");
        Set(resources, "SecondaryTextBrush", dark ? "#C9D2DE" : "#6B4C3A");
        Set(resources, "BorderBrush", dark ? "#CC596577" : "#D8D9B99E");
        Set(resources, "ControlBrush", dark ? "#E12A323E" : "#FAFFFCF8");
        Set(resources, "HoverBrush", dark ? "#E13B4656" : "#F5FFF0D9");
        Set(resources, "SelectionBrush", dark ? "#E3344D6D" : "#F5FFE0B8");
        Set(resources, "AccentBrush", dark ? "#7DB4FF" : "#D94A1A");
        Set(resources, "AccentHoverBrush", dark ? "#9BC5FF" : "#B93812");
        Set(resources, "AccentTextBrush", dark ? "#0B1520" : "#FFFFFF");
        Set(resources, "ErrorBrush", dark ? "#FFB4AE" : "#B42318");
        Set(resources, "IconBackgroundBrush", dark ? "#D12A466C" : "#EBFFE0B8");
        Set(resources, "TableHeaderBrush", dark ? "#F02B3441" : "#FFFFF3E5");
        Set(resources, "TableAlternateBrush", dark ? "#D9232A34" : "#FFFFFBF6");
        Set(resources, "TableGridBrush", dark ? "#B6505C6D" : "#FFE8D2BC");
        Set(resources, "TabBarBrush", "#00000000");
        Set(resources, "TabIdleBrush", dark ? "#FF1C212B" : "#99FFEEDB");
        Set(resources, "TabHoverBrush", dark ? "#FF282F3B" : "#CCFFD59D");
        Set(resources, "TabSelectedBrush", dark ? "#FF315A85" : "#E6E85D16");
        Set(resources, "TabSelectedTextBrush", dark ? "#FFF8FAFC" : "#FFFFFFFF");
        SetGradient(resources, "GlassSurfaceBrush",
            dark ? ["#EA262D38", "#C6181D25", "#DB222A35"] : ["#F2FFFFFF", "#D8FFF4E8", "#E8FFFCF7"]);
        SetGradient(resources, "GlassElevatedSurfaceBrush",
            dark ? ["#F02D3542", "#D51C222B", "#E928313D"] : ["#FAFFFFFF", "#E8FFF0DE", "#F3FFFBF4"]);
        foreach (Window window in System.Windows.Application.Current.Windows) ApplyNativeWindow(window, dark);
        ThemeChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void Attach(Window window)
    {
        window.SourceInitialized += (_, _) => ApplyNativeWindow(window, _isDark);
        window.Opacity = 0;
        RoutedEventHandler? loaded = null;
        loaded = (_, _) => { window.Loaded -= loaded; window.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } }); };
        window.Loaded += loaded;
        ApplyNativeWindow(window, _isDark);
    }

    public static void ApplyTo(Forms.ContextMenuStrip? menu, bool dark)
    {
        if (menu is null) return;
        menu.BackColor = Html(dark ? "#20252E" : "#FFFCF8");
        menu.ForeColor = Html(dark ? "#F1F4F8" : "#2A1F18");
        menu.Renderer = new Forms.ToolStripProfessionalRenderer(new MenuColorTable(dark));
        foreach (Forms.ToolStripItem item in menu.Items)
        {
            item.BackColor = menu.BackColor;
            item.ForeColor = menu.ForeColor;
            item.Padding = item is Forms.ToolStripSeparator
                ? Forms.Padding.Empty
                : new Forms.Padding(8, 4, 8, 4);
        }
    }

    private static void Set(ResourceDictionary resources, string key, string color) => resources[key] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    private static void SetGradient(ResourceDictionary resources, string key, string[] colors)
    {
        var brush = new LinearGradientBrush { StartPoint = new System.Windows.Point(0, 0), EndPoint = new System.Windows.Point(1, 1) };
        brush.GradientStops.Add(new GradientStop(ParseColor(colors[0]), 0));
        brush.GradientStops.Add(new GradientStop(ParseColor(colors[1]), 0.58));
        brush.GradientStops.Add(new GradientStop(ParseColor(colors[2]), 1));
        resources[key] = brush;
    }
    private static System.Windows.Media.Color ParseColor(string value) => (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(value);
    private static Drawing.Color Html(string value) => Drawing.ColorTranslator.FromHtml(value);

    private static void ApplyNativeWindow(Window window, bool dark)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) || window.WindowStyle == WindowStyle.None) return;
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        var enabled = dark ? 1 : 0;
        if (DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int)) != 0) DwmSetWindowAttribute(handle, 19, ref enabled, sizeof(int));
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) { window.SetResourceReference(Window.BackgroundProperty, "WindowBackgroundBrush"); return; }
        var backdrop = window is MainWindow ? 2 : 3;
        if (DwmSetWindowAttribute(handle, 38, ref backdrop, sizeof(int)) != 0) { window.SetResourceReference(Window.BackgroundProperty, "WindowBackgroundBrush"); return; }
        window.Background = System.Windows.Media.Brushes.Transparent;
        if (HwndSource.FromHwnd(handle) is HwndSource source) source.CompositionTarget.BackgroundColor = Colors.Transparent;
        var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        DwmExtendFrameIntoClientArea(handle, ref margins);
        var corner = 2;
        DwmSetWindowAttribute(handle, 33, ref corner, sizeof(int));
        var border = ColorRef(dark ? "#414A58" : "#D9B99E");
        var caption = unchecked((int)0xFFFFFFFF);
        var text = unchecked((int)0xFFFFFFFF);
        DwmSetWindowAttribute(handle, 34, ref border, sizeof(int));
        DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));
        DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
    }

    private static int ColorRef(string html) { var color = Html(html); return color.R | color.G << 8 | color.B << 16; }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);
    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr window, ref Margins margins);
    [StructLayout(LayoutKind.Sequential)]
    private struct Margins { public int Left; public int Right; public int Top; public int Bottom; }

    private sealed class MenuColorTable(bool dark) : Forms.ProfessionalColorTable
    {
        public override Drawing.Color ToolStripDropDownBackground => Html(dark ? "#20252E" : "#FFFCF8");
        public override Drawing.Color MenuItemSelected => Html(dark ? "#343D4A" : "#FFF0D9");
        public override Drawing.Color MenuItemBorder => Html(dark ? "#414A58" : "#D9B99E");
        public override Drawing.Color ImageMarginGradientBegin => ToolStripDropDownBackground;
        public override Drawing.Color ImageMarginGradientMiddle => ToolStripDropDownBackground;
        public override Drawing.Color ImageMarginGradientEnd => ToolStripDropDownBackground;
        public override Drawing.Color SeparatorDark => Html(dark ? "#414A58" : "#E8D2BC");
        public override Drawing.Color SeparatorLight => ToolStripDropDownBackground;
    }
}
