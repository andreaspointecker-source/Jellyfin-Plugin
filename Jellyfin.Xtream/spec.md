# CandyTv (Jellyfin.Xtream) - Technische Spezifikation

## Projektübersicht

**CandyTv** (ehemals Jellyfin.Xtream) ist ein umfassendes Jellyfin-Plugin für die Integration von Xtream-kompatiblen IPTV-APIs. Es ermöglicht Live-TV-Streaming mit EPG, Video-On-Demand, TV-Serien und TV-Aufzeichnungen.

### Projekt-Metadaten

- **Projektname**: CandyTv
- **Assembly-Name**: CandyTv
- **Plugin-GUID**: `5d774c35-8567-46d3-a950-9bb8227a0c5d`
- **Aktuelle Version**: 0.0.2
- **Lizenz**: GPL-3.0
- **Target Framework**: .NET 8.0
- **Jellyfin Target ABI**: 10.10.7.0
- **Kategorie**: Live TV
- **Root-Verzeichnis**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream`
- **Repository**: https://github.com/andreaspointecker-source/Jellyfin-Plugin

---

## Architektur-Übersicht

### System-Design

Das Plugin folgt einem mehrschichtigen Architekturmuster:

```
┌─────────────────────────────────────────────────────┐
│         Jellyfin Server Integration                 │
│  (ILiveTvService, IChannel, ISupportsDirectStream) │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────────┐
│              Channel-Schicht                        │
│  LiveTvService | VodChannel | SeriesChannel |      │
│  CatchupChannel                                     │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────────┐
│              Service-Schicht                        │
│  StreamService | TaskService | CacheService |       │
│  ConnectionManager | ChannelListService |           │
│  ThumbnailCacheService                              │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────────┐
│              Client-Schicht                         │
│  XtreamClient | ConnectionInfo | JSON Converters    │
└────────────────┬────────────────────────────────────┘
                 │
┌────────────────┴────────────────────────────────────┐
│         Xtream-Kompatibler API-Server               │
└─────────────────────────────────────────────────────┘
```

### Komponenten-Hierarchie

```
Plugin.cs (Einstiegspunkt)
    ├── StreamService (Core-Logic)
    ├── TaskService (Scheduled-Tasks)
    │
    ├── LiveTvService (ILiveTvService)
    │   └── EPG-Cache (IMemoryCache, 10min TTL)
    │
    ├── VodChannel (IChannel)
    ├── SeriesChannel (IChannel)
    └── CatchupChannel (IChannel)

StreamService
    ├── XtreamClient (HTTP-Client)
    ├── ConnectionManager (Queue)
    ├── CacheService (Extended-Cache)
    └── ThumbnailCacheService (Disk-Cache)

XtreamClient
    └── JSON-Converters
        ├── Base64Converter
        ├── IntToBoolConverter
        ├── SingularToListConverter
        └── UnixTimestampConverter
```

---

## Kern-Komponenten

### 1. Plugin-Einstiegspunkt

**Datei**: `Plugin.cs`
**Standort**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream\Plugin.cs`

Die Haupt-Plugin-Klasse, erbt von `BasePlugin<PluginConfiguration>` und implementiert `IHasWebPages`.

**Wichtige Eigenschaften**:
```csharp
public override string Name => "CandyTv";                                    // Plugin.cs:54
public override Guid Id => Guid.Parse("5d774c35-8567-46d3-a950-9bb8227a0c5d"); // Plugin.cs:57
public ConnectionInfo Creds => new(Configuration.BaseUrl, ...);              // Plugin.cs:62
public string DataVersion => Assembly.GetVersion() + Config.GetHashCode();   // Plugin.cs:67
```

**Hauptverantwortlichkeiten**:
- Plugin-Lebenszyklus-Verwaltung
- Konfigurations-Management
- Service-Initialisierung (StreamService, TaskService)
- Web-UI-Seiten-Registrierung
- Automatische TV-Guide/Kanal-Aktualisierung bei Config-Änderungen

**Wichtige Methoden**:
- `UpdateConfiguration()`: Löst automatische Aktualisierung aus (Plugin.cs:117)
- `GetPages()`: Gibt eingebettete Web-Ressourcen zurück (Plugin.cs:95)

---

### 2. Channel-Implementierungen

#### 2.1 LiveTvService

**Datei**: `LiveTvService.cs`
**Standort**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream\LiveTvService.cs:46`

Implementiert `ILiveTvService` und `ISupportsDirectStreamProvider` für Live-IPTV.

**Hauptfunktionen**:
- Live-TV-Kanal-Auflistung
- EPG-Datenabruf mit 10-minütigem Memory-Cache
- Direct-Stream-Provider mit Restream-Pufferung
- Kanal-Metadaten (Nummer, Name, Bild, Tags)

**Wichtige Methoden**:
```csharp
// Alle konfigurierten Live-Kanäle abrufen
public async Task<IEnumerable<ChannelInfo>> GetChannelsAsync(...)           // LiveTvService.cs:55

// EPG-Programme für Datumsbereich (mit 10-Min-Cache)
public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(...)           // LiveTvService.cs:157

// Direct-Stream mit Restream-Pufferung
public async Task<ILiveStream> GetChannelStreamWithDirectStreamProvider(...) // LiveTvService.cs:208
```

**EPG-Caching-Logik** (LiveTvService.cs:166-194):
```csharp
string key = $"xtream-epg-{channelId}";
if (memoryCache.TryGetValue(key, out ICollection<ProgramInfo>? cached))
{
    return cached; // Cache-Hit
}

// Cache-Miss: Von API abrufen
var epg = await client.GetEpgInfoAsync(creds, streamId, ct);
memoryCache.Set(key, epg, DateTimeOffset.Now.AddMinutes(10)); // 10min TTL
```

**Datenfluss**:
```
User-Request
    → LiveTvService.GetChannelsAsync()
    → StreamService.GetLiveStreamsWithOverrides()
    → PluginConfiguration (Filter nach Kategorien)
    → ChannelMappings (Custom-Ordering anwenden)
    → XtreamClient.GetLiveStreamsAsync()
    → ConnectionManager (optional)
    → Xtream API
    → ThumbnailCache
    → Jellyfin
```

---

#### 2.2 VodChannel

**Datei**: `VodChannel.cs`
**Standort**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream\VodChannel.cs:39`

Implementiert `IChannel` für Video-On-Demand-Inhalte.

**Struktur**:
```
Root → Kategorien → VOD-Streams
```

