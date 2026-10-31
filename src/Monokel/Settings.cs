using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monokel;

/// <summary>Taste, die zusammen mit dem Mausrad zoomt.</summary>
internal enum ZoomModifier
{
    Control,
    Windows,
}

/// <summary>Benutzereinstellungen, gespeichert unter %APPDATA%\Monokel\settings.json.</summary>
internal sealed class Settings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Monokel", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public ZoomModifier Modifier { get; set; } = ZoomModifier.Control;

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath), JsonOptions) ?? new Settings();
            }
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // Defekte oder unlesbare Datei: mit Standardwerten weitermachen.
        }
        return new Settings();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
