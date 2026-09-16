namespace SteamGameImporter;

internal sealed class GameEntry
{
    public bool Selected { get; set; } = false;
    public string Name { get; set; } = "";
    public string Executable { get; set; } = "";
    public string StartDirectory { get; set; } = "";
    public string LaunchOptions { get; set; } = "";
    public string? ArtworkUrl { get; set; }
    public string ArtworkStatus => string.IsNullOrWhiteSpace(ArtworkUrl) ? "Nincs kiválasztva" : "Kiválasztva";
}