**Hauptfunktionen**:
- Kategoriebasierte Organisation
- VOD-Metadaten (Titel, Cover-Art, Erstellungsdatum, Tags)
- Stream-Quellen-Info-Generierung
- Provider-ID-Unterstützung für Metadaten-Provider

**Wichtige Methoden**:
```csharp
// Kategorien oder Streams basierend auf Folder-ID
public async Task<ChannelItemResult> GetChannelItems(...)  // VodChannel.cs:90

// VOD-Kanal-Sichtbarkeit pro User
public bool IsEnabledFor(string userId)                    // VodChannel.cs:174
```

**Media-Source-Generierung** (VodChannel.cs:123):
```csharp
List<MediaSourceInfo> sources = [
    Plugin.Instance.StreamService.GetMediaSourceInfo(
        StreamType.Vod,
        stream.StreamId,
        stream.ContainerExtension)
];
```

---

#### 2.3 SeriesChannel

**Datei**: `SeriesChannel.cs`
**Standort**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream\SeriesChannel.cs:39`

Implementiert `IChannel` für TV-Serien-Inhalte.

**Hierarchische Struktur**:
```
Root → Serien-Kategorien → Serien → Staffeln → Episoden
```

**GUID-Präfixe** (definiert in StreamService.cs:51-68):
- `SeriesCategoryPrefix`: 0x5d774c37
- `SeriesPrefix`: 0x5d774c38
- `SeasonPrefix`: 0x5d774c39
- `EpisodePrefix`: 0x5d774c3a

**Hauptfunktionen**:
- Hierarchie: Kategorien → Serien → Staffeln → Episoden
- Metadaten: Bewertungen, Genres, Besetzung, Ausstrahlungsdaten
- Episoden-Video/Audio-Codec-Informationen
- Staffel- und Episoden-Cover-Art

**Navigations-Logic** (SeriesChannel.cs:91):
```csharp
if (string.IsNullOrEmpty(query.FolderId))
    return await GetCategories(...);                    // Root-Ebene

Guid guid = Guid.Parse(query.FolderId);
StreamService.FromGuid(guid, out int prefix, ...);

if (prefix == StreamService.SeriesCategoryPrefix)
    return await GetSeries(categoryId, ...);            // Kategorie-Ebene

if (prefix == StreamService.SeriesPrefix)
    return await GetSeasons(seriesId, ...);             // Serien-Ebene

if (prefix == StreamService.SeasonPrefix)
    return await GetEpisodes(seriesId, seasonId, ...);  // Staffel-Ebene
```

---

#### 2.4 CatchupChannel

**Datei**: `CatchupChannel.cs`
**Standort**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream\CatchupChannel.cs:39`

Implementiert `IChannel` für TV-Aufzeichnungen (zeitversetztes Ansehen).

**Struktur**:
```
Root → Kanäle (mit Aufzeichnung) → Tage → EPG-Programme
```

**Hauptfunktionen**:
- EPG-basierte Aufzeichnung mit konfigurierbarer Archiv-Dauer
- Tag-für-Tag-Browsing-Interface
- Fallback auf ganztägige Aufzeichnung wenn keine EPG-Daten verfügbar
- Automatische datumsbasierte Cache-Invalidierung

**DataVersion** (CatchupChannel.cs:50):
```csharp
// Enthält aktuelles Datum für tägliche Cache-Invalidierung
public string DataVersion => Plugin.Instance.DataVersion + DateTime.Today.ToShortDateString();
```

**Timeshift-URL-Generierung** (via StreamService.cs:418):
```csharp
string uri = $"{baseUrl}/streaming/timeshift.php?" +
    $"username={username}&password={password}&" +
    $"stream={streamId}&start={YYYY-MM-DD:HH-mm}&duration={minutes}";
```

---

### 3. Service-Schicht

#### 3.1 StreamService

**Datei**: `Service/StreamService.cs`
**Standort**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream\Service\StreamService.cs:38`

Zentraler Service für die Verwaltung von Streams, Kategorien und Media-Source-Informationen.

**GUID-Kodierungs-System**:

Das Plugin verwendet ein einzigartiges GUID-System zur Identifizierung verschiedener Inhaltstypen. Jede GUID kodiert vier 32-Bit-Integers:

```csharp
// GUID-Struktur: [Prefix][ID1][ID2][ID3]
// Beispiel: [LiveTvPrefix][ChannelID][0][0]
```

**GUID-Präfixe** (StreamService.cs:40-93):

| Präfix-Konstante | Hex-Wert | Zweck | Verwendung |
|-----------------|----------|-------|------------|
| `VodCategoryPrefix` | 0x5d774c35 | VOD-Kategorie-Ordner | VodChannel Root |
| `StreamPrefix` | 0x5d774c36 | Stream-Items | VOD-Streams |
| `SeriesCategoryPrefix` | 0x5d774c37 | Serien-Kategorie-Ordner | SeriesChannel Root |
| `SeriesPrefix` | 0x5d774c38 | Serien-Ordner | Serien |
| `SeasonPrefix` | 0x5d774c39 | Staffel-Ordner | Staffeln |
| `EpisodePrefix` | 0x5d774c3a | Episoden-Items | Episoden |
| `CatchupPrefix` | 0x5d774c3b | Aufzeichnungs-Kanal-Ordner | Catchup-Channels |
| `CatchupStreamPrefix` | 0x5d774c3c | Aufzeichnungs-Stream-Items | Catchup-Streams |
| `MediaSourcePrefix` | 0x5d774c3d | Media-Source-Identifikation | Stream-URLs |
| `LiveTvPrefix` | 0x5d774c3e | Live-TV-Kanäle | Live-Channels |
| `EpgPrefix` | 0x5d774c3f | EPG-Programm-Einträge | EPG-Items |

**Wichtige Methoden**:

```csharp
// Vier Integers in GUID kodieren
public static Guid ToGuid(int i0, int i1, int i2, int i3)                    // StreamService.cs:339

// GUID zurück in Integers dekodieren
public static void FromGuid(Guid id, out int i0, out int i1, ...)            // StreamService.cs:357

// Tags aus Stream-Namen extrahieren (z.B. [HD], |4K|)
public static ParsedName ParseName(string name)                              // StreamService.cs:109

// Konfigurierte Live-Kanäle abrufen
public async Task<IEnumerable<StreamInfo>> GetLiveStreams(...)               // StreamService.cs:157

// Live-Kanäle mit Custom-Ordering
public async Task<IEnumerable<StreamInfo>> GetLiveStreamsWithOverrides(...)  // StreamService.cs:171

