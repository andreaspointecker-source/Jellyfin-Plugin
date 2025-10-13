# CandyTv (Jellyfin.Xtream) - Entwicklungsplan

## Projektinformationen

- **Root-Verzeichnis**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream`
- **Version**: 0.0.2
- **Ziel**: Professionelles Jellyfin IPTV-Plugin für Unraid
- **Framework**: .NET 8.0
- **Jellyfin ABI**: 10.10.7.0

---

## Aktuelle Architektur

### Komponenten-Übersicht

```
1. Plugin-Layer (Plugin.cs)
   └─ Lifecycle, Configuration, Service-Init

2. Channel-Layer
   ├─ LiveTvService.cs (Live-TV + EPG)
   ├─ VodChannel.cs (Video-On-Demand)
   ├─ SeriesChannel.cs (TV-Serien)
   └─ CatchupChannel.cs (Aufzeichnungen)

3. Service-Layer
   ├─ StreamService.cs (Core-Logic, GUID-System)
   ├─ ConnectionManager.cs (API-Queue)
   ├─ CacheService.cs (Extended-Cache)
   ├─ ThumbnailCacheService.cs (Image-Cache)
   ├─ ChannelListService.cs (Custom-Lists)
   └─ TaskService.cs (Scheduled-Tasks)

4. Client-Layer
   ├─ XtreamClient.cs (API-Client)
   ├─ ConnectionInfo.cs (Credentials)
   └─ JSON-Converters (Base64, IntToBool, etc.)

5. API-Layer
   └─ XtreamController.cs (REST-API für Admin-UI)

6. Web-UI-Layer
   └─ Configuration/Web/ (HTML/JS/CSS)
