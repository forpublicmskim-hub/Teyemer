using System.Windows;
using Microsoft.Win32;
using Teyemer.Core;
using Teyemer.Infrastructure;

namespace Teyemer.App;

public partial class App : System.Windows.Application
{
    private AppController? _controller;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try
        {
            var store = new JsonSettingsStore();
            var settings = await store.LoadAsync();
            ThemeService.Apply(settings.UseDarkMode);
            _controller = new AppController(settings, store, new StartupRegistrationService());
            _controller.Initialize(e.Args.Contains("--minimized", StringComparer.OrdinalIgnoreCase));
            SystemEvents.SessionSwitch += OnSessionSwitch;
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Teyemer를 시작하지 못했습니다.\n{ex.Message}", "Teyemer", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e) =>
        _controller?.SetSessionActive(e.Reason is SessionSwitchReason.SessionUnlock or SessionSwitchReason.ConsoleConnect);

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend) _controller?.SetSessionActive(false);
        if (e.Mode == PowerModes.Resume) _controller?.SetSessionActive(true);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _controller?.Dispose();
        base.OnExit(e);
    }
}