// Media-Source-Info mit Stream-URLs generieren
public MediaSourceInfo GetMediaSourceInfo(StreamType type, int id, ...)      // StreamService.cs:389
```

**Tag-Parsing-Logic** (StreamService.cs:109):

Unterstützt mehrere Tag-Formate:
- `[TAG]` - Eckige Klammern
- `|TAG|` - Pipe-Trennzeichen
- Unicode Block Elements (U+2580 - U+259F) als Trennzeichen

```csharp
// Beispiel-Parsing
"Arte HD [HD] |German|"
    → Title: "Arte HD"
    → Tags: ["HD", "German"]
```

**Channel-List-Override-Logic** (StreamService.cs:177):

```csharp
if (config.ChannelMappings != null && config.ChannelMappings.Count > 0)
{
    // Custom-Channel-List-Ordering verwenden
    var orderedStreams = new List<StreamInfo>();
    var streamById = streams.GroupBy(s => s.StreamId)
                           .ToDictionary(g => g.Key, g => g.First());

    var sortedMappings = config.ChannelMappings.Values
                               .OrderBy(m => m.Position)
                               .ToList();

    foreach (var mapping in sortedMappings)
    {
        if (streamById.TryGetValue(mapping.StreamId, out var stream))
        {
            stream.Num = mapping.Position + 1; // Channel-Nummer setzen
            orderedStreams.Add(stream);
        }
    }

    return orderedStreams; // NUR Streams aus Custom-Liste
}

return streams; // Alle konfigurierten Streams
```

**Media-Source-URL-Generierung** (StreamService.cs:389):

```csharp
// Live-TV
uri = $"{baseUrl}/{username}/{password}/{streamId}";

// VOD
uri = $"{baseUrl}/movie/{username}/{password}/{streamId}.{ext}";

// Serien
uri = $"{baseUrl}/series/{username}/{password}/{episodeId}.{ext}";

// Catchup
uri = $"{baseUrl}/streaming/timeshift.php?" +
      $"username={username}&password={password}&" +
      $"stream={streamId}&start={YYYY-MM-DD:HH-mm}&duration={min}";
```

---

#### 3.2 ConnectionManager

**Datei**: `Service/ConnectionManager.cs`

Verwaltet Verbindungs-Warteschlangen zur Xtream-API um Single-Connection-Constraint durchzusetzen.

**Funktionen**:
- Optionale Verbindungs-Warteschlange (konfigurierbar via `EnableConnectionQueue`)
- Anfrage-Statistik-Tracking
- Verhindert Überlastung des Providers mit gleichzeitigen Anfragen

**Statistiken**:
```csharp
public static bool IsBusy { get; }              // Aktueller Verbindungsstatus
public static int QueuedRequests { get; }       // Anzahl wartender Anfragen
public static long TotalRequests { get; }       // Gesamt-Anfragen
```

**Usage-Pattern**:
```csharp
if (Plugin.Instance.Configuration.EnableConnectionQueue)
{
    return await ConnectionManager.ExecuteAsync(
        async () => await apiCall(),
        null,
        cancellationToken);
}
else
{
    return await apiCall(); // Direkter Aufruf
}
```

---

#### 3.3 CacheService

**Datei**: `Service/CacheService.cs`

Bietet erweiterte Caching-Funktionen für API-Antworten.

**Funktionen**:
- Konfigurierbare Cache-Aufbewahrung (via `EnableExtendedCache`)
- Wartungsfenster-Unterstützung (konfigurierbare Stunden)
- Cache-Hit/Miss-Rate-Tracking

**Statistiken**:
```csharp
public static double CacheHitRate { get; }      // Prozentsatz der Cache-Treffer
public static long CacheHits { get; }           // Gesamt-Cache-Treffer
public static long CacheMisses { get; }         // Gesamt-Cache-Fehlschläge
```

**Wartungsfenster** (PluginConfiguration.cs:99):
```csharp
public int MaintenanceStartHour { get; set; } = 3;   // 03:00 Uhr
public int MaintenanceEndHour { get; set; } = 6;     // 06:00 Uhr
```

---

#### 3.4 ThumbnailCacheService

**Datei**: `Service/ThumbnailCacheService.cs`

Cached Thumbnail-Bilder vom Xtream-Provider auf lokale Festplatte.

**Funktionen**:
- Konfigurierbare Cache-Aufbewahrung (via `ThumbnailCacheRetentionDays`)
- Festplattenbasiertes Caching im Plugin-Daten-Verzeichnis
- URL-zu-lokaler-Pfad-Zuordnung
- Cache-Statistiken (Dateianzahl, Gesamtgröße)

**Cache-Location**:
```
{plugin_data_path}/thumbnails/
```

**Wichtige Methoden**:
```csharp
// Gibt gecachte lokale URL oder Original-URL zurück
public async Task<string?> GetCachedUrlAsync(string? url, ...)

// Cache-Statistiken
public static double CacheHitRate { get; }
public static int CachedImages { get; }
public static long CacheRequests { get; }
```

**Retention-Policy** (PluginConfiguration.cs:125):
```csharp
public int ThumbnailCacheRetentionDays { get; set; } = 30; // Standard: 30 Tage
```

---

#### 3.5 ChannelListService

**Datei**: `Service/ChannelListService.cs`

Behandelt benutzerdefiniertes Kanallisten-Parsing und Fuzzy-Matching.

**Funktionen**:
- TXT-Datei-Parsing für Kanallisten (ein Kanal pro Zeile)
- Fuzzy-Matching mit FuzzySharp-Bibliothek
- Top-N-Match-Abruf
- Exakt-Match-Erkennung

**Wichtige Methoden**:
```csharp
// Parst TXT-Content (newline-delimited)
public IReadOnlyCollection<string> ParseTxtContent(string content)

// Gibt Fuzzy-Matches mit Scores zurück
public IEnumerable<MatchResult> GetTopMatches(string channelName,
                                              IEnumerable<StreamInfo> streams,
                                              int topN)
```

**Fuzzy-Matching-Scores**:
- Score > 90: Sehr gute Übereinstimmung
- Score 70-90: Gute Übereinstimmung
- Score < 70: Schlechte Übereinstimmung

---

#### 3.6 TaskService

**Datei**: `Service/TaskService.cs`

Verwaltet Jellyfin-geplante Aufgaben.

**Hauptverantwortlichkeiten**:
- Laufende Aufgaben abbrechen
- Aufgaben zur Ausführung einreihen
- TV-Guide-Aktualisierung auslösen
- Kanal-Aktualisierung auslösen

**Usage** (Plugin.cs:124):
```csharp
// Force TV-Guide-Refresh bei Config-Update
TaskService.CancelIfRunningAndQueue(
    "Jellyfin.LiveTv",
    "Jellyfin.LiveTv.Guide.RefreshGuideScheduledTask");

