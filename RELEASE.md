# Release-Checkliste für CandyTv Plugin

## Erforderliche Dateien im ZIP-Package

Das Release-ZIP muss **ALLE** folgenden Dateien enthalten:

### Haupt-Plugin
- `CandyTv.dll` (Haupt-Plugin-Assembly)

### Icon
- `CandyTV.png` (Plugin-Icon, exakt diese Schreibweise mit großem V!)

### Abhängigkeiten (NuGet-Packages)
Die folgenden DLL-Dateien sind **zwingend erforderlich** und müssen aus dem Build-Output-Verzeichnis kopiert werden:

- `Diacritics.dll`
- `FuzzySharp.dll`
- `ICU4N.dll`
- `ICU4N.Transliterator.dll`
- `J2N.dll`
- `Newtonsoft.Json.dll`

## Build-Output-Verzeichnis

Alle Dateien befinden sich nach dem Build in:
```
Jellyfin.Xtream\bin\Release\net8.0\
```

## PowerShell-Befehl für ZIP-Erstellung

```powershell
Compress-Archive -Path `
  "Jellyfin.Xtream\bin\Release\net8.0\CandyTv.dll", `
  "Jellyfin.Xtream\bin\Release\net8.0\CandyTV.png", `
  "Jellyfin.Xtream\bin\Release\net8.0\Diacritics.dll", `
  "Jellyfin.Xtream\bin\Release\net8.0\FuzzySharp.dll", `
  "Jellyfin.Xtream\bin\Release\net8.0\ICU4N.dll", `
  "Jellyfin.Xtream\bin\Release\net8.0\ICU4N.Transliterator.dll", `
  "Jellyfin.Xtream\bin\Release\net8.0\J2N.dll", `
  "Jellyfin.Xtream\bin\Release\net8.0\Newtonsoft.Json.dll" `
  -DestinationPath "CandyTv_X.X.XX.zip" -Force
```

## Wichtige Hinweise

⚠️ **KRITISCH**: Ohne die Abhängigkeits-DLLs wird das Plugin **NICHT funktionieren** und Jellyfin kann es nicht laden!

⚠️ **Icon-Schreibweise**: Das Icon muss exakt `CandyTV.png` heißen (großes V), nicht `CandyTv.png`

## Version-Bump-Prozess

1. Version in `build.yaml` erhöhen
2. Version in `Jellyfin.Xtream.csproj` erhöhen (AssemblyVersion + FileVersion)
3. Neuen Eintrag in `manifest.json` hinzufügen
4. Build durchführen: `dotnet build --configuration Release`
5. ZIP mit **ALLEN** oben genannten Dateien erstellen
6. Git commit + push
7. Git Tag erstellen und pushen
8. GitHub Release mit dem vollständigen ZIP erstellen

## Fehlerprävention

- ✅ Immer prüfen, dass alle 8 Dateien im ZIP sind (7 DLLs + 1 PNG)
- ✅ ZIP vor dem Release entpacken und Inhalt verifizieren
- ✅ Sicherstellen, dass `CandyTV.png` vorhanden ist (nicht `CandyTv.png`)
