using System.Net.Http.Headers;
using System.Text.Json;

namespace SteamGameImporter;

internal sealed class SteamGridDbClient(string apiKey)
{
    private readonly HttpClient _http = CreateClient(apiKey);
    private static HttpClient CreateClient(string key) { var h = new HttpClient(); h.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key); return h; }

    public async Task<string?> FindGridUrlAsync(string name, CancellationToken token)
    {
        using var search = await _http.GetAsync("https://www.steamgriddb.com/api/v2/search/autocomplete/" + Uri.EscapeDataString(name), token);
        if (!search.IsSuccessStatusCode) return null;
        using var sd = JsonDocument.Parse(await search.Content.ReadAsStreamAsync(token));
        var data = sd.RootElement.GetProperty("data"); if (data.GetArrayLength() == 0) return null;
        int id = data[0].GetProperty("id").GetInt32();
        using var grids = await _http.GetAsync($"https://www.steamgriddb.com/api/v2/grids/game/{id}?dimensions=600x900&types=static", token);
        if (!grids.IsSuccessStatusCode) return null;
        using var gd = JsonDocument.Parse(await grids.Content.ReadAsStreamAsync(token));
        var images = gd.RootElement.GetProperty("data");
        return images.GetArrayLength() > 0 ? images[0].GetProperty("url").GetString() : null;
    }

    public async Task DownloadGridAsync(string url, string destination, CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        byte[] bytes = await _http.GetByteArrayAsync(url, token);
        await File.WriteAllBytesAsync(destination, bytes, token);
    }
}
