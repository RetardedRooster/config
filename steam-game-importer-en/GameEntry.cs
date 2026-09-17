namespace SteamGameImporter;

internal sealed class GameEntry
{
    public bool Selected { get; set; } = false;
    public string Name { get; set; } = "";
    public string Executable { get; set; } = "";
    public string StartDirectory { get; set; } = "";
    public string LaunchOptions { get; set; } = "";
    public string? ArtworkUrl { get; set; }
    public string? HeroUrl { get; set; }
    public string? LogoUrl { get; set; }
    public bool ArtworkManuallyConfigured { get; set; }
    public string ArtworkStatus => $"Cover:{Mark(ArtworkUrl)} Background:{Mark(HeroUrl)} Logo:{Mark(LogoUrl)}";
    private static string Mark(string? value) => string.IsNullOrWhiteSpace(value) ? "–" : "✓";
}

