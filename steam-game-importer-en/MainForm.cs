using System.Diagnostics;

namespace SteamGameImporter;

internal sealed class MainForm : Form
{
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AutoGenerateColumns = false, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
    private readonly TextBox _scanPath = new() { PlaceholderText = "Folder or drive to scan…", Width = 430 };
    private readonly ComboBox _steamUser = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly TextBox _apiKey = new() { UseSystemPasswordChar = true, Width = 210, PlaceholderText = "SteamGridDB API key (optional)" };
    private readonly ToolStripStatusLabel _status = new("Ready");
    private CancellationTokenSource? _cts;
    private string? _steamPath;

    public MainForm()
    {
        Text = "Steam Game Importer"; Width = 1180; Height = 760; MinimumSize = new Size(900, 560); StartPosition = FormStartPosition.CenterScreen;
        var applicationIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (applicationIcon is not null) Icon = applicationIcon;
        BuildGrid(); BuildLayout(); SteamTheme.Apply(this);
        Load += (_, _) => { _apiKey.Text = CredentialStore.LoadApiKey(); InitializeSteam(); };
        FormClosing += (_, _) => CredentialStore.SaveApiKey(_apiKey.Text);
        _apiKey.Leave += (_, _) => CredentialStore.SaveApiKey(_apiKey.Text);
    }

