namespace SteamGameImporter;

internal static class GameScanner
{
    private static readonly HashSet<string> Ignored = new(StringComparer.OrdinalIgnoreCase)
    { "unins000.exe", "uninstall.exe", "setup.exe", "installer.exe", "crashreporter.exe", "unitycrashhandler64.exe", "unitycrashhandler32.exe", "vc_redist.x64.exe", "vc_redist.x86.exe", "dxsetup.exe" };

    public static async Task<List<GameEntry>> ScanAsync(IEnumerable<string> roots, IProgress<string> progress, CancellationToken token)
    {
        return await Task.Run(() =>
        {
            var found = new List<GameEntry>();
            foreach (string root in roots.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!Directory.Exists(root)) continue;
                progress.Report(root);
                ScanDirectory(root, 0, found, progress, token);
            }
            return found.GroupBy(x => Path.GetFullPath(x.Executable), StringComparer.OrdinalIgnoreCase).Select(g => g.First()).OrderBy(x => x.Name).ToList();
        }, token);
    }

    private static void ScanDirectory(string dir, int depth, List<GameEntry> found, IProgress<string> progress, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (depth > 5 || IsIgnoredDirectory(dir)) return;
        try
        {
            foreach (string exe in Directory.EnumerateFiles(dir, "*.exe", SearchOption.TopDirectoryOnly))
            {
                token.ThrowIfCancellationRequested();
                var info = new FileInfo(exe);
                if (Ignored.Contains(info.Name) || info.Length < 100_000) continue;
                string gameName = InferName(exe);
                found.Add(new GameEntry { Name = gameName, Executable = exe, StartDirectory = info.DirectoryName ?? dir });
                progress.Report(gameName);
            }
            foreach (string sub in Directory.EnumerateDirectories(dir)) ScanDirectory(sub, depth + 1, found, progress, token);
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    private static bool IsIgnoredDirectory(string path)
    {
        string n = Path.GetFileName(path);
        return n.Equals("Windows", StringComparison.OrdinalIgnoreCase) || n.Equals("ProgramData", StringComparison.OrdinalIgnoreCase)
            || n.Equals("node_modules", StringComparison.OrdinalIgnoreCase) || n.StartsWith("$", StringComparison.OrdinalIgnoreCase);
    }

    private static string InferName(string exe)
    {
        try
        {
            var v = System.Diagnostics.FileVersionInfo.GetVersionInfo(exe);
            if (!string.IsNullOrWhiteSpace(v.ProductName) && !v.ProductName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase)) return v.ProductName.Trim();
        }
        catch { }
        string file = Path.GetFileNameWithoutExtension(exe);
        return file.Equals("game", StringComparison.OrdinalIgnoreCase) || file.Equals("launcher", StringComparison.OrdinalIgnoreCase)
            ? Directory.GetParent(exe)?.Name ?? file : file;
    }
}