// Force Channel-Refresh bei Config-Update
TaskService.CancelIfRunningAndQueue(
    "Jellyfin.LiveTv",
    "Jellyfin.LiveTv.Channels.RefreshChannelsScheduledTask");
```

---

#### 3.7 Restream

**Datei**: `Service/Restream.cs`

Bietet Direct-Stream-Pufferung für Live-TV.

**Funktionen**:
- HTTP-Stream-Pufferung
- Consumer-Count-Tracking
- Wrapped-Buffer-Streams (`WrappedBufferStream`, `WrappedBufferReadStream`)

**Wichtige Eigenschaften**:
```csharp
public const string TunerHost = "Jellyfin.Xtream.Restream";
public int ConsumerCount { get; set; }
public MediaSourceInfo MediaSource { get; }
```

**Usage** (LiveTvService.cs:208):
```csharp
// Vorhandene Streams wiederverwenden
ILiveStream? stream = currentLiveStreams.Find(
    s => s.TunerHostId == Restream.TunerHost &&
         s.MediaSource.Id == mediaSourceInfo.Id);

if (stream == null)
{
    stream = new Restream(appHost, httpClientFactory, logger, mediaSourceInfo);
    await stream.Open(cancellationToken);
}

stream.ConsumerCount++;
return stream;
```

---

### 4. Client-Schicht

#### 4.1 XtreamClient

**Datei**: `Client/XtreamClient.cs`
**Standort**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream\Client\XtreamClient.cs:37`

HTTP-Client für Xtream-API-Kommunikation.

**Hauptfunktionen**:
- User-Agent-Header: `Jellyfin.Xtream/{version}`
- JSON-Deserialisierung mit Newtonsoft.Json
- Optionale Verbindungs-Warteschlangen-Integration
- CancellationToken-Unterstützung

**API-Methoden**:

| Methode | Xtream-Action | Rückgabe | Zeile |
|---------|---------------|----------|-------|
| `GetUserAndServerInfoAsync()` | `player_api.php` | User/Server-Info | 85 |
| `GetLiveStreamsAsync()` | `get_live_streams` | Alle Live-Streams | 115 |
| `GetLiveStreamsByCategoryAsync()` | `get_live_streams&category_id=` | Streams in Kategorie | 121 |
| `GetLiveCategoryAsync()` | `get_live_categories` | Live-TV-Kategorien | 139 |
| `GetVodStreamsByCategoryAsync()` | `get_vod_streams&category_id=` | VOD-Streams | 103 |
| `GetVodInfoAsync()` | `get_vod_info&vod_id=` | VOD-Metadaten | 109 |
| `GetVodCategoryAsync()` | `get_vod_categories` | VOD-Kategorien | 133 |
| `GetSeriesByCategoryAsync()` | `get_series&category_id=` | Serien | 91 |
| `GetSeriesStreamsBySeriesAsync()` | `get_series_info&series_id=` | Staffeln/Episoden | 97 |
| `GetSeriesCategoryAsync()` | `get_series_categories` | Serien-Kategorien | 127 |
| `GetEpgInfoAsync()` | `get_simple_data_table&stream_id=` | EPG-Daten | 145 |

**Connection-Queue-Logic** (XtreamClient.cs:57):
```csharp
private async Task<T> QueryApi<T>(ConnectionInfo connectionInfo,
                                   string urlPath,
                                   CancellationToken cancellationToken)
{
    var config = Plugin.Instance?.Configuration;
    bool useConnectionQueue = config?.EnableConnectionQueue ?? false;

    if (useConnectionQueue)
    {
        return await ConnectionManager.ExecuteAsync(
            async () =>
            {
                Uri uri = new Uri(connectionInfo.BaseUrl + urlPath);
                string jsonContent = await client.GetStringAsync(uri, cancellationToken);
                return JsonConvert.DeserializeObject<T>(jsonContent)!;
            },
            null,
            cancellationToken);
    }
    else
    {
        // Direkter Aufruf ohne Warteschlange
        Uri uri = new Uri(connectionInfo.BaseUrl + urlPath);
        string jsonContent = await client.GetStringAsync(uri, cancellationToken);
        return JsonConvert.DeserializeObject<T>(jsonContent)!;
    }
}
```

---

#### 4.2 ConnectionInfo

**Datei**: `Client/ConnectionInfo.cs`

Kapselt Xtream-API-Zugangsdaten.

**Eigenschaften**:
```csharp
public string BaseUrl { get; }      // API-URL (mit Protokoll, ohne trailing slash)
public string UserName { get; }     // API-Benutzername
public string Password { get; }     // API-Passwort
```

**Usage**:
```csharp
var creds = new ConnectionInfo(
    "https://example.com",
    "username",
    "password"
);
```

---

#### 4.3 JSON-Konverter

Das Plugin enthält benutzerdefinierte JSON-Konverter um Eigenheiten in Xtream-API-Antworten zu behandeln:

| Konverter | Zweck | Standort |
|-----------|-------|----------|
| `Base64Converter` | Dekodiert Base64-kodierte Strings | `Client/Base64Converter.cs` |
| `SingularToListConverter` | Konvertiert einzelne Objekte zu Listen | `Client/SingularToListConverter.cs` |
| `OnlyObjectConverter` | Behandelt nur-Objekt-Antworten | `Client/OnlyObjectConverter.cs` |
| `IntToBoolConverter` | Konvertiert Integers (0/1) zu Booleans | `Client/IntToBoolConverter.cs` |
| `UnixTimestampConverter` | Konvertiert Unix-Timestamps zu DateTime | `Client/UnixTimestampConverter.cs` |

**Beispiel-Usage**:
```csharp
[JsonProperty("tv_archive")]
[JsonConverter(typeof(IntToBoolConverter))]
public bool TvArchive { get; set; }  // API: 0 oder 1 → bool

[JsonProperty("added")]
[JsonConverter(typeof(UnixTimestampConverter))]
public DateTime Added { get; set; }  // API: Unix-Timestamp → DateTime
```

---

### 5. Datenmodelle

#### 5.1 Client-Modelle

**Location**: `Client/Models/`

**Kern-Modelle**:

