using System.Security;
using Microsoft.Win32;

namespace Monokel;

/// <summary>Taste, die zusammen mit dem Mausrad zoomt.</summary>
internal enum ZoomModifier
{
    Control,
    Windows,
}

/// <summary>Benutzereinstellungen, gespeichert in der Registry unter HKCU\Software\Monokel.</summary>
internal sealed class Settings
{
    private const string KeyPath = @"Software\Monokel";

    public ZoomModifier Modifier { get; set; } = ZoomModifier.Control;

    public static Settings Load()
    {
        var settings = new Settings();
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            if (key?.GetValue(nameof(Modifier)) is string value
                && Enum.TryParse<ZoomModifier>(value, out var modifier)
                && Enum.IsDefined(modifier))
            {
                settings.Modifier = modifier;
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException)
        {
            // Unlesbarer Schlüssel: mit Standardwerten weitermachen.
        }
        return settings;
    }

    public void Save()
    {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
        key.SetValue(nameof(Modifier), Modifier.ToString());
    }
}
