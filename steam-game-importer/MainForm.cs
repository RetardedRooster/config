using System.Diagnostics;

namespace SteamGameImporter;

internal sealed class MainForm : Form
{
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly TextBox _scanPath = new() { PlaceholderText = "Keresési mappa vagy meghajtó…", Width = 430 };
    private readonly ComboBox _steamUser = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly TextBox _apiKey = new() { UseSystemPasswordChar = true, Width = 210, PlaceholderText = "SteamGridDB API-kulcs (opcionális)" };
    private readonly ToolStripStatusLabel _status = new("Kész");
    private CancellationTokenSource? _cts;
    private string? _steamPath;

    public MainForm()
    {
        Text = "Steam Game Importer"; Width = 1100; Height = 680; MinimumSize = new Size(850, 500); StartPosition = FormStartPosition.CenterScreen;
        BuildGrid(); BuildLayout();
        Load += (_, _) => { _apiKey.Text = CredentialStore.LoadApiKey(); InitializeSteam(); };
        FormClosing += (_, _) => CredentialStore.SaveApiKey(_apiKey.Text);
        _apiKey.Leave += (_, _) => CredentialStore.SaveApiKey(_apiKey.Text);
    }

    private void BuildGrid()
    {
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(GameEntry.Selected), HeaderText = "✓", Width = 38 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(GameEntry.Name), HeaderText = "Játék neve", Width = 230 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(GameEntry.Executable), HeaderText = "Indítófájl", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(GameEntry.StartDirectory), HeaderText = "Kezdőmappa", Width = 260 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(GameEntry.LaunchOptions), HeaderText = "Indítási opciók", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(GameEntry.ArtworkStatus), HeaderText = "Artwork", Width = 210, ReadOnly = true });
    }

    private void BuildLayout()
    {
        var choose = Button("Tallózás…", (_, _) => ChooseScanFolder()); var scan = Button("Keresés", async (_, _) => await Scan());
        var addManual = Button("Kézi hozzáadás", (_, _) => AddManual()); var remove = Button("Kijelölt sor törlése", (_, _) => RemoveRows());
        var artwork = Button("Artworkok kiválasztása", async (_, _) => await ChooseArtworkForCurrentRow());
        var import = Button("Hozzáadás a Steamhez", async (_, _) => await Import());
        var stop = Button("Leállítás", (_, _) => _cts?.Cancel());
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 78, Padding = new Padding(7), WrapContents = true };
        top.Controls.AddRange([_scanPath, choose, scan, stop, addManual, remove, artwork]);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(7), WrapContents = false };
        bottom.Controls.AddRange([new Label { Text = "Steam felhasználó:", AutoSize = true, Margin = new Padding(3, 9, 3, 3) }, _steamUser, _apiKey, import]);
        var status = new StatusStrip(); status.Items.Add(_status);
        Controls.Add(_grid); Controls.Add(top); Controls.Add(bottom); Controls.Add(status);
    }

    private static Button Button(string text, EventHandler action) { var b = new Button { Text = text, AutoSize = true }; b.Click += action; return b; }

    private void InitializeSteam()
    {
        _steamPath = SteamLocator.FindSteamPath();
        if (_steamPath is null) { _status.Text = "Steam nem található. Telepítsd vagy indítsd el egyszer a Steamet."; return; }
        _steamUser.Items.AddRange(SteamLocator.FindUserIds(_steamPath).Cast<object>().ToArray()); if (_steamUser.Items.Count > 0) _steamUser.SelectedIndex = 0;
        string games = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Games");
        _scanPath.Text = Directory.Exists(games) ? games : Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";
    }

    private void ChooseScanFolder() { using var d = new FolderBrowserDialog { Description = "Válaszd ki a játékokat tartalmazó mappát vagy meghajtót" }; if (d.ShowDialog(this) == DialogResult.OK) _scanPath.Text = d.SelectedPath; }

    private async Task Scan()
    {
        if (!Directory.Exists(_scanPath.Text)) { MessageBox.Show(this, "A megadott mappa nem létezik.", "Hiba"); return; }
        _cts = new CancellationTokenSource(); _status.Text = "Keresés…";
        try
        {
            var progress = new Progress<string>(x => _status.Text = "Keresés: " + x);
            var games = await GameScanner.ScanAsync([_scanPath.Text], progress, _cts.Token);
            _grid.DataSource = games; _status.Text = $"{games.Count} lehetséges indítófájl található.";
        }
        catch (OperationCanceledException) { _status.Text = "Keresés megszakítva."; }
        finally { _cts.Dispose(); _cts = null; }
    }

    private void AddManual()
    {
        using var f = new OpenFileDialog { Filter = "Windows program (*.exe)|*.exe|Minden fájl (*.*)|*.*", Title = "Válaszd ki a játék indítófájlját" };
        if (f.ShowDialog(this) != DialogResult.OK) return;
        using var d = new FolderBrowserDialog { Description = "Válaszd ki a játék mappáját", SelectedPath = Path.GetDirectoryName(f.FileName) };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        var list = (_grid.DataSource as List<GameEntry>)?.ToList() ?? [];
        list.Add(new GameEntry { Name = Path.GetFileNameWithoutExtension(f.FileName), Executable = f.FileName, StartDirectory = d.SelectedPath });
        _grid.DataSource = null; _grid.DataSource = list;
    }

    private void RemoveRows()
    {
        var list = (_grid.DataSource as List<GameEntry>)?.ToList() ?? [];
        foreach (DataGridViewRow row in _grid.SelectedRows.Cast<DataGridViewRow>().OrderByDescending(r => r.Index)) if (row.DataBoundItem is GameEntry g) list.Remove(g);
        _grid.DataSource = null; _grid.DataSource = list;
    }

    private async Task Import()
    {
        _grid.EndEdit();
        if (_steamPath is null || _steamUser.SelectedItem is not string user) { MessageBox.Show(this, "Nem található Steam-felhasználó.", "Hiba"); return; }
        var selected = (_grid.DataSource as List<GameEntry>)?.Where(x => x.Selected).ToList() ?? [];
        if (selected.Count == 0) { MessageBox.Show(this, "Nincs kijelölt játék."); return; }
        CredentialStore.SaveApiKey(_apiKey.Text);
        if (!string.IsNullOrWhiteSpace(_apiKey.Text))
        {
            var client = new SteamGridDbClient(_apiKey.Text.Trim());
            foreach (var game in selected)
            {
                if (!ChooseArtworkSet(client, game)) { _status.Text = "Importálás megszakítva az artwork-választásnál."; return; }
            }
            _grid.Refresh();
        }
        if (Process.GetProcessesByName("steam").Length > 0)
        {
            MessageBox.Show(this,
                "A Steam még fut, ezért most nem módosítottam semmit.\n\n" +
                "A tálca jobb alsó sarkában kattints jobb gombbal a Steam ikonra, válaszd a Kilépés lehetőséget, majd próbáld újra.",
                "Előbb zárd be a Steamet", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string config = Path.Combine(_steamPath, "userdata", user, "config"); string vdf = Path.Combine(config, "shortcuts.vdf");
        try
        {
            if (File.Exists(vdf)) { string backupDir = Path.Combine(config, "SteamGameImporter_Backups"); Directory.CreateDirectory(backupDir); File.Copy(vdf, Path.Combine(backupDir, $"shortcuts_{DateTime.Now:yyyyMMdd_HHmmss}.vdf")); }
            var shortcuts = ShortcutVdf.Read(vdf); int added = 0, skipped = 0;
            foreach (var game in selected)
            {
                string exe = Quote(game.Executable), start = Quote(game.StartDirectory);
                bool duplicate = shortcuts.Any(x => Normalize(x.Exe) == Normalize(exe) || (x.AppName.Equals(game.Name, StringComparison.OrdinalIgnoreCase) && Normalize(x.StartDir) == Normalize(start)));
                if (duplicate) { skipped++; continue; }
                shortcuts.Add(new SteamShortcut { AppName = game.Name.Trim(), Exe = exe, StartDir = start, LaunchOptions = game.LaunchOptions.Trim() }); added++;
            }
            ShortcutVdf.Write(vdf, shortcuts);
            var verified = ShortcutVdf.Read(vdf);
            int verifiedCount = selected.Count(game => verified.Any(x => Normalize(x.Exe) == Normalize(Quote(game.Executable))));
            if (verifiedCount < added)
                throw new InvalidDataException($"Az ellenőrzés sikertelen: {added} új bejegyzésből csak {verifiedCount} olvasható vissza.");
            int artworkDownloaded = 0;
            var artworkErrors = new List<string>();
            if (!string.IsNullOrWhiteSpace(_apiKey.Text))
                (artworkDownloaded, artworkErrors) = await DownloadArtwork(selected, user);
            else
                artworkErrors.Add("Nincs megadva SteamGridDB API-kulcs.");
            string log = Path.Combine(config, "SteamGameImporter_last_import.txt");
            File.WriteAllText(log, $"Idő: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\nFájl: {vdf}\r\nSteam-felhasználó: {user}\r\nHozzáadva: {added}\r\nDuplikáció: {skipped}\r\nVisszaellenőrizve: {verifiedCount}\r\nArtwork letöltve: {artworkDownloaded}\r\nArtwork hibák:\r\n{string.Join("\r\n", artworkErrors)}\r\n");
            _status.Text = $"Kész: {added} hozzáadva, {skipped} duplikáció kihagyva, fájl ellenőrizve.";
            string artMessage = artworkErrors.Count == 0
                ? $"Artwork: {artworkDownloaded} borító letöltve."
                : $"Artwork: {artworkDownloaded} letöltve, {artworkErrors.Count} hiba. Részletek a naplóban.";
            MessageBox.Show(this, $"{added} játék hozzáadva. {skipped} duplikáció kihagyva.\n{artMessage}\n\nSteam-profil: {user}\nFájl: {vdf}\n\nMost indítsd el a Steamet.", "Importálás kész");
        }
        catch (Exception ex) { MessageBox.Show(this, "A Steam könyvtár nem módosítható:\n" + ex.Message, "Hiba", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task<(int Downloaded, List<string> Errors)> DownloadArtwork(IEnumerable<GameEntry> games, string user)
    {
        var client = new SteamGridDbClient(_apiKey.Text.Trim()); string grid = Path.Combine(_steamPath!, "userdata", user, "config", "grid");
        int downloaded = 0;
        var errors = new List<string>();
        foreach (var game in games)
        {
            _status.Text = "Borítókép: " + game.Name;
            try
            {
                uint appId = ShortcutVdf.ComputeAppId(Quote(game.Executable), game.Name);
                var assets = new[]
                {
                    (game.ArtworkUrl, Path.Combine(grid, appId + "p"), "borító"),
                    (game.HeroUrl, Path.Combine(grid, appId + "_hero"), "háttér"),
                    (game.LogoUrl, Path.Combine(grid, appId + "_logo"), "logó")
                };
                foreach (var asset in assets)
                {
                    if (asset.Item1 is null) { errors.Add($"{game.Name}: {asset.Item3} kihagyva."); continue; }
                    await client.DownloadGridAsync(asset.Item1, asset.Item2, CancellationToken.None); downloaded++;
                }
            }
            catch (Exception ex) { errors.Add($"{game.Name}: {ex.Message}"); }
        }
        return (downloaded, errors);
    }

    private async Task ChooseArtworkForCurrentRow()
    {
        _grid.EndEdit();
        if (_grid.CurrentRow?.DataBoundItem is not GameEntry game) { MessageBox.Show(this, "Előbb jelölj ki egy játék-sort."); return; }
        if (string.IsNullOrWhiteSpace(_apiKey.Text)) { MessageBox.Show(this, "Előbb add meg a SteamGridDB API-kulcsot.", "API-kulcs szükséges"); return; }
        CredentialStore.SaveApiKey(_apiKey.Text);
        var client = new SteamGridDbClient(_apiKey.Text.Trim());
        if (ChooseArtworkSet(client, game)) _grid.Refresh();
        await Task.CompletedTask;
    }

    private bool ChooseArtworkSet(SteamGridDbClient client, GameEntry game)
    {
        var choices = new[]
        {
            (ArtworkKind.Cover, game.ArtworkUrl),
            (ArtworkKind.Hero, game.HeroUrl),
            (ArtworkKind.Logo, game.LogoUrl)
        };
        var selected = new string?[3];
        for (int i = 0; i < choices.Length; i++)
        {
            using var picker = new ArtworkPickerForm(client, game.Name, choices[i].Item2, choices[i].Item1);
            if (picker.ShowDialog(this) != DialogResult.OK) return false;
            selected[i] = picker.SelectedArtworkUrl;
        }
        game.ArtworkUrl = selected[0]; game.HeroUrl = selected[1]; game.LogoUrl = selected[2];
        return true;
    }

    private static string Quote(string s) => "\"" + s.Trim().Trim('"') + "\"";
    private static string Normalize(string s) => s.Trim().Trim('"').Replace('/', '\\').TrimEnd('\\').ToLowerInvariant();
}