| Modell | Zweck | Wichtige Properties | Datei |
|--------|-------|---------------------|-------|
| `PlayerApi` | User/Server-Info | `UserInfo`, `ServerInfo` | `PlayerApi.cs` |
| `UserInfo` | Benutzerkonto | `Username`, `Status`, `ExpDate`, `MaxConnections` | `UserInfo.cs` |
| `ServerInfo` | Server-Details | `Url`, `Timezone` | `ServerInfo.cs` |
| `Category` | Content-Kategorie | `CategoryId`, `CategoryName` | `Category.cs` |
| `StreamInfo` | Live/VOD-Stream | `StreamId`, `Name`, `StreamIcon`, `Num`, `TvArchive` | `StreamInfo.cs` |
| `Series` | TV-Serien | `SeriesId`, `Name`, `Cover`, `Genre`, `Cast`, `Rating5Based` | `Series.cs` |
| `SeriesStreamInfo` | Serie mit Episoden | `Info`, `Seasons`, `Episodes` | `SeriesStreamInfo.cs` |
| `Season` | Staffel | `SeasonId`, `Name`, `Cover`, `AirDate`, `Overview` | `Season.cs` |
| `Episode` | Episode | `EpisodeId`, `Title`, `ContainerExtension`, `Added`, `Info` | `Episode.cs` |
| `EpisodeInfo` | Episode-Details | `Plot`, `MovieImage`, `Video`, `Audio` | `EpisodeInfo.cs` |
| `VideoInfo` | Video-Codec | `CodecName`, `Width`, `Height`, `AspectRatio`, `BitDepth` | `VideoInfo.cs` |
| `AudioInfo` | Audio-Codec | `CodecName`, `Bitrate`, `Channels`, `SampleRate` | `AudioInfo.cs` |
| `EpgListings` | EPG-Daten | `Listings` (List<EpgInfo>) | `EpgListings.cs` |
| `EpgInfo` | EPG-Programm | `Id`, `Title`, `Description`, `Start`, `End` | `EpgInfo.cs` |
| `VodStreamInfo` | VOD mit Metadaten | `Info`, `MovieData` | `VodStreamInfo.cs` |
| `VodInfo` | VOD-Metadaten | `TmdbId`, `Name`, `Plot`, `Cast`, `Rating` | `VodInfo.cs` |

---

#### 5.2 Konfigurations-Modelle

**Location**: `Configuration/`

**PluginConfiguration** (PluginConfiguration.cs:26):

| Property | Typ | Default | Beschreibung | Zeile |
|----------|-----|---------|--------------|-------|
| `BaseUrl` | string | "https://example.com" | Xtream-API-Basis-URL | 31 |
| `Username` | string | "" | API-Benutzername | 36 |
| `Password` | string | "" | API-Passwort | 41 |
| `IsCatchupVisible` | bool | false | Aufzeichnungs-Kanal anzeigen | 46 |
| `IsSeriesVisible` | bool | false | Serien-Kanal anzeigen | 51 |
| `IsVodVisible` | bool | false | VOD-Kanal anzeigen | 56 |
| `IsTmdbVodOverride` | bool | true | TMDB für VOD-Metadaten | 61 |
| `LiveTv` | Dict | {} | Ausgewählte Live-Kategorien/Kanäle | 66 |
| `Vod` | Dict | {} | Ausgewählte VOD-Kategorien/Streams | 71 |
| `Series` | Dict | {} | Ausgewählte Serien-Kategorien | 76 |
| `ChannelLists` | Collection | [] | Benutzerdefinierte Kanallisten | 81 |
| `ChannelMappings` | Dict | {} | Kanallisten-Zuordnungen | 86 |
| `EnableConnectionQueue` | bool | true | Verbindungs-Warteschlange aktivieren | 91 |
| `EnableExtendedCache` | bool | true | Erweiterten Cache aktivieren | 96 |
| `MaintenanceStartHour` | int | 3 | Wartungsfenster Start (0-23) | 101 |
| `MaintenanceEndHour` | int | 6 | Wartungsfenster Ende (0-23) | 106 |
| `EnableEpgPreload` | bool | true | Auto-Preload EPG-Daten | 111 |
| `EnableMetadataUpdate` | bool | true | Auto-Update Metadaten | 116 |
| `EnableThumbnailCache` | bool | true | Thumbnail-Caching aktivieren | 121 |
| `ThumbnailCacheRetentionDays` | int | 30 | Thumbnail-Cache-Aufbewahrung | 126 |

**Weitere Config-Modelle**:

| Modell | Zweck | Datei |
|--------|-------|-------|
| `ChannelOverrides` | Kanal-Anpassungen (Nummer, Name, Icon) | `ChannelOverrides.cs` |
| `ChannelList` | Benutzerdefinierte Kanalliste | `ChannelList.cs` |
| `ChannelMapping` | Kanallisten-Eintrag (StreamId, Position) | `ChannelMapping.cs` |
| `SerializableDictionary<K,V>` | XML-serialisierbares Dictionary | `SerializableDictionary.cs` |

---

#### 5.3 API-Modelle

**Location**: `Api/Models/`

| Modell | Zweck | Datei |
|--------|-------|-------|
| `CategoryResponse` | Kategorie für Admin-UI | `CategoryResponse.cs` |
| `ChannelResponse` | Kanal für Admin-UI | `ChannelResponse.cs` |
| `ItemResponse` | Stream/Serie für Admin-UI | `ItemResponse.cs` |
| `ChannelMatchResponse` | Fuzzy-Match-Ergebnis | `ChannelMatchResponse.cs` |
| `MatchChannelRequest` | Match-Request-Payload | `MatchChannelRequest.cs` |
| `ParseChannelListRequest` | Parse-Request-Payload | `ParseChannelListRequest.cs` |

---

### 6. API-Controller