```

---

## Entwicklungsphasen

### Phase 1: Stabilisierung & Bugfixes ✅ (Aktuell)

**Status**: In Progress (v0.0.2)

**Ziele**:
- [x] Core-Funktionalität implementiert
- [x] Live-TV mit EPG funktionsfähig
- [x] VOD-Kanal funktionsfähig
- [x] Serien-Kanal funktionsfähig
- [x] Catchup-Kanal funktionsfähig
- [x] Custom-Channel-Lists implementiert
- [x] Thumbnail-Caching implementiert
- [x] Connection-Queue implementiert
- [ ] Performance-Optimierungen
- [ ] Memory-Leak-Fixes
- [ ] Error-Handling verbessern

**Bekannte Issues**:
1. Credential-Exposure in Stream-URLs
2. EPG-Cache könnte optimiert werden
3. Thumbnail-Cache-Cleanup fehlt (Retention-Policy)

---

### Phase 2: Performance-Optimierung 🔄 (Geplant)

**Ziele**:
- [ ] EPG-Preload-Mechanismus optimieren
- [ ] Async-Operations verbessern
- [ ] Memory-Cache-Strategien überarbeiten
- [ ] Database-Caching für Metadaten
- [ ] Connection-Pool für XtreamClient
- [ ] Lazy-Loading für große Channel-Listen
- [ ] Background-Tasks für Cache-Warmup

**Metriken**:
- EPG-Load-Time: < 2s für 100 Kanäle
- Channel-Load-Time: < 1s für 500 Kanäle
- Memory-Usage: < 200 MB idle
- Cache-Hit-Rate: > 85%

---

### Phase 3: Feature-Erweiterungen 📋 (Geplant)

#### 3.1 Erweiterte Channel-Verwaltung

- [ ] **Channel-Gruppen**: Benutzerdefinierte Gruppierung
- [ ] **Favoriten-System**: User-spezifische Favoriten
- [ ] **Channel-Hiding**: Kanäle ausblenden statt löschen
- [ ] **Multi-Profile**: Verschiedene Configs pro User
- [ ] **Channel-Import/Export**: M3U/XML-Support
- [ ] **Auto-Sorting**: Intelligente Kanal-Sortierung

#### 3.2 EPG-Verbesserungen

- [ ] **EPG-Images**: Program-Cover in Catchup
- [ ] **Extended-EPG**: Genre, Cast, Ratings
- [ ] **EPG-Search**: Volltextsuche in EPG
- [ ] **Recording-Schedule**: Jellyfin-Recording-Integration
- [ ] **EPG-Notifications**: Benachrichtigungen bei Programm-Start
- [ ] **Multi-EPG-Sources**: Fallback-EPG-Quellen

#### 3.3 Streaming-Features

- [ ] **Quality-Selection**: Automatische Qualitäts-Auswahl
- [ ] **Adaptive-Streaming**: HLS/DASH-Support
- [ ] **Stream-Fallback**: Alternative Streams bei Ausfall
- [ ] **Buffer-Management**: Intelligentes Pre-Buffering
- [ ] **Network-Optimization**: Bandwidth-Detection
- [ ] **Subtitle-Support**: Externes Subtitle-Loading

#### 3.4 Admin-Features

- [ ] **Dashboard**: Erweiterte Statistiken & Monitoring
- [ ] **Health-Check**: Provider-Status-Monitoring
- [ ] **Log-Viewer**: Web-basierter Log-Viewer
- [ ] **Config-Backup**: Automatisches Backup/Restore
- [ ] **Migration-Tool**: Update-Assistant
- [ ] **Test-Suite**: Provider-Connection-Testing

---

### Phase 4: Unraid-Optimierung 🎯 (Geplant)

#### 4.1 Docker-Integration

- [ ] **Docker-Compose**: Optimiertes Compose-File
- [ ] **Environment-Variables**: Config via ENV
- [ ] **Volume-Management**: Best-Practice-Mappings
- [ ] **Network-Modes**: Bridge/Host/Custom-Optimization
- [ ] **Resource-Limits**: CPU/Memory-Constraints
- [ ] **Health-Checks**: Docker-Health-Monitoring

#### 4.2 Unraid-App-Template

```xml
<?xml version="1.0"?>
<Container version="2">
  <Name>Jellyfin-CandyTv</Name>
  <Repository>jellyfin/jellyfin:latest</Repository>
  <Registry>https://hub.docker.com/r/jellyfin/jellyfin/</Registry>
  <Network>bridge</Network>
  <Privileged>false</Privileged>
  <Support>https://github.com/Kevinjil/Jellyfin.Xtream</Support>
  <Project>https://github.com/Kevinjil/Jellyfin.Xtream</Project>
  <Overview>Jellyfin mit CandyTv IPTV-Plugin</Overview>
  <Category>MediaServer:Video</Category>
  <WebUI>http://[IP]:[PORT:8096]</WebUI>
  <Icon>https://raw.githubusercontent.com/Kevinjil/Jellyfin.Xtream/master/icon.png</Icon>

  <Config Name="WebUI" Target="8096" Default="8096" Mode="tcp" Description="Web-UI Port" Type="Port" Display="always" Required="true" Mask="false"/>
  <Config Name="AppData" Target="/config" Default="/mnt/user/appdata/jellyfin" Mode="rw" Description="Config Directory" Type="Path" Display="advanced" Required="true" Mask="false"/>
  <Config Name="Media" Target="/media" Default="/mnt/user/media" Mode="rw" Description="Media Directory" Type="Path" Display="advanced" Required="false" Mask="false"/>
  <Config Name="PUID" Target="PUID" Default="99" Mode="" Description="User ID" Type="Variable" Display="advanced" Required="false" Mask="false"/>
  <Config Name="PGID" Target="PGID" Default="100" Mode="" Description="Group ID" Type="Variable" Display="advanced" Required="false" Mask="false"/>
</Container>
```

#### 4.3 Unraid-Spezifische Features

- [ ] **Community-App**: Veröffentlichung im Unraid-App-Store
- [ ] **Auto-Update**: Plugin-Update-Mechanismus
- [ ] **Backup-Integration**: Unraid-Backup-Plugin-Support
- [ ] **Notification-Integration**: Unraid-Notifications
- [ ] **Dashboard-Widget**: Unraid-Dashboard-Integration
- [ ] **Resource-Monitoring**: Unraid-Resource-Stats

---

### Phase 5: Sicherheit & Compliance 🔒 (Geplant)

#### 5.1 Sicherheitsverbesserungen

- [ ] **Credential-Encryption**: Verschlüsselte Speicherung
- [ ] **API-Key-Support**: Alternative zu Username/Password
- [ ] **Token-Based-Auth**: OAuth2/JWT-Support
- [ ] **Rate-Limiting**: API-Rate-Limiting
- [ ] **IP-Whitelist**: Zugriffsbeschränkung
- [ ] **Audit-Logging**: Security-Event-Logging

#### 5.2 Credential-Exposure-Mitigation

**Problem**: Stream-URLs enthalten Credentials

**Lösungsansätze**:

```csharp
// Option 1: Proxy-Endpoint (Empfohlen)
// Stream-URL: https://jellyfin.local/Xtream/Stream/{token}
// Token enthält verschlüsselte Stream-Info
// Plugin leitet zu echtem Stream weiter mit Credentials

