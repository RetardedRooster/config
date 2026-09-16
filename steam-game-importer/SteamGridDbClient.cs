using System.Net.Http.Headers;
using System.Text.Json;

namespace SteamGameImporter;

internal sealed class SteamGridDbClient(string apiKey)
{
    private readonly HttpClient _http = CreateClient(apiKey);
    private readonly HttpClient _imageHttp = new();
    private static HttpClient CreateClient(string key) { var h = new HttpClient(); h.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key); return h; }

    public async Task<string?> FindGridUrlAsync(string name, CancellationToken token)
    {
        using var search = await _http.GetAsync("https://www.steamgriddb.com/api/v2/search/autocomplete/" + Uri.EscapeDataString(name), token);
        if (!search.IsSuccessStatusCode) throw new HttpRequestException($"SteamGridDB keresési hiba: {(int)search.StatusCode} {search.ReasonPhrase}");
        using var sd = JsonDocument.Parse(await search.Content.ReadAsStreamAsync(token));
        var data = sd.RootElement.GetProperty("data"); if (data.GetArrayLength() == 0) return null;
        int id = data[0].GetProperty("id").GetInt32();
        using var grids = await _http.GetAsync($"https://www.steamgriddb.com/api/v2/grids/game/{id}?dimensions=600x900&types=static", token);
        if (!grids.IsSuccessStatusCode) throw new HttpRequestException($"SteamGridDB grid hiba: {(int)grids.StatusCode} {grids.ReasonPhrase}");
        using var gd = JsonDocument.Parse(await grids.Content.ReadAsStreamAsync(token));
        var images = gd.RootElement.GetProperty("data");
        return images.GetArrayLength() > 0 ? images[0].GetProperty("url").GetString() : null;
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
                _ => throw new InvalidDataException($"Nem támogatott képformátum: {media}")
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
        if (output.Length < 1024) throw new InvalidDataException("A letöltött artwork túl kicsi vagy hibás.");
        return destination;
    }
}