**Datei**: `Api/XtreamController.cs`
**Standort**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream\Api\XtreamController.cs:40`

RESTful-API-Endpunkte für die Admin-UI des Plugins.

**Basis-Route**: `/Xtream`

**Endpunkte**:

| Methode | Route | Zweck | Auth | Zeile |
|---------|-------|-------|------|-------|
| GET | `/LiveCategories` | Live-TV-Kategorien | RequiresElevation | 82 |
| GET | `/LiveCategories/{categoryId}` | Live-Streams in Kategorie | RequiresElevation | 98 |
| GET | `/VodCategories` | VOD-Kategorien | RequiresElevation | 116 |
| GET | `/VodCategories/{categoryId}` | VOD-Streams in Kategorie | RequiresElevation | 132 |
| GET | `/SeriesCategories` | Serien-Kategorien | RequiresElevation | 150 |
| GET | `/SeriesCategories/{categoryId}` | Serien in Kategorie | RequiresElevation | 166 |
| GET | `/LiveTv` | Konfigurierte TV-Kanäle | RequiresElevation | 184 |
| POST | `/ChannelLists/Parse` | TXT-Kanalliste parsen | RequiresElevation | 198 |
| POST | `/ChannelLists/Match` | Fuzzy-Match Kanalname | RequiresElevation | 212 |
| GET | `/ChannelLists/AllStreams` | Alle verfügbaren Streams | RequiresElevation | 244 |
| GET | `/OptimizationStats` | Optimierungs-Statistiken | RequiresElevation | 273 |
| POST | `/ResetChannelOrder` | Kanal-Zuordnungen löschen | RequiresElevation | 295 |
| GET | `/UserInfo` | Xtream User/Server-Info | RequiresElevation | 328 |
| GET | `/ThumbnailCacheStats` | Thumbnail-Cache-Statistiken | RequiresElevation | 361 |
| POST | `/ClearThumbnailCache` | Thumbnail-Cache leeren | RequiresElevation | 403 |

**Optimierungs-Statistiken Response** (XtreamController.cs:274):
```json
{
  "isBusy": false,
  "queuedRequests": 0,
  "totalRequests": 1234,
  "cacheHitRate": 0.85,
  "cacheHits": 850,
  "cacheMisses": 150,
  "thumbnailCacheHitRate": 0.92,
  "thumbnailCachedImages": 450,
  "thumbnailCacheRequests": 489
}
```

---

### 7. Web-UI-Komponenten

**Location**: `Configuration/Web/`

Embedded HTML/CSS/JS-Ressourcen für die Admin-UI:

| Datei | Zweck |
|-------|-------|
| `XtreamCredentials.html` | Zugangsdaten-Konfiguration |
| `XtreamCredentials.js` | Zugangsdaten-Logic |
| `XtreamLive.html` | Live-TV-Kanal-Auswahl |
| `XtreamLive.js` | Live-TV-Logic |
| `XtreamLiveOverrides.html` | Kanal-Überschreibungen |
| `XtreamLiveOverrides.js` | Override-Management |
| `XtreamChannelLists.html` | Custom-Kanallisten |
| `XtreamChannelLists.js` | Kanallisten-Logic |
| `XtreamVod.html` | VOD-Kategorie-Auswahl |
| `XtreamVod.js` | VOD-Logic |
| `XtreamSeries.html` | Serien-Kategorie-Auswahl |
| `XtreamSeries.js` | Serien-Logic |
| `Xtream.css` | Gemeinsame CSS-Styles |
| `Xtream.js` | Gemeinsame JS-Utilities |

**Embedding** (Plugin.cs:95):
```csharp
public IEnumerable<PluginPageInfo> GetPages()
{
    return new[]
    {
        CreateStatic("XtreamCredentials.html"),
        CreateStatic("XtreamCredentials.js"),
        CreateStatic("Xtream.css"),
        CreateStatic("Xtream.js"),
        // ... weitere Ressourcen
    };
}
```

**Ressourcen-Pfad-Pattern**:
```
Jellyfin.Xtream.Configuration.Web.{filename}
```

---

## Datenfluss-Diagramme

### Live-TV-EPG-Fluss

```
Jellyfin EPG-Request
    ↓
LiveTvService.GetProgramsAsync(channelId, startDate, endDate)
    ↓
Memory-Cache prüfen (key: "xtream-epg-{channelId}", TTL: 10min)
    ↓ (Cache-Miss)
XtreamClient.GetEpgInfoAsync(streamId)
    ↓
ConnectionManager (falls EnableConnectionQueue = true)
    ↓
Xtream API: /player_api.php?action=get_simple_data_table&stream_id={id}
    ↓
JSON Deserialisieren (mit Custom-Converters)
    ↓
Im Memory-Cache speichern (10 Minuten)
    ↓
Nach Datumsbereich filtern (startDate ≤ epg.End && epg.Start < endDate)
    ↓
ProgramInfo-Liste an Jellyfin zurückgeben
```

### VOD/Serien-Streaming-Fluss

```
User spielt VOD/Episode ab
    ↓
Channel.GetChannelItems() mit Media-Item
    ↓
StreamService.GetMediaSourceInfo(StreamType, streamId, extension)
    ↓
Stream-URL generieren:
  - VOD: {baseUrl}/movie/{username}/{password}/{streamId}.{ext}
  - Series: {baseUrl}/series/{username}/{password}/{episodeId}.{ext}
    ↓
MediaSourceInfo mit Path = stream-URL zurückgeben
    ↓
Jellyfin streamt URL direkt (kein Restream-Buffering)
    ↓
User-Client empfängt Stream
```

### Direct-Stream-Fluss (Live-TV)

```
User spielt Live-Kanal ab
    ↓
LiveTvService.GetChannelStreamWithDirectStreamProvider(channelId)
    ↓
Vorhandene Streams prüfen (Reuse falls verfügbar)
    ↓ (Neuer Stream benötigt)
StreamService.GetMediaSourceInfo(StreamType.Live, channelId, restream: true)
    ↓
Stream-URL generieren: {baseUrl}/{username}/{password}/{streamId}
    ↓
Restream-Instanz erstellen
    ↓
Restream.Open() - Startet HTTP-Stream-Pufferung
    ↓
ConsumerCount erhöhen
    ↓
ILiveStream an Jellyfin zurückgeben
    ↓
Jellyfin transcodiert/remuxed Stream zum Client
    ↓
User-Client empfängt Stream
```

---

## Plugin-Lebenszyklus

### Initialisierung

```
Jellyfin-Server startet
    ↓
PluginServiceRegistrator.RegisterServices()
    ↓
Services als Singletons registrieren:
  - LiveTvService
  - VodChannel
  - SeriesChannel
  - CatchupChannel
  - XtreamVodProvider
  - ConnectionManager
  - CacheService
  - ChannelListService
  - ThumbnailCacheService
    ↓
Plugin-Constructor aufgerufen
    ↓
StreamService & TaskService initialisieren
    ↓
Plugin.Instance wird verfügbar
    ↓
Jellyfin scannt nach Channels & Live-TV
```

### Konfigurations-Update

```
User speichert Config in Admin-UI
    ↓
Plugin.UpdateConfiguration(newConfig)
    ↓
