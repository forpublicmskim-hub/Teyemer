namespace Teyemer.Core;

public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}

public interface IStartupRegistrationService
{
    bool IsRegistered();
    void SetRegistered(bool enabled, string executablePath, bool startMinimized);
}
