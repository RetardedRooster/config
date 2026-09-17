using System.Drawing.Imaging;

namespace SteamGameImporter;

internal sealed class ArtworkPickerForm : Form
{
    private readonly SteamGridDbClient _client;
    private readonly TextBox _query = new() { Width = 330 };
    private readonly ComboBox _games = new() { Width = 330, DropDownStyle = ComboBoxStyle.DropDownList, DisplayMember = nameof(SteamGridGame.Name) };
    private readonly FlowLayoutPanel _images = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };
    private readonly Label _status = new() { AutoSize = true, Text = "Searching…", Margin = new Padding(8) };
    private CancellationTokenSource? _cts;
    private readonly ArtworkKind _kind;
    public string? SelectedArtworkUrl { get; private set; }

    public ArtworkPickerForm(SteamGridDbClient client, string initialQuery, string? currentUrl, ArtworkKind kind)
    {
        _client = client; _query.Text = initialQuery; SelectedArtworkUrl = currentUrl; _kind = kind;
        string kindName = kind switch { ArtworkKind.Cover => "cover", ArtworkKind.Hero => "background", ArtworkKind.Logo => "logo", _ => "artwork" };
        Text = $"SteamGridDB {kindName} selection"; Width = 900; Height = 720; MinimumSize = new Size(650, 500); StartPosition = FormStartPosition.CenterParent;
        var search = new Button { Text = "Search", AutoSize = true };
        search.Click += async (_, _) => await SearchGames();
        _query.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; await SearchGames(); } };
        _games.SelectedIndexChanged += async (_, _) => await LoadArtwork();
        var skip = new Button { Text = "Skip artwork", AutoSize = true };
        skip.Click += (_, _) => { SelectedArtworkUrl = null; DialogResult = DialogResult.OK; Close(); };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var searchRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Padding = new Padding(6, 6, 6, 2),
            WrapContents = true
        };
        searchRow.Controls.AddRange([_query, search]);

        var selectionRow = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Fill,
            Padding = new Padding(6, 2, 6, 4),
            WrapContents = true
        };
        selectionRow.Controls.AddRange([_games, skip, cancel]);

        _status.Dock = DockStyle.Fill;
        _status.Margin = new Padding(10, 3, 10, 7);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(searchRow, 0, 0);
        layout.Controls.Add(selectionRow, 0, 1);
        layout.Controls.Add(_status, 0, 2);
        layout.Controls.Add(_images, 0, 3);
        Controls.Add(layout);
        SteamTheme.Apply(this); _images.BackColor = SteamTheme.Background; _status.ForeColor = SteamTheme.Muted;
        Shown += async (_, _) => await SearchGames();
    }

    private async Task SearchGames()
    {
        CancelPrevious(); _status.Text = "Searching for games…"; _games.DataSource = null; _images.Controls.Clear();
        try
        {
            var found = await _client.SearchGamesAsync(_query.Text.Trim(), _cts!.Token);
            _games.DataSource = found;
            _status.Text = found.Count == 0 ? "No results. Try a different title." : "Select a game, then click the artwork you want.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private async Task LoadArtwork()
    {
        if (_games.SelectedItem is not SteamGridGame game) return;
        CancelPrevious(); _status.Text = $"Loading artwork: {game.Name}…"; _images.Controls.Clear();
        try
        {
            var artworks = await _client.GetArtworkAsync(game.Id, _kind, _cts!.Token);
            foreach (var art in artworks) AddArtworkTile(art);
            _status.Text = artworks.Count == 0 ? "No suitable static artwork was found for this game." : $"{artworks.Count} images. Click the one you want to use.";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { _status.Text = ex.Message; }
    }

    private void AddArtworkTile(SteamGridArtwork art)
    {
        Size tile = _kind == ArtworkKind.Cover ? new Size(150, 225) : _kind == ArtworkKind.Hero ? new Size(300, 120) : new Size(240, 140);
        var picture = new PictureBox { Width = tile.Width, Height = tile.Height, Margin = new Padding(8), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, BackColor = SteamTheme.Card, Cursor = Cursors.Hand, Tag = art.Url };
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
