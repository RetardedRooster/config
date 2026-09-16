# Steam Game Importer

Natív Windows-alkalmazás nem Steames játékok felderítésére és a Steam könyvtárhoz adására. Nem használ böngészős felületet vagy Pythont.

## Funkciók

- mappa vagy teljes meghajtó átvizsgálása játékindító EXE-k után;
- kézi EXE- és játékmappa-választás a Windows Intéző párbeszédablakaival;
- több Steam-felhasználó támogatása;
- `shortcuts.vdf` automatikus biztonsági mentése;
- útvonal- és névalapú duplikációvédelem;
- opcionális 600×900-as SteamGridDB-borító API-kulccsal;
- önálló, egyfájlos `win-x64` kiadás.

## Fordítás Windows alatt

1. Telepítsd a Microsoft **.NET 8 SDK** csomagot: https://dotnet.microsoft.com/download/dotnet/8.0
2. Jobb klikk a `build-windows.ps1` fájlon, majd **Futtatás PowerShell-lel**.
3. A kész program: `publish\win-x64\SteamGameImporter.exe`.

Ha a PowerShell blokkolja a helyi scriptet, terminálból futtasd:

```powershell
powershell -ExecutionPolicy Bypass -File .\build-windows.ps1
```

## Használat

1. Válassz keresési mappát és indítsd el a keresést, vagy használd a **Kézi hozzáadás** gombot.
2. Ellenőrizd és szükség szerint szerkeszd a táblázatot.
3. Válaszd ki a Steam-felhasználót.
4. Teljesen lépj ki a Steamből a tálca Steam ikonjának **Kilépés** parancsával, majd kattints a **Hozzáadás a Steamhez** gombra. A program nem engedi az írást, amíg a `steam.exe` fut.
5. Indítsd újra a Steamet.

SteamGridDB-borítóhoz saját API-kulcs szükséges. Kulcs nélkül minden más funkció működik.

## Biztonság

A program minden módosítás előtt időbélyeges mentést készít ide:

`Steam\userdata\<felhasználó>\config\SteamGameImporter_Backups`

Az alkalmazás nem tölt le és nem telepít játékokat.

Az utolsó import ellenőrzési naplója: `Steam\userdata\<felhasználó>\config\SteamGameImporter_last_import.txt`.
