using System.Drawing.Imaging;

namespace SteamGameImporter;

internal sealed class ArtworkPickerForm : Form
{
    private readonly SteamGridDbClient _client;
    private readonly TextBox _query = new() { Width = 330 };
    private readonly ComboBox _games = new() { Width = 330, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(SteamGridGame.Name) };
    private readonly FlowLayoutPanel _images = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };
    private readonly Label _status = new() { AutoSize = true, Text = "Keresés…", Margin = new Padding(8) };
    private CancellationTokenSource? _cts;
    private readonly ArtworkKind _kind;
    public string? SelectedArtworkUrl { get; private set; }

    public ArtworkPickerForm(SteamGridDbClient client, string initialQuery, string? currentUrl, ArtworkKind kind)
    {
        _client = client; _query.Text = initialQuery; SelectedArtworkUrl = currentUrl; _kind = kind;
        string kindName = kind switch { ArtworkKind.Cover => "borító", ArtworkKind.Hero => "háttér", ArtworkKind.Logo => "logó", _ => "artwork" };
        Text = $"SteamGridDB {kindName} kiválasztása"; Width = 900; Height = 720; MinimumSize = new Size(650, 500); StartPosition = FormStartPosition.CenterParent;
        var search = new Button { Text = "Keresés", AutoSize = true };
        search.Click += async (_, _) => await SearchGames();
        _query.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchGames(); } };
        _games.SelectedIndexChanged += async (_, _) => await LoadArtwork();
        var skip = new Button { Text = "Artwork kihagyása", AutoSize = true };
        skip.Click += (_, _) => { SelectedArtworkUrl = null; DialogResult = DialogResult.OK; Close(); };
        var cancel = new Button { Text = "Mégse", AutoSize = true, DialogResult = DialogResult.Cancel };
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, Padding = new Padding(6), WrapContents = false };
        top.Controls.AddRange([_query, search, _games, skip, cancel]);
        Controls.Add(_images); Controls.Add(_status); Controls.Add(top);
        Shown += async (_, _) => await SearchGames();
    }

    private async Task SearchGames()
    {
        CancelPrevious(); _status.Text = "Játékok keresése…"; _games.DataSource = null; _images.Controls.Clear();
        try
        {
            var found = await _client.SearchGamesAsync(_query.Text.Trim(), _cts!.Token);
            _games.DataSource = found;
            _status.Text = found.Count == 0 ? "Nincs találat. Próbálj másik címet." : "Válassz játékot, majd kattints a megfelelő borítóra.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private async Task LoadArtwork()
    {
        if (_games.SelectedItem is not SteamGridGame game) return;
        CancelPrevious(); _status.Text = $"Borítók betöltése: {game.Name}…"; _images.Controls.Clear();
        try
        {
            var artworks = await _client.GetArtworkAsync(game.Id, _kind, _cts!.Token);
            foreach (var art in artworks) AddArtworkTile(art);
            _status.Text = artworks.Count == 0 ? "Ehhez a játékhoz nincs megfelelő statikus artwork." : $"{artworks.Count} kép. Kattints a használni kívántra.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private void AddArtworkTile(SteamGridArtwork art)
    {
        Size tile = _kind == ArtworkKind.Cover ? new Size(150, 225) : _kind == ArtworkKind.Hero ? new Size(300, 120) : new Size(240, 140);
        var picture = new PictureBox { Width = tile.Width, Height = tile.Height, SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.DimGray, Cursor = Cursors.Hand, Tag = art.Url };
        picture.Click += (_, _) => { SelectedArtworkUrl = art.Url; DialogResult = DialogResult.OK; Close(); };
        _images.Controls.Add(picture);
        _ = LoadPreview(picture, string.IsNullOrWhiteSpace(art.ThumbUrl) ? art.Url : art.ThumbUrl, _cts!.Token);
    }

    private async Task LoadPreview(PictureBox picture, string url, CancellationToken token)
    {
        try
        {
            byte[] bytes = await _client.DownloadPreviewAsync(url, token);
            using var stream = new MemoryStream(bytes); using var image = Image.FromStream(stream);
            picture.Image = new Bitmap(image);
        }
        catch { }
    }

    private void CancelPrevious() { _cts?.Cancel(); _cts?.Dispose(); _cts = new CancellationTokenSource(); }
    protected override void Dispose(bool disposing) { if (disposing) { _cts?.Cancel(); _cts?.Dispose(); } base.Dispose(disposing); }
}
