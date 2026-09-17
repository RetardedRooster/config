# Steam Game Importer – Detailed English User Guide

## 1. Download and start

1. Download `SteamGameImporter-Windows-x64-English.zip`.
2. Right-click the ZIP and choose **Extract All**.
3. Start the extracted `SteamGameImporter.exe`.
4. If Windows SmartScreen warns you, confirm that the program came from the official release. If you trust it, choose **More info**, then **Run anyway**.

The package is a self-contained Windows x64 application. A separate .NET installation is not required.

## 2. Create a SteamGridDB API key

The API key lets the application search for covers, backgrounds and logos. Keep it private and do not include it in screenshots.

1. Open [SteamGridDB](https://www.steamgriddb.com/).
2. Select **Log in** and sign in with your Steam account.
3. Open your profile menu and select **Preferences**.
4. Open the **API** tab, or go directly to [steamgriddb.com/profile/preferences/api](https://www.steamgriddb.com/profile/preferences/api).
5. Select **Generate API key**. You can use an existing key if one is already listed.
6. Copy the entire key without leading or trailing spaces.

If the key is exposed, revoke it on that page and generate a replacement.

## 3. Enter the API key

1. Start Steam Game Importer.
2. Locate **SteamGridDB API key (optional)** at the bottom of the window.
3. Paste the key with `Ctrl+V`. Its characters are hidden for security.
4. Click elsewhere or close the application normally. The program encrypts the key for the current Windows user and saves it locally.
5. Select a game row and click **Choose artwork**. If results appear, the key works.

Common problems:

- **401 / Unauthorized:** the key is invalid, incomplete or revoked. Copy it again or generate a new one.
- **No results:** try the official English game title or a shorter title.
- **Network error:** check the connection, firewall and SteamGridDB availability.
- **Key not retained:** do not alternate between Administrator and normal-user launches; encryption is tied to the Windows user.

## 4. Find and select games

1. Enter the game folder in the top field or use **Browse…**.
2. Do not select Steam's own `SteamLibrary`; the scanner excludes Steam libraries.
3. Click **Scan**. Use **Stop** to cancel.
4. Keep check marks only beside games you want to import.
5. Use **Remove selected row** for an incorrect result.
6. If a game is missing, choose **Add manually**, select its launcher `.exe`, then its game folder.
7. You can edit the name, start directory and launch options in the table.

## 5. Select artwork

With a valid API key, the program automatically chooses the first matching cover, background and logo, then displays a preview before import.

- **Approve and continue:** accept the selected images.
- **Change…:** show SteamGridDB results for that artwork type.
- **Skip:** do not download that artwork type.
- In the result window, edit the search title if needed, select the correct game, then click the image you want.
- Use **Choose artwork** on the main window to configure the selected row before import.

## 6. Add games to Steam

1. Select the correct Steam user at the bottom.
2. Confirm the game check marks.
3. Click **Add to Steam** and review the artwork.
4. Fully exit Steam when requested: right-click the Steam icon in the Windows notification area and choose **Exit**. Closing only its main window is not enough.
5. If the running client stopped the first attempt, click **Add to Steam** again after Steam exits.
6. Start Steam after a successful import. Games appear in the library and the **Installed** collection.

## 7. Backups and troubleshooting

Backups:

```text
Steam\userdata\<Steam user>\config\SteamGameImporter_Backups
```

Latest import log:

```text
Steam\userdata\<Steam user>\config\SteamGameImporter_last_import.txt
```

If artwork is missing, inspect `Artwork errors` in the log, verify the API key, and fully restart Steam.

## 8. Important notes

- The application does not download or install games; it adds existing executables to Steam.
- Duplicate executable paths are not imported twice.
- Only launch files from sources you trust.
- Keep your SteamGridDB API key private.
