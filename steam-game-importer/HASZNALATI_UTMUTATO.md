# Steam Game Importer – részletes használati útmutató

## 1. Letöltés és indítás

1. Töltsd le a `SteamGameImporter-Windows-x64.zip` fájlt.
2. Jobb gomb a ZIP-en, majd **Az összes kibontása**.
3. Indítsd el a kibontott `SteamGameImporter.exe` fájlt.
4. Ha a Windows SmartScreen figyelmeztet, ellenőrizd, hogy a fájlt a hivatalos letöltési linkről szerezted-e be. Ezután válaszd a **További információ**, majd a **Futtatás mindenképpen** lehetőséget, ha megbízol a programban.

A program önálló Windows x64 alkalmazás; külön .NET telepítés nem szükséges.

## 2. SteamGridDB API-kulcs létrehozása

Az API-kulcs a borító, háttér és logó kereséséhez szükséges. Ne oszd meg másokkal, és ne küldd el képernyőképen.

1. Nyisd meg a [SteamGridDB](https://www.steamgriddb.com/) weboldalát.
2. Kattints a jobb felső **Log in** lehetőségre, és jelentkezz be Steam-fiókkal.
3. Nyisd meg a jobb felső profilmenüt, majd válaszd a **Preferences** lehetőséget.
4. Nyisd meg az **API** lapot. Közvetlen cím: [steamgriddb.com/profile/preferences/api](https://www.steamgriddb.com/profile/preferences/api).
5. Kattints a **Generate API key** gombra. Ha már van kulcsod, a meglévőt is használhatod.
6. Másold ki a teljes kulcsot, szóközök nélkül.

Ha a kulcs nyilvánosságra kerül, vond vissza az API-oldalon, majd generálj újat.

## 3. Az API-kulcs megadása a programban

1. Indítsd el a Steam Game Importert.
2. Az ablak alján keresd meg a **SteamGridDB API-kulcs (opcionális)** mezőt.
3. Illeszd be a kulcsot a `Ctrl+V` billentyűkkel. A mező biztonsági okból elrejti a karaktereket.
4. Kattints máshová vagy zárd be szabályosan az alkalmazást. A program automatikusan, a jelenlegi Windows-felhasználóhoz titkosítva menti a kulcsot.
5. Jelölj ki egy játékot, majd kattints az **Artworkok kiválasztása** gombra. Ha megjelennek a találatok, a kulcs működik.

Gyakori hibák:

- **401 / Unauthorized:** hibás, hiányos vagy visszavont kulcs. Másold ki újra, vagy generálj újat.
- **Nincs találat:** próbáld a játék hivatalos, rövidebb angol címét.
- **Hálózati hiba:** ellenőrizd az internetet, a tűzfalat és a SteamGridDB elérhetőségét.
- **Nem marad meg a kulcs:** ne futtasd felváltva rendszergazdaként és normál felhasználóként, mert a mentés Windows-felhasználóhoz kötött.

## 4. Játékok keresése és kiválasztása

1. A felső mezőben add meg a játékokat tartalmazó mappát, vagy használd a **Tallózás…** gombot.
2. Ne a Steam saját `SteamLibrary` mappáját válaszd; a program ezt kizárja a keresésből.
3. Kattints a **Keresés** gombra. A **Leállítás** gombbal megszakíthatod a vizsgálatot.
4. Csak azoknál a játékoknál legyen pipa, amelyeket hozzá szeretnél adni.
5. Hibás találatnál használd a **Kijelölt sor törlése** gombot.
6. Ha egy játék nem található, válaszd a **Kézi hozzáadás** gombot, az indító `.exe` fájlt, majd a játék mappáját.
7. A játék nevét, kezdőmappáját és indítási opcióit a táblázatban módosíthatod.

## 5. Artworkök automatikus és kézi kiválasztása

Érvényes API-kulccsal a program automatikusan megkeresi az első megfelelő borítót, hátteret és logót. A Steamhez adás előtt minden kijelölt játékhoz előnézet jelenik meg.

- **Jóváhagyás és folytatás:** elfogadja az automatikus képeket.
- **Csere…:** megnyitja a SteamGridDB találatait az adott képtípushoz.
- **Kihagyás:** az adott képet nem tölti le.
- A találati ablakban módosíthatod a keresett címet, kiválaszthatod a megfelelő játékot, majd a kívánt képre kattinthatsz.
- Az **Artworkok kiválasztása** gombbal már importálás előtt kézzel beállíthatod a kijelölt sor képeit.

## 6. Hozzáadás a Steamhez

1. Alul válaszd ki a megfelelő Steam-felhasználót.
2. Ellenőrizd a hozzáadandó játékok pipáit.
3. Kattints a **Hozzáadás a Steamhez** gombra, majd ellenőrizd az artworköket.
4. Teljesen állítsd le a Steamet: a Windows értesítési területén kattints jobb gombbal a Steam ikonra, majd válaszd a **Kilépés** parancsot. Az ablak bezárása nem elég.
5. Ha az előző próbálkozást a futó Steam megállította, kattints ismét a **Hozzáadás a Steamhez** gombra.
6. Sikeres importálás után indítsd újra a Steamet. A játékok a könyvtárban és a **Telepített** kategóriában jelennek meg.

## 7. Mentések és hibakeresés

Biztonsági mentések:

```text
Steam\userdata\<Steam-felhasználó>\config\SteamGameImporter_Backups
```

Az utolsó import naplója:

```text
Steam\userdata\<Steam-felhasználó>\config\SteamGameImporter_last_import.txt
```

Ha az artwork nem jelenik meg, ellenőrizd a napló `Artwork hibák` részét, az API-kulcsot, majd indítsd újra teljesen a Steamet.

## 8. Fontos tudnivalók

- A program nem tölti le és nem telepíti a játékokat; meglévő indítófájlokat ad hozzá a Steamhez.
- A duplikált indítófájlokat nem adja hozzá újra.
- Csak megbízható helyről származó játékfájlt indíts el.
- Az API-kulcs személyes adat; ne tedd közzé.
