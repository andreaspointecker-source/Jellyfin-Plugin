# CandyTv Plugin - Projektstruktur

## Verzeichnisstruktur

```
Jellyfin.Xtream-original/
├── .claude/                    # Claude Code Konfiguration
├── Backups/                    # Alte Backups (historisch)
├── docs/                       # Dokumentation
│   ├── ai-behavior.md         # AI Assistant Verhaltensrichtlinien
│   ├── plan.md                # Entwicklungs-Roadmap
│   ├── PUBLISH.md             # GitHub Publishing Anleitung
│   ├── PROJECT_STRUCTURE.md   # Diese Datei
│   ├── RELEASE.md             # Release-Checkliste
│   ├── spec.md                # Technische Spezifikation
│   └── tasks.md               # Aufgabenliste
├── Jellyfin.Xtream/           # Plugin-Quellcode
│   ├── Api/                   # API Controller
│   ├── Client/                # Xtream API Client
│   ├── Configuration/         # Plugin-Konfiguration
│   │   └── Web/              # Web-UI Konfiguration
│   ├── icon/                  # Plugin-Icon
│   │   └── CandyTV.png       # Plugin-Icon (192x192)
│   ├── Providers/             # Metadata-Provider
│   ├── Service/               # Backend-Services
│   ├── bin/                   # Build-Output (nicht in Git)
│   ├── obj/                   # Build-Artefakte (nicht in Git)
│   ├── CatchupChannel.cs      # Catchup-Channel Implementierung
│   ├── Jellyfin.Xtream.csproj # C# Projektdatei
│   ├── LiveTvService.cs       # Live-TV Service
│   ├── Plugin.cs              # Plugin-Hauptklasse
│   ├── PluginServiceRegistrator.cs  # Dependency Injection
│   ├── SeriesChannel.cs       # Serien-Channel
│   └── VodChannel.cs          # VOD-Channel
├── build.yaml                 # Jellyfin Plugin Manifest
├── CandyTV.png               # Icon für Build (Kopie)
├── CandyTv_0.0.14.zip        # Aktuelles Release-Paket
├── jellyfin.ruleset          # Code-Analyse-Regeln
├── Jellyfin.Xtream.sln       # Visual Studio Solution
├── LICENSE                    # MIT Lizenz
├── manifest.json             # Plugin-Repository Manifest
└── README.md                 # Projekt-Readme

```

## Dateibeschreibungen

### Root-Level

- **build.yaml**: Jellyfin Plugin Build-Manifest (wird von GitHub Actions verwendet)
- **manifest.json**: Plugin-Repository Manifest für Jellyfin Katalog
- **CandyTV.png**: Icon-Datei (wird für Build benötigt)
- **jellyfin.ruleset**: Code-Analyse-Regeln für StyleCop
- **Jellyfin.Xtream.sln**: Visual Studio Solution-Datei

### Jellyfin.Xtream/ (Plugin-Quellcode)

#### Hauptdateien
- **Plugin.cs**: Plugin-Hauptklasse, implementiert `IPlugin`
- **PluginServiceRegistrator.cs**: Registriert Services für Dependency Injection
- **LiveTvService.cs**: Implementiert `ILiveTvService` für Live-TV Integration

#### Channel-Implementierungen
- **CatchupChannel.cs**: Catchup/Replay-Funktionalität
- **SeriesChannel.cs**: Serien-Management
- **VodChannel.cs**: Video-On-Demand-Management

#### Ordner
- **Api/**: API-Controller für zusätzliche Endpunkte
- **Client/**: Xtream API Client-Implementierung
- **Configuration/**: Plugin-Konfiguration und Web-UI
- **icon/**: Plugin-Icon-Dateien
- **Providers/**: Metadata-Provider für Jellyfin
- **Service/**: Backend-Services (EPG, Caching, etc.)

### docs/ (Dokumentation)

- **ai-behavior.md**: Verhaltensrichtlinien für AI-Assistenten
- **plan.md**: 6-Phasen Entwicklungs-Roadmap (Q1-Q4 2025)
- **PROJECT_STRUCTURE.md**: Dieses Dokument
- **PUBLISH.md**: Anleitung zum Veröffentlichen auf GitHub
- **RELEASE.md**: Release-Checkliste mit allen erforderlichen Dateien
- **spec.md**: Vollständige technische Spezifikation
- **tasks.md**: 28 detaillierte Aufgaben mit Sprint-Planung

## Build-Prozess

1. **Build**: `dotnet build --configuration Release`
2. **Output**: `Jellyfin.Xtream/bin/Release/net8.0/`
3. **ZIP-Erstellung**: Siehe `docs/RELEASE.md` für Details

## Git-Workflow

1. Version in `build.yaml` und `.csproj` erhöhen
2. Eintrag in `manifest.json` hinzufügen
3. Build durchführen
4. ZIP mit allen Dependencies erstellen (siehe `docs/RELEASE.md`)
5. Commit + Push
6. Tag erstellen: `git tag -a v0.0.X -m "Release vX.X.X"`
7. Tag pushen: `git push origin v0.0.X`
8. GitHub Release mit ZIP erstellen

## Wichtige Hinweise

- **Keine temporären Dateien committen**: Build-Outputs, ZIPs (außer aktuelles Release), .ps1-Scripts
- **Icon-Schreibweise**: `CandyTV.png` (großes V!)
- **Alle Dependencies im ZIP**: Siehe `docs/RELEASE.md`
- **Checksum berechnen**: `Get-FileHash -Algorithm MD5` vor manifest.json-Update
