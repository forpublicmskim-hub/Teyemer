using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace Teyemer.App;

public static class ThemeService
{
    private static bool _isDark;
    public static void Apply(bool dark)
    {
        _isDark = dark;
        var resources = System.Windows.Application.Current.Resources;
        Set(resources, "WindowBackgroundBrush", dark ? "#171B22" : "#F4F6F8");
        Set(resources, "SurfaceBrush", dark ? "#20252E" : "#FFFFFF");
        Set(resources, "ElevatedSurfaceBrush", dark ? "#272D38" : "#F8FAFC");
        Set(resources, "TextBrush", dark ? "#F1F4F8" : "#182230");
        Set(resources, "SecondaryTextBrush", dark ? "#B7C0CC" : "#52606D");
        Set(resources, "BorderBrush", dark ? "#414A58" : "#CBD2D9");
        Set(resources, "ControlBrush", dark ? "#2B323E" : "#FFFFFF");
        Set(resources, "HoverBrush", dark ? "#343D4A" : "#E9EEF5");
        Set(resources, "SelectionBrush", dark ? "#2D4B70" : "#D8E8FF");
        Set(resources, "AccentBrush", dark ? "#6EA8FE" : "#2563A9");
        Set(resources, "AccentHoverBrush", dark ? "#8AB9FF" : "#1E4F88");
        Set(resources, "AccentTextBrush", dark ? "#101820" : "#FFFFFF");
        Set(resources, "ErrorBrush", dark ? "#FF9B95" : "#B42318");
        Set(resources, "IconBackgroundBrush", dark ? "#263F63" : "#DCEAFF");
        foreach (Window window in System.Windows.Application.Current.Windows) ApplyNativeWindow(window, dark);
    }

    public static void Attach(Window window)
    {
        window.SourceInitialized += (_, _) => ApplyNativeWindow(window, _isDark);
        ApplyNativeWindow(window, _isDark);
    }

    public static void ApplyTo(Forms.ContextMenuStrip? menu, bool dark)
    {
        if (menu is null) return;
        menu.BackColor = Html(dark ? "#20252E" : "#FFFFFF");
        menu.ForeColor = Html(dark ? "#F1F4F8" : "#182230");
        menu.Renderer = new Forms.ToolStripProfessionalRenderer(new MenuColorTable(dark));
        foreach (Forms.ToolStripItem item in menu.Items) { item.BackColor = menu.BackColor; item.ForeColor = menu.ForeColor; }
    }

    private static void Set(ResourceDictionary resources, string key, string color) => resources[key] = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    private static Drawing.Color Html(string value) => Drawing.ColorTranslator.FromHtml(value);

    private static void ApplyNativeWindow(Window window, bool dark)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763) || window.WindowStyle == WindowStyle.None) return;
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        var enabled = dark ? 1 : 0;
        if (DwmSetWindowAttribute(handle, 20, ref enabled, sizeof(int)) != 0) DwmSetWindowAttribute(handle, 19, ref enabled, sizeof(int));
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
        var border = ColorRef(dark ? "#414A58" : "#CBD2D9");
        var caption = ColorRef(dark ? "#20252E" : "#F4F6F8");
        var text = ColorRef(dark ? "#F1F4F8" : "#182230");
        DwmSetWindowAttribute(handle, 34, ref border, sizeof(int));
        DwmSetWindowAttribute(handle, 35, ref caption, sizeof(int));
        DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
    }

    private static int ColorRef(string html) { var color = Html(html); return color.R | color.G << 8 | color.B << 16; }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int valueSize);

    private sealed class MenuColorTable(bool dark) : Forms.ProfessionalColorTable
    {
        public override Drawing.Color ToolStripDropDownBackground => Html(dark ? "#20252E" : "#FFFFFF");
        public override Drawing.Color MenuItemSelected => Html(dark ? "#343D4A" : "#E9EEF5");
        public override Drawing.Color MenuItemBorder => Html(dark ? "#414A58" : "#CBD2D9");
        public override Drawing.Color ImageMarginGradientBegin => ToolStripDropDownBackground;
        public override Drawing.Color ImageMarginGradientMiddle => ToolStripDropDownBackground;
        public override Drawing.Color ImageMarginGradientEnd => ToolStripDropDownBackground;
        public override Drawing.Color SeparatorDark => Html(dark ? "#414A58" : "#CBD2D9");
        public override Drawing.Color SeparatorLight => ToolStripDropDownBackground;
    }
}