    private void BuildGrid()
    {
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(GameEntry.Selected), HeaderText = "✓", Width = 38 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(GameEntry.Name), HeaderText = "Game name", Width = 230 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(GameEntry.Executable), HeaderText = "Executable", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(GameEntry.StartDirectory), HeaderText = "Start directory", Width = 260 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(GameEntry.LaunchOptions), HeaderText = "Launch options", Width = 150 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(GameEntry.ArtworkStatus), HeaderText = "Artwork", Width = 210, ReadOnly = true });
    }

    private void BuildLayout()
    {
        var choose = Button("Browse…", (_, _) => ChooseScanFolder()); var scan = Button("Scan", async (_, _) => await Scan());
        var addManual = Button("Add manually", (_, _) => AddManual()); var remove = Button("Remove selected row", (_, _) => RemoveRows());
        var artwork = Button("Choose artwork", async (_, _) => await ChooseArtworkForCurrentRow());
        var import = Button("Add to Steam", async (_, _) => await Import());
        var stop = Button("Stop", (_, _) => _cts?.Cancel());
        var scanRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
        scanRow.Controls.AddRange([_scanPath, choose, scan, stop]);
        var actionRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = Padding.Empty };
        actionRow.Controls.AddRange([addManual, remove, artwork]);
        var top = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(7), ColumnCount = 1, RowCount = 2, BackColor = SteamTheme.Panel };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        top.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        top.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        top.Controls.Add(scanRow, 0, 0); top.Controls.Add(actionRow, 0, 1);
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, Padding = new Padding(7), WrapContents = false };
        bottom.Controls.AddRange([new Label { Text = "Steam user:", AutoSize = true, Margin = new Padding(3, 9, 3, 3) }, _steamUser, _apiKey, import]);
        var status = new StatusStrip(); status.Items.Add(_status);
        var headerText = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = SteamTheme.Panel };
        headerText.Controls.Add(new Label { Text = "STEAM GAME IMPORTER", AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 18F), Margin = Padding.Empty });
        headerText.Controls.Add(new Label { Text = "Manage non-Steam games, artwork and categories", AutoSize = true, ForeColor = SteamTheme.Muted, Font = new Font("Segoe UI", 9.5F), Margin = Padding.Empty });
        var header = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = SteamTheme.Panel, Padding = new Padding(16, 2, 8, 4) };
        header.Controls.Add(headerText);
        Controls.Add(_grid); Controls.Add(bottom); Controls.Add(top); Controls.Add(header); Controls.Add(status);
    }

    private static Button Button(string text, EventHandler action) { var b = new Button { Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(0, 36), UseCompatibleTextRendering = false }; b.Click += action; return b; }

    private void InitializeSteam()
    {
        _steamPath = SteamLocator.FindSteamPath();
        if (_steamPath is null) { _status.Text = "Steam was not found. Install or launch Steam once."; return; }
        _steamUser.Items.AddRange(SteamLocator.FindUserIds(_steamPath).Cast<object>().ToArray()); if (_steamUser.Items.Count > 0) _steamUser.SelectedIndex = 0;
        string games = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Games");
        _scanPath.Text = Directory.Exists(games) ? games : Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";
    }

    private void ChooseScanFolder() { using var d = new FolderBrowserDialog { Description = "Select the folder or drive containing your games" }; if (d.ShowDialog(this) == DialogResult.OK) _scanPath.Text = d.SelectedPath; }

    private async Task Scan()
    {
        if (!Directory.Exists(_scanPath.Text)) { MessageBox.Show(this, "The selected folder does not exist.", "Error"); return; }
        _cts = new CancellationTokenSource(); _status.Text = "Scanning…";
        try
        {
            var progress = new Progress<string>(x => _status.Text = "Scanning: " + x);
            var games = await GameScanner.ScanAsync([_scanPath.Text], progress, _cts.Token);
            _grid.DataSource = games; _status.Text = $"{games.Count} potential executables found.";
        }
        catch (OperationCanceledException) { _status.Text = "Scan cancelled."; }
        finally { _cts.Dispose(); _cts = null; }
    }

    private void AddManual()
    {
        using var f = new OpenFileDialog { Filter = "Windows application (*.exe)|*.exe|All files (*.*)|*.*", Title = "Select the game executable" };
        if (f.ShowDialog(this) != DialogResult.OK) return;
        using var d = new FolderBrowserDialog { Description = "Select the game folder", SelectedPath = Path.GetDirectoryName(f.FileName) };
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
        if (_steamPath is null || _steamUser.SelectedItem is not string user) { MessageBox.Show(this, "No Steam user was found.", "Error"); return; }
        var selected = (_grid.DataSource as List<GameEntry>)?.Where(x => x.Selected).ToList() ?? [];
        if (selected.Count == 0) { MessageBox.Show(this, "No game is selected."); return; }
        CredentialStore.SaveApiKey(_apiKey.Text);
        if (!string.IsNullOrWhiteSpace(_apiKey.Text))
        {
            var client = new SteamGridDbClient(_apiKey.Text.Trim());
            await AutoSelectArtwork(client, selected);
            foreach (var game in selected)
            {
                using var review = new ArtworkReviewForm(client, game);
                if (review.ShowDialog(this) != DialogResult.OK) { _status.Text = "Import cancelled during artwork review."; return; }
            }
            _grid.Refresh();
        }
        if (Process.GetProcessesByName("steam").Length > 0)
        {
            MessageBox.Show(this,
                "Steam is still running, so no changes were made.\n\n" +
                "Right-click the Steam icon in the notification area, choose Exit, then try again.",
                "Close Steam first", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                var duplicate = shortcuts.FirstOrDefault(x => Normalize(x.Exe) == Normalize(exe) || (x.AppName.Equals(game.Name, StringComparison.OrdinalIgnoreCase) && Normalize(x.StartDir) == Normalize(start)));
                if (duplicate is not null)
                {
                    if (!duplicate.Tags.Contains("Installed", StringComparer.OrdinalIgnoreCase)) duplicate.Tags.Add("Installed");
                    skipped++; continue;
                }
                shortcuts.Add(new SteamShortcut { AppName = game.Name.Trim(), Exe = exe, StartDir = start, LaunchOptions = game.LaunchOptions.Trim(), Tags = ["Installed"] }); added++;
            }
            ShortcutVdf.Write(vdf, shortcuts);
            var verified = ShortcutVdf.Read(vdf);
            int verifiedCount = selected.Count(game => verified.Any(x => Normalize(x.Exe) == Normalize(Quote(game.Executable))));
            if (verifiedCount < added)
                throw new InvalidDataException($"Verification failed: {added} new entries, only {verifiedCount} could be read back.");
            int artworkDownloaded = 0;
            var artworkErrors = new List<string>();
            if (!string.IsNullOrWhiteSpace(_apiKey.Text))
                (artworkDownloaded, artworkErrors) = await DownloadArtwork(selected, user);
            else
                artworkErrors.Add("No SteamGridDB API key was provided.");
            string log = Path.Combine(config, "SteamGameImporter_last_import.txt");
            File.WriteAllText(log, $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\nFile: {vdf}\r\nSteam user: {user}\r\nAdded: {added}\r\nDuplicates: {skipped}\r\nVerified: {verifiedCount}\r\nArtwork downloaded: {artworkDownloaded}\r\nArtwork errors:\r\n{string.Join("\r\n", artworkErrors)}\r\n");
            _status.Text = $"Done: {added} added, {skipped} duplicates skipped, file verified.";
            string artMessage = artworkErrors.Count == 0
                ? $"Artwork: {artworkDownloaded} art assets downloaded."
                : $"Artwork: {artworkDownloaded} downloaded, {artworkErrors.Count} errors. See the log for details.";
            MessageBox.Show(this, $"{added} games added. {skipped} duplicates skipped.\n{artMessage}\n\nSteam profile: {user}\nFile: {vdf}\n\nStart Steam now.", "Import complete");
        }
        catch (Exception ex) { MessageBox.Show(this, "The Steam library could not be modified:\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private async Task<(int Downloaded, List<string> Errors)> DownloadArtwork(IEnumerable<GameEntry> games, string user)
    {
        var client = new SteamGridDbClient(_apiKey.Text.Trim()); string grid = Path.Combine(_steamPath!, "userdata", user, "config", "grid");
        int downloaded = 0;
        var errors = new List<string>();
        foreach (var game in games)
        {
            _status.Text = "Artwork: " + game.Name;
            try
            {
                uint appId = ShortcutVdf.ComputeAppId(Quote(game.Executable), game.Name);
                var assets = new[]
                {
                    (game.ArtworkUrl, Path.Combine(grid, appId + "p"), "cover"),
                    (game.HeroUrl, Path.Combine(grid, appId + "_hero"), "background"),
                    (game.LogoUrl, Path.Combine(grid, appId + "_logo"), "logo")
                };
                foreach (var asset in assets)
                {
                    if (asset.Item1 is null) { errors.Add($"{game.Name}: {asset.Item3} skipped."); continue; }
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
        if (_grid.CurrentRow?.DataBoundItem is not GameEntry game) { MessageBox.Show(this, "Select a game row first."); return; }
        if (string.IsNullOrWhiteSpace(_apiKey.Text)) { MessageBox.Show(this, "Enter your SteamGridDB API key first.", "API key required"); return; }
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
        game.ArtworkManuallyConfigured = true;
        return true;
    }

    private async Task AutoSelectArtwork(SteamGridDbClient client, IEnumerable<GameEntry> games)
    {
        foreach (var game in games.Where(x => !x.ArtworkManuallyConfigured))
        {
            _status.Text = "Automatic artwork: " + game.Name;
            try
            {
                var matches = await client.SearchGamesAsync(game.Name, CancellationToken.None);
                if (matches.Count == 0) continue;
                int gameId = matches[0].Id;
                game.ArtworkUrl = (await client.GetArtworkAsync(gameId, ArtworkKind.Cover, CancellationToken.None)).FirstOrDefault()?.Url;
                game.HeroUrl = (await client.GetArtworkAsync(gameId, ArtworkKind.Hero, CancellationToken.None)).FirstOrDefault()?.Url;
                game.LogoUrl = (await client.GetArtworkAsync(gameId, ArtworkKind.Logo, CancellationToken.None)).FirstOrDefault()?.Url;
            }
            catch { }
        }
    }

    private static string Quote(string s) => "\"" + s.Trim().Trim('"') + "\"";
    private static string Normalize(string s) => s.Trim().Trim('"').Replace('/', '\\').TrimEnd('\\').ToLowerInvariant();
}