public async Task<IActionResult> ProxyStream(string token)
{
    var streamInfo = DecryptToken(token);
    var realUrl = BuildXtreamUrl(streamInfo, _creds);
    return Redirect(realUrl);
}

// Option 2: Session-Based-Credentials
// Credentials nur während Jellyfin-Session gültig
// Nach Logout ungültig machen

// Option 3: Time-Limited-URLs
// URLs mit Ablaufzeit generieren
// Provider-Support erforderlich
```

**Implementierungs-Priorität**: HIGH

---

### Phase 6: Testing & Dokumentation 📚 (Ongoing)

#### 6.1 Test-Coverage

- [ ] **Unit-Tests**: Core-Logic-Coverage > 80%
- [ ] **Integration-Tests**: API-Client-Tests
- [ ] **E2E-Tests**: Full-Workflow-Tests
- [ ] **Performance-Tests**: Load/Stress-Testing
- [ ] **Compatibility-Tests**: Verschiedene Xtream-Provider

#### 6.2 Dokumentation

- [x] **spec.md**: Technische Spezifikation
- [x] **plan.md**: Entwicklungsplan (dieses Dokument)
- [ ] **tasks.md**: Task-Liste mit Prioritäten
- [ ] **ai-behavior.md**: AI-Assistant-Richtlinien
- [ ] **API-Docs**: Swagger/OpenAPI-Spec
- [ ] **User-Manual**: Benutzerhandbuch
- [ ] **Admin-Guide**: Administrator-Handbuch
- [ ] **Troubleshooting-Guide**: Fehlerbehandlung
- [ ] **Developer-Guide**: Entwickler-Dokumentation

---

## Technische Schulden

### Kritisch 🔴

1. **Credential-Exposure**: Stream-URLs enthalten Plaintext-Credentials
   - **Impact**: Sicherheitsrisiko
   - **Aufwand**: Hoch (Proxy-System erforderlich)
   - **Priorität**: Sehr hoch

2. **Memory-Leaks**: Potenzielle Leaks bei long-running Sessions
   - **Impact**: Performance
   - **Aufwand**: Mittel
   - **Priorität**: Hoch

3. **Error-Handling**: Unzureichende Exception-Behandlung
   - **Impact**: Stabilität
   - **Aufwand**: Mittel
   - **Priorität**: Hoch

### Wichtig 🟡

4. **Test-Coverage**: Keine automatisierten Tests
   - **Impact**: Wartbarkeit
   - **Aufwand**: Hoch
   - **Priorität**: Mittel

5. **Async-Patterns**: Nicht konsistent async/await
   - **Impact**: Performance
   - **Aufwand**: Mittel
   - **Priorität**: Mittel

6. **Logging**: Unzureichendes strukturiertes Logging
   - **Impact**: Debugging
   - **Aufwand**: Niedrig
   - **Priorität**: Mittel

### Niedrig 🟢

7. **Code-Duplication**: DRY-Violations in Channel-Classes
   - **Impact**: Wartbarkeit
   - **Aufwand**: Niedrig
   - **Priorität**: Niedrig

8. **Magic-Numbers**: Hardcoded-Values (z.B. Cache-TTL)
   - **Impact**: Konfigurierbarkeit
   - **Aufwand**: Niedrig
   - **Priorität**: Niedrig

---

## Roadmap-Timeline

```
Q1 2025: Phase 2 - Performance-Optimierung
├─ Jan: Memory-Optimierung, Async-Patterns
├─ Feb: Cache-Strategien, Connection-Pool
└─ Mar: Background-Tasks, Monitoring

Q2 2025: Phase 3 - Feature-Erweiterungen
├─ Apr: Channel-Management-Features
├─ May: EPG-Verbesserungen
└─ Jun: Streaming-Features

Q3 2025: Phase 4 - Unraid-Optimierung
├─ Jul: Docker-Integration
├─ Aug: Unraid-App-Template
└─ Sep: Community-App-Veröffentlichung

Q4 2025: Phase 5 - Sicherheit & Compliance
├─ Okt: Credential-Encryption
├─ Nov: Proxy-System für Streams
└─ Dez: Security-Audit & Fixes

