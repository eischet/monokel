using Microsoft.Win32;

namespace Monokel;

/// <summary>Start mit Windows über den Run-Schlüssel des Benutzers; der Schlüssel selbst ist der gespeicherte Zustand.</summary>
internal static class Autostart
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Monokel";

    private static string Command => $"\"{Environment.ProcessPath}\"";

    /// <summary>Nur wahr, wenn genau diese EXE eingetragen ist. Ein Eintrag auf einen alten Pfad zählt nicht.</summary>
    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            return string.Equals(key?.GetValue(ValueName) as string, Command, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
        {
            key.SetValue(ValueName, Command);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
