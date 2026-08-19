using Microsoft.Win32;
using Teyemer.Core;

namespace Teyemer.Infrastructure;

public sealed class StartupRegistrationService : IStartupRegistrationService
{
    public const string ValueName = "Teyemer";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
        return key?.GetValue(ValueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    public void SetRegistered(bool enabled, string executablePath, bool startMinimized)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, true)
            ?? throw new InvalidOperationException("현재 사용자의 자동 실행 레지스트리를 열 수 없습니다.");
        if (!enabled) { key.DeleteValue(ValueName, false); return; }
        key.SetValue(ValueName, BuildCommand(executablePath, startMinimized), RegistryValueKind.String);
    }

    public static string BuildCommand(string executablePath, bool startMinimized) =>
        $"\"{executablePath.Replace("\"", "\\\"")}\"{(startMinimized ? " --minimized" : string.Empty)}";
}
