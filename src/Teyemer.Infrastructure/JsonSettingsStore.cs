using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using Teyemer.Core;

namespace Teyemer.Infrastructure;

public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly string _path;

    public JsonSettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Teyemer", "settings.json");
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_path)) return AppSettings.CreateDefault();
            await using var stream = File.OpenRead(_path);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, Options, cancellationToken);
            if (settings is null) return AppSettings.CreateDefault();
            settings.Normalize();
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return AppSettings.CreateDefault();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        settings.Normalize();
        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        var tempPath = _path + ".tmp";
        await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None,
            4096, FileOptions.WriteThrough))
            await JsonSerializer.SerializeAsync(stream, settings, Options, cancellationToken);
        File.Move(tempPath, _path, true);
    }
}