2026: Phase 6 - Testing & Stabilisierung
├─ Q1: Unit/Integration-Tests
├─ Q2: E2E/Performance-Tests
├─ Q3: Dokumentation-Vervollständigung
└─ Q4: v1.0 Release
```

---

## Prioritäten-Matrix

### Must-Have (v0.1.0)

- [x] Live-TV-Streaming
- [x] EPG-Daten
- [x] VOD-Streaming
- [x] Serien-Streaming
- [ ] Credential-Encryption
- [ ] Error-Handling
- [ ] Performance-Optimierung

### Should-Have (v0.2.0)

- [ ] Proxy-System für Streams
- [ ] Channel-Gruppen
- [ ] EPG-Search
- [ ] Admin-Dashboard
- [ ] Test-Suite
- [ ] Unraid-App-Template

### Could-Have (v0.3.0)

- [ ] Multi-Profile
- [ ] Recording-Support
- [ ] Adaptive-Streaming
- [ ] Health-Monitoring
- [ ] M3U-Import/Export

### Won't-Have (vorerst)

- DVR-Funktionalität (Jellyfin-native)
- Custom-Transcoding (Jellyfin-native)
- Mobile-App (Jellyfin-Apps nutzen)
- Multi-Server-Sync

---

## Entwicklungs-Richtlinien

### Code-Standards

1. **StyleCop-Compliance**: Alle Rules erfüllen
2. **Nullable-Reference-Types**: Immer aktiviert
3. **XML-Documentation**: Alle public APIs
4. **Async-First**: Async/await wo möglich
5. **LINQ-Queries**: Bevorzugt gegenüber Loops
6. **Dependency-Injection**: Constructor-Injection

### Git-Workflow

```
master (stable, production-ready)
  ↑
develop (next release)
  ↑
feature/* (neue Features)
bugfix/* (Bugfixes)
hotfix/* (kritische Fixes für master)
```

### Commit-Messages

```
<type>(<scope>): <subject>

<body>

<footer>

Types: feat, fix, docs, style, refactor, perf, test, chore
Scope: plugin, service, client, api, ui, config, docs
```

Beispiel:
```
feat(service): Add thumbnail cache cleanup mechanism

Implements automatic cleanup of expired thumbnails based on
retention policy. Runs during maintenance window.

Closes #42
```

### Branch-Naming

```
feature/channel-groups
bugfix/epg-cache-leak
hotfix/stream-url-encoding
docs/api-reference
refactor/async-patterns
```

---

## Review-Checkliste

### Code-Review

- [ ] StyleCop-Warnings behoben
- [ ] Unit-Tests geschrieben
- [ ] XML-Documentation vollständig
- [ ] Performance-Impact geprüft
- [ ] Memory-Leaks geprüft
- [ ] Error-Handling implementiert
- [ ] Logging hinzugefügt
- [ ] Backward-Compatibility geprüft

### Security-Review

- [ ] Input-Validation
- [ ] SQL-Injection-Prevention
- [ ] XSS-Prevention
- [ ] CSRF-Protection
- [ ] Credential-Handling
- [ ] Rate-Limiting
- [ ] Audit-Logging

### Performance-Review

- [ ] Algorithmen-Complexity
- [ ] Database-Queries optimiert
- [ ] Cache-Strategien geprüft
- [ ] Memory-Allocation minimiert
- [ ] Async-Operations optimiert
- [ ] Load-Testing durchgeführt

---

## Release-Prozess

### Version-Numbering

```
MAJOR.MINOR.PATCH

MAJOR: Breaking Changes
MINOR: New Features (backward-compatible)
PATCH: Bugfixes (backward-compatible)
```

### Release-Checklist

1. [ ] Alle Tests passing
2. [ ] Changelog aktualisiert
3. [ ] Version-Nummer erhöht
4. [ ] build.yaml aktualisiert
5. [ ] DLL gebaut (Release-Mode)
6. [ ] GitHub-Release erstellt
7. [ ] Release-Notes geschrieben
8. [ ] Community benachrichtigt
9. [ ] Unraid-App-Store aktualisiert (falls verfügbar)

---

## Kontakt & Support

- **Repository**: https://github.com/Kevinjil/Jellyfin.Xtream
- **Issues**: https://github.com/Kevinjil/Jellyfin.Xtream/issues
- **Discussions**: https://github.com/Kevinjil/Jellyfin.Xtream/discussions
- **Original-Maintainer**: Kevin Jilissen

---

**Plan-Version**: 1.0
**Letzte Aktualisierung**: 2025-01-13
**Status**: Work in Progress
**Nächster Review**: Nach Phase 2-Completion
