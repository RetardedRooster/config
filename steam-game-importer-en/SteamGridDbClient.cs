using System.Net.Http.Headers;
using System.Text.Json;

namespace SteamGameImporter;

internal sealed record SteamGridGame(int Id, string Name);
internal sealed record SteamGridArtwork(string Url, string ThumbUrl);
internal enum ArtworkKind { Cover, Hero, Logo }

internal sealed class SteamGridDbClient(string apiKey)
{
    private readonly HttpClient _http = CreateClient(apiKey);
    private readonly HttpClient _imageHttp = new();
    private static HttpClient CreateClient(string key) { var h = new HttpClient(); h.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key); return h; }

    public async Task<List<SteamGridGame>> SearchGamesAsync(string name, CancellationToken token)
    {
        using var response = await _http.GetAsync("https://www.steamgriddb.com/api/v2/search/autocomplete/" + Uri.EscapeDataString(name), token);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"SteamGridDB search error: {(int)response.StatusCode} {response.ReasonPhrase}");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(token));
        return document.RootElement.GetProperty("data").EnumerateArray()
            .Select(x => new SteamGridGame(x.GetProperty("id").GetInt32(), x.GetProperty("name").GetString() ?? "Ismeretlen"))
            .Take(30).ToList();
    }

    public async Task<List<SteamGridArtwork>> GetPortraitsAsync(int gameId, CancellationToken token)
        => await GetArtworkAsync(gameId, ArtworkKind.Cover, token);

    public async Task<List<SteamGridArtwork>> GetArtworkAsync(int gameId, ArtworkKind kind, CancellationToken token)
    {
        string endpoint = kind switch
        {
            ArtworkKind.Cover => $"grids/game/{gameId}?dimensions=600x900&types=static",
            ArtworkKind.Hero => $"heroes/game/{gameId}?types=static",
            ArtworkKind.Logo => $"logos/game/{gameId}?types=static",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        using var response = await _http.GetAsync("https://www.steamgriddb.com/api/v2/" + endpoint, token);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException($"SteamGridDB artwork error: {(int)response.StatusCode} {response.ReasonPhrase}");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(token));
        return document.RootElement.GetProperty("data").EnumerateArray()
            .Select(x => new SteamGridArtwork(x.GetProperty("url").GetString() ?? "", x.TryGetProperty("thumb", out var thumb) ? thumb.GetString() ?? "" : x.GetProperty("url").GetString() ?? ""))
            .Where(x => !string.IsNullOrWhiteSpace(x.Url)).Take(60).ToList();
    }

    public async Task<byte[]> DownloadPreviewAsync(string url, CancellationToken token) => await _imageHttp.GetByteArrayAsync(url, token);

    public async Task<string?> FindGridUrlAsync(string name, CancellationToken token)
    {
        var games = await SearchGamesAsync(name, token); if (games.Count == 0) return null;
        var images = await GetPortraitsAsync(games[0].Id, token);
        return images.FirstOrDefault()?.Url;
    }

    public async Task<string> DownloadGridAsync(string url, string destinationWithoutExtension, CancellationToken token)
    {
        using var response = await _imageHttp.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        string media = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
        string extension = media switch
        {
            "image/png" => ".png",
            "image/jpeg" or "image/jpg" => ".jpg",
            _ => Path.GetExtension(new Uri(url).AbsolutePath).ToLowerInvariant() switch
            {
                ".png" => ".png", ".jpg" or ".jpeg" => ".jpg",
                _ => throw new InvalidDataException($"Unsupported image format: {media}")
            }
        };
        Directory.CreateDirectory(Path.GetDirectoryName(destinationWithoutExtension)!);
        foreach (string oldExtension in new[] { ".png", ".jpg", ".jpeg" })
        {
            string old = destinationWithoutExtension + oldExtension;
            if (File.Exists(old)) File.Delete(old);
        }
        string destination = destinationWithoutExtension + extension;
        await using var input = await response.Content.ReadAsStreamAsync(token);
        await using var output = File.Create(destination);
        await input.CopyToAsync(output, token);
        if (output.Length < 1024) throw new InvalidDataException("The downloaded artwork is too small or invalid.");
        return destination;
    }
}