base.UpdateConfiguration(newConfig) - In XML speichern
    ↓
TaskService.CancelIfRunningAndQueue(
  "Jellyfin.LiveTv.Guide.RefreshGuideScheduledTask")
    ↓
TaskService.CancelIfRunningAndQueue(
  "Jellyfin.LiveTv.Channels.RefreshChannelsScheduledTask")
    ↓
Jellyfin aktualisiert alle Channels & EPG-Daten
    ↓
UI zeigt neue Kanäle/EPG
```

---

## Sicherheitsüberlegungen

### Bekanntes Sicherheitsproblem: Credential-Exposure

**Problem**: Jellyfin veröffentlicht Remote-Stream-Pfade in seiner API und Standard-UI. Da Xtream-Format Credentials in URLs einschließt, kann jeder mit Bibliothekszugriff die Credentials sehen.

**URL-Format-Beispiele**:
```
Live:    {baseUrl}/{username}/{password}/{streamId}
VOD:     {baseUrl}/movie/{username}/{password}/{streamId}.{ext}
Series:  {baseUrl}/series/{username}/{password}/{episodeId}.{ext}
Catchup: {baseUrl}/streaming/timeshift.php?username={user}&password={pass}&stream={id}&...
```

**Impact**: Users mit Zugriff auf die Jellyfin-Bibliothek können API-Credentials extrahieren.

**Aktuelle Mitigation**:
- In README.md unter "Known problems / Loss of confidentiality" dokumentiert
- Empfehlung: Plugin nur auf vertrauenswürdigen/privaten Servern verwenden
- Erwägen Sie API-Credentials mit limitierten Berechtigungen falls Provider unterstützt

**Geplante Lösung** (siehe tasks.md TASK-002):
- Proxy-System implementieren
- Token-basierte Stream-URLs statt Credentials
- Tokens mit 24h-Expiration
- Redirect-Logic zu echten URLs

---

## Performance & Caching

### Caching-Strategie (3-Tier-System)

| Cache-Typ | Location | TTL | Zweck | Config |
|-----------|----------|-----|-------|--------|
| **EPG-Cache** | Memory (IMemoryCache) | 10min | EPG-API-Calls reduzieren | Hardcoded |
| **Extended-Cache** | CacheService | Config | General-API-Caching | `EnableExtendedCache` |
| **Thumbnail-Cache** | Disk | 30d | Image-Caching | `EnableThumbnailCache` |

### Connection-Management

**Connection-Queue** (optional):
- Verhindert gleichzeitige API-Calls
- Tracking von Request-Statistics
- Konfigurierbar via `EnableConnectionQueue`

**Vorteile**:
- Verhindert Provider-Rate-Limiting
- Reduziert Server-Last
- Verbessert Stabilität mit langsamen Providern

### Memory-Überlegungen

**EPG-Daten**:
- Cached per-channel für 10min
- Large-Channel-Lineups mit häufigen EPG-Requests können signifikanten Memory verbrauchen
- Erwägung: Adaptive TTL basierend auf Update-Frequency

**Thumbnails**:
- Auf Disk gespeichert, nicht in Memory
- Cache-Size wächst basierend auf Anzahl Channels/Content
- Automatisches Cleanup via Retention-Policy (geplant: TASK-007)

---

## Build & Deployment

### Build-Commands

```bash
# Debug-Build
dotnet build Jellyfin.Xtream.sln

# Release-Build
dotnet build Jellyfin.Xtream.sln -c Release

# Clean
dotnet clean Jellyfin.Xtream.sln

# Restore
dotnet restore Jellyfin.Xtream.sln
```

### Output

**Assembly**: `CandyTv.dll`
**Location**: `bin/Release/net8.0/CandyTv.dll`

### Unraid-Installation

#### 1. Plugin-Verzeichnis

```bash
# Standard Jellyfin Unraid Plugin-Pfad
/mnt/user/appdata/jellyfin/plugins/CandyTv_0.0.2/
```

#### 2. Installation

```bash
# Plugin-Ordner erstellen
mkdir -p /mnt/user/appdata/jellyfin/plugins/CandyTv_0.0.2

# DLL kopieren
cp bin/Release/net8.0/CandyTv.dll /mnt/user/appdata/jellyfin/plugins/CandyTv_0.0.2/

# Permissions setzen
chown -R 99:100 /mnt/user/appdata/jellyfin/plugins/
chmod -R 755 /mnt/user/appdata/jellyfin/plugins/

# Container neustarten
docker restart jellyfin
```

#### 3. Konfiguration

```
Jellyfin Admin Dashboard
  → Plugins
  → CandyTv
  → Zugangsdaten konfigurieren
```

---

## Dependencies

### Runtime-Dependencies

| Package | Version | Zweck |
|---------|---------|-------|
| **Jellyfin.Controller** | 10.10.7 | Jellyfin-Server-Integration |
| **Jellyfin.Model** | 10.10.7 | Jellyfin-Datenmodelle |
| **Newtonsoft.Json** | 13.0.3 | JSON-Serialisierung |
| **FuzzySharp** | 2.0.2 | Fuzzy-String-Matching |
| **Microsoft.AspNetCore.App** | (Framework) | ASP.NET Core APIs |

### Analyzer-Dependencies (Development)

| Package | Version |
|---------|---------|
| SerilogAnalyzer | 0.15.0 |
| StyleCop.Analyzers | 1.2.0-beta.556 |
| SmartAnalyzers.MultithreadingAnalyzer | 1.1.31 |

---

## Code-Qualität & Standards

### Projekt-Settings

```xml
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
<Nullable>enable</Nullable>
<AnalysisMode>AllEnabledByDefault</AnalysisMode>
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

### Analyzer-Config

- **StyleCop.Analyzers**: C#-Style-Enforcement
- **SerilogAnalyzer**: Logging-Best-Practices
- **MultithreadingAnalyzer**: Threading-Safety
- **.editorconfig**: Formatting-Rules
- **jellyfin.ruleset**: Custom-Analysis-Rules

**Alle public APIs müssen XML-Documentation haben!**

---

## Fehlerbehebung

### Häufige Probleme

#### Kanäle erscheinen nicht
1. Credentials in Admin-UI prüfen
2. Kategorien-Auswahl prüfen
3. Jellyfin-Logs checken
4. API-Test: `/Xtream/UserInfo`

#### Streams spielen nicht
1. `Published server URIs` konfigurieren (Jellyfin Networking)
2. Stream-URLs in Logs prüfen
3. Direkt in VLC testen
4. Transcoding-Logs prüfen

