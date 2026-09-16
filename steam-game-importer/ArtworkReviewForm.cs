namespace SteamGameImporter;

internal sealed class ArtworkReviewForm : Form
{
    private readonly SteamGridDbClient _client;
    private readonly GameEntry _game;
    private readonly PictureBox _cover = CreatePicture(180, 270);
    private readonly PictureBox _hero = CreatePicture(360, 150);
    private readonly PictureBox _logo = CreatePicture(280, 150);
    private CancellationTokenSource _cts = new();

    public ArtworkReviewForm(SteamGridDbClient client, GameEntry game)
    {
        _client = client; _game = game;
        Text = $"Artwork ellenőrzése – {game.Name}"; Width = 970; Height = 530; MinimumSize = new Size(800, 450); StartPosition = FormStartPosition.CenterParent;
        var cards = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(12) };
        cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27)); cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        cards.Controls.Add(CreateCard("Borító", _cover, ArtworkKind.Cover), 0, 0);
        cards.Controls.Add(CreateCard("Háttér / hero", _hero, ArtworkKind.Hero), 1, 0);
        cards.Controls.Add(CreateCard("Logó", _logo, ArtworkKind.Logo), 2, 0);
        var approve = new Button { Text = "Jóváhagyás és folytatás", AutoSize = true, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Importálás megszakítása", AutoSize = true, DialogResult = DialogResult.Cancel };
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(8) };
        bottom.Controls.AddRange([approve, cancel]);
        Controls.Add(cards); Controls.Add(bottom); AcceptButton = approve; CancelButton = cancel;
        Shown += async (_, _) => await RefreshAll();
    }

    private Control CreateCard(string title, PictureBox picture, ArtworkKind kind)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, Padding = new Padding(6) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font(Font, FontStyle.Bold) }, 0, 0);
        panel.Controls.Add(picture, 0, 1);
        var change = new Button { Text = "Csere…", AutoSize = true };
        change.Click += async (_, _) => await Change(kind);
        var skip = new Button { Text = "Kihagyás", AutoSize = true };
        skip.Click += async (_, _) => { SetUrl(kind, null); _game.ArtworkManuallyConfigured = true; await Refresh(kind); };
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        actions.Controls.AddRange([change, skip]); panel.Controls.Add(actions, 0, 2);
        return panel;
    }

    private async Task Change(ArtworkKind kind)
    {
        using var picker = new ArtworkPickerForm(_client, _game.Name, GetUrl(kind), kind);
        if (picker.ShowDialog(this) == DialogResult.OK)
        {
            SetUrl(kind, picker.SelectedArtworkUrl); _game.ArtworkManuallyConfigured = true; await Refresh(kind);
        }
    }

    private async Task RefreshAll() { await Refresh(ArtworkKind.Cover); await Refresh(ArtworkKind.Hero); await Refresh(ArtworkKind.Logo); }
    private async Task Refresh(ArtworkKind kind)
    {
        PictureBox picture = kind switch { ArtworkKind.Cover => _cover, ArtworkKind.Hero => _hero, _ => _logo };
        picture.Image?.Dispose(); picture.Image = null;
        string? url = GetUrl(kind); if (string.IsNullOrWhiteSpace(url)) { picture.BackColor = Color.FromArgb(55, 55, 55); return; }
        try
        {
            byte[] bytes = await _client.DownloadPreviewAsync(url, _cts.Token);
            using var stream = new MemoryStream(bytes); using var image = Image.FromStream(stream); picture.Image = new Bitmap(image);
        }
        catch { picture.BackColor = Color.DarkRed; }
    }

    private string? GetUrl(ArtworkKind kind) => kind switch { ArtworkKind.Cover => _game.ArtworkUrl, ArtworkKind.Hero => _game.HeroUrl, _ => _game.LogoUrl };
    private void SetUrl(ArtworkKind kind, string? value) { if (kind == ArtworkKind.Cover) _game.ArtworkUrl = value; else if (kind == ArtworkKind.Hero) _game.HeroUrl = value; else _game.LogoUrl = value; }
    private static PictureBox CreatePicture(int width, int height) => new() { Width = width, Height = height, Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.FromArgb(55, 55, 55), BorderStyle = BorderStyle.FixedSingle };
    protected override void Dispose(bool disposing) { if (disposing) { _cts.Cancel(); _cts.Dispose(); _cover.Image?.Dispose(); _hero.Image?.Dispose(); _logo.Image?.Dispose(); } base.Dispose(disposing); }
}
