using Microsoft.Win32;

namespace SteamGameImporter;

internal static class SteamLocator
{
    public static string? FindSteamPath()
    {
        string? path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null)?.ToString();
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) return path.Replace('/', '\\');
        path = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null)?.ToString();
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) return path;
        string fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        return Directory.Exists(fallback) ? fallback : null;
    }

    public static IReadOnlyList<string> FindUserIds(string steamPath) =>
        Directory.Exists(Path.Combine(steamPath, "userdata"))
            ? Directory.GetDirectories(Path.Combine(steamPath, "userdata"))
                .Where(x => ulong.TryParse(Path.GetFileName(x), out _))
                .OrderByDescending(x => Directory.GetLastWriteTimeUtc(Path.Combine(x, "config")))
                .Select(Path.GetFileName).Cast<string>().ToList()
            : [];
}