#### Fehlende EPG-Daten
1. Manual-Refresh: Dashboard → Live TV → Refresh
2. Provider-EPG-Verfügbarkeit prüfen
3. Logs auf API-Errors prüfen

#### Langsame Performance
1. `EnableConnectionQueue` aktivieren
2. `EnableExtendedCache` aktivieren
3. `EnableEpgPreload` aktivieren
4. Kategorien/Kanäle reduzieren
5. Stats monitoren: `/Xtream/OptimizationStats`

---

## Unraid-Spezifika

### Docker-Commands

```bash
# Status
docker ps | grep jellyfin

# Logs
docker logs jellyfin

# Shell
docker exec -it jellyfin bash

# Restart
docker restart jellyfin
```

### Backup-Verzeichnisse

```bash
/mnt/user/appdata/jellyfin/plugins/      # Plugins
/mnt/user/appdata/jellyfin/config/       # Configs
/mnt/user/appdata/jellyfin/data/         # Database
```

### Network-Config

- **Network Type**: Bridge oder Custom (br0 für dedizierte IP)
- **Ports**: 8096:8096 (HTTP), 8920:8920 (HTTPS)

### Permissions

**Standard Unraid**:
- **PUID**: 99 (nobody)
- **PGID**: 100 (users)

---

## Xtream-API-Referenz

### Base-URL

```
https://example.com/player_api.php?username={user}&password={pass}&action={action}
```

### Actions

| Action | Parameter | Returns |
|--------|-----------|---------|
| (none) | - | User/Server-Info |
| `get_live_streams` | `category_id` (opt) | Live-Channels |
| `get_live_categories` | - | Live-Categories |
| `get_vod_streams` | `category_id` | VOD-Streams |
| `get_vod_info` | `vod_id` | VOD-Metadata |
| `get_vod_categories` | - | VOD-Categories |
| `get_series` | `category_id` | Series-List |
| `get_series_info` | `series_id` | Series-Details |
| `get_series_categories` | - | Series-Categories |
| `get_simple_data_table` | `stream_id` | EPG-Data |

### Stream-URLs

```
Live:    {baseUrl}/{user}/{pass}/{streamId}.{ext}
VOD:     {baseUrl}/movie/{user}/{pass}/{streamId}.{ext}
Series:  {baseUrl}/series/{user}/{pass}/{episodeId}.{ext}
Catchup: {baseUrl}/streaming/timeshift.php?username={user}&password={pass}&stream={id}&start={YYYY-MM-DD:HH-mm}&duration={min}
```

---

## Datei-Struktur

```
Jellyfin.Xtream/
├── Api/
│   ├── Models/
│   │   ├── ChannelMatchResponse.cs
│   │   ├── ChannelResponse.cs
│   │   ├── CategoryResponse.cs
│   │   ├── ItemResponse.cs
│   │   ├── MatchChannelRequest.cs
│   │   └── ParseChannelListRequest.cs
│   └── XtreamController.cs
├── Client/
│   ├── Models/
│   │   ├── AudioInfo.cs
│   │   ├── Category.cs
│   │   ├── Episode.cs
│   │   ├── EpisodeInfo.cs
│   │   ├── EpgInfo.cs
│   │   ├── EpgListings.cs
│   │   ├── PlayerApi.cs
│   │   ├── Season.cs
│   │   ├── Series.cs
│   │   ├── SeriesInfo.cs
│   │   ├── SeriesStreamInfo.cs
│   │   ├── ServerInfo.cs
│   │   ├── StreamInfo.cs
│   │   ├── UserInfo.cs
│   │   ├── VideoInfo.cs
│   │   ├── VodInfo.cs
│   │   └── VodStreamInfo.cs
│   ├── Base64Converter.cs
│   ├── ConnectionInfo.cs
│   ├── IntToBoolConverter.cs
│   ├── OnlyObjectConverter.cs
│   ├── SingularToListConverter.cs
│   ├── UnixTimestampConverter.cs
│   └── XtreamClient.cs
├── Configuration/
│   ├── Web/
│   │   ├── Xtream.css
│   │   ├── Xtream.js
│   │   ├── XtreamChannelLists.html
│   │   ├── XtreamChannelLists.js
│   │   ├── XtreamCredentials.html
│   │   ├── XtreamCredentials.js
│   │   ├── XtreamLive.html
│   │   ├── XtreamLive.js
│   │   ├── XtreamLiveOverrides.html
│   │   ├── XtreamLiveOverrides.js
│   │   ├── XtreamSeries.html
│   │   ├── XtreamSeries.js
│   │   ├── XtreamVod.html
│   │   └── XtreamVod.js
│   ├── ChannelList.cs
│   ├── ChannelMapping.cs
│   ├── ChannelOverrides.cs
│   ├── PluginConfiguration.cs
│   └── SerializableDictionary.cs
├── Providers/
│   └── XtreamVodProvider.cs
├── Service/
│   ├── CacheService.cs
│   ├── ChannelListService.cs
│   ├── ConnectionManager.cs
│   ├── MatchResult.cs
│   ├── ParsedName.cs
│   ├── Restream.cs
│   ├── StreamService.cs
│   ├── StreamType.cs
│   ├── TaskService.cs
│   ├── ThumbnailCacheService.cs
│   ├── WrappedBufferReadStream.cs
│   └── WrappedBufferStream.cs
├── CatchupChannel.cs
├── LiveTvService.cs
├── Plugin.cs
├── PluginServiceRegistrator.cs
├── SeriesChannel.cs
├── VodChannel.cs
├── Jellyfin.Xtream.csproj
├── .editorconfig
├── jellyfin.ruleset
├── spec.md (dieses Dokument)
├── plan.md
├── tasks.md
└── ai-behavior.md
```

---

## Referenzen

### Externe Dokumentation

- [Xtream API Documentation](https://xtream-ui.org/api-xtreamui-xtreamcode/)
- [Jellyfin Plugin Development](https://jellyfin.org/docs/general/server/plugins/)
- [Jellyfin Live TV](https://jellyfin.org/docs/general/server/live-tv/)

### Repository

- **GitHub**: https://github.com/andreaspointecker-source/Jellyfin-Plugin
- **Issues**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/issues

---

**Spezifikations-Version**: 1.0
**Datum**: 2025-01-13
**Basierend auf**: CandyTv v0.0.2
**Root**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream`
**Autor**: Generiert aus Source-Code-Analyse

---

**Ende der Spezifikation**
