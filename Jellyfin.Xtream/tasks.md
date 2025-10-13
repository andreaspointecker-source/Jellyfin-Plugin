# CandyTv (Jellyfin.Xtream) - Task-Liste

## Projektinformationen

- **Root**: `C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream`
- **Version**: 0.0.2 → 0.1.0 (geplant)
- **Letzte Aktualisierung**: 2025-01-13

---

## Legende

- 🔴 **Kritisch**: Muss vor v0.1.0 erledigt werden
- 🟡 **Hoch**: Sollte vor v0.1.0 erledigt werden
- 🟢 **Mittel**: Nice-to-have für v0.1.0
- ⚪ **Niedrig**: Kann später erledigt werden
- ✅ **Erledigt**: Task abgeschlossen
- 🔄 **In Progress**: Wird aktuell bearbeitet
- 📋 **Geplant**: Eingeplant für nächsten Sprint
- 💡 **Idee**: Noch nicht eingeplant

**Aufwands-Schätzung**: XS (<2h), S (2-4h), M (4-8h), L (8-16h), XL (>16h)

---

## Phase 1: Stabilisierung & Bugfixes (v0.0.2 → v0.1.0)

### Kritische Sicherheit 🔴

#### TASK-001: Credential-Encryption implementieren
- **Priorität**: 🔴 Kritisch
- **Status**: 📋 Geplant
- **Aufwand**: L (10h)
- **Assignee**: TBD
- **Deadline**: Q1 2025

**Beschreibung**:
Credentials (BaseUrl, Username, Password) in PluginConfiguration verschlüsselt speichern statt Plaintext.

**Teilaufgaben**:
- [ ] Encryption/Decryption-Helper implementieren
- [ ] PluginConfiguration.cs anpassen
- [ ] Migration für bestehende Configs erstellen
- [ ] Unit-Tests schreiben
- [ ] Dokumentation aktualisieren

**Akzeptanzkriterien**:
- Credentials werden AES-256 verschlüsselt
- Migration läuft automatisch bei Plugin-Update
- Rückwärtskompatibilität gewährleistet

**Dateien**:
- `Configuration/PluginConfiguration.cs`
- `Configuration/CredentialEncryption.cs` (neu)
- `Plugin.cs` (Migration-Logic)

---

#### TASK-002: Stream-URL-Proxy implementieren
- **Priorität**: 🔴 Kritisch
- **Status**: 📋 Geplant
- **Aufwand**: XL (20h)
- **Assignee**: TBD
- **Deadline**: Q2 2025

**Beschreibung**:
Proxy-System um Credentials aus Stream-URLs zu entfernen. Jellyfin erhält Token-URLs statt echte Xtream-URLs.

**Teilaufgaben**:
- [ ] Token-Generierungs-System implementieren
- [ ] XtreamController: `/Stream/{token}` Endpoint
- [ ] Token-Encryption & Expiration
- [ ] GetMediaSourceInfo() anpassen (Token-URLs)
- [ ] Redirect-Logic zu echten URLs
- [ ] Load-Testing & Performance-Optimierung
- [ ] Dokumentation

**Akzeptanzkriterien**:
- Stream-URLs enthalten keine Credentials
- Tokens haben 24h-Expiration
- < 50ms Overhead durch Proxy
- Backward-Compatibility mit alten URLs

**Dateien**:
- `Api/XtreamController.cs`
- `Service/StreamTokenService.cs` (neu)
- `Service/StreamService.cs`

---

### Kritische Stabilität 🔴

#### TASK-003: Memory-Leak-Analyse & Fixes
- **Priorität**: 🔴 Kritisch
- **Status**: 📋 Geplant
- **Aufwand**: M (6h)
- **Assignee**: TBD
- **Deadline**: Q1 2025

**Beschreibung**:
Potenzielle Memory-Leaks in long-running Sessions identifizieren und beheben.

**Teilaufgaben**:
- [ ] Memory-Profiling mit dotMemory
- [ ] HttpClient-Lebenszyklus prüfen
- [ ] IDisposable-Pattern überall korrekt
- [ ] Memory-Cache-Eviction testen
- [ ] Restream-Cleanup überprüfen
- [ ] Load-Testing (24h+)

**Akzeptanzkriterien**:
- Kein Memory-Growth nach 24h Betrieb
- Alle IDisposable korrekt disposed
- Memory < 200 MB idle

**Dateien**:
- `Client/XtreamClient.cs`
- `Service/Restream.cs`
- `Service/ThumbnailCacheService.cs`
- `LiveTvService.cs`

---

#### TASK-004: Error-Handling verbessern
- **Priorität**: 🔴 Kritisch
- **Status**: 📋 Geplant
- **Aufwand**: M (8h)
- **Assignee**: TBD
- **Deadline**: Q1 2025

**Beschreibung**:
Konsistentes Exception-Handling mit aussagekräftigen Fehlermeldungen.

**Teilaufgaben**:
- [ ] Custom Exception-Types definieren
- [ ] Try-Catch-Blocks ergänzen
- [ ] Logging strukturieren (Serilog)
- [ ] User-Friendly Error-Messages
- [ ] API-Controller Error-Responses
- [ ] Retry-Logic für transiente Fehler

**Akzeptanzkriterien**:
- Keine unhandled Exceptions
- Alle Fehler geloggt mit Context
- User-UI zeigt verständliche Fehler

**Dateien**:
- `Exceptions/` (neu)
- Alle Controller/Service-Files

---

### Performance 🟡

#### TASK-005: Async-Patterns optimieren
- **Priorität**: 🟡 Hoch
- **Status**: 📋 Geplant
- **Aufwand**: M (6h)
- **Assignee**: TBD
- **Deadline**: Q1 2025

**Beschreibung**:
Inkonsistente async/await-Patterns korrigieren und optimieren.

**Teilaufgaben**:
- [ ] ConfigureAwait(false) wo sinnvoll
- [ ] Task.WhenAll für parallele Ops
- [ ] ValueTask wo passend
- [ ] CancellationToken durchgängig
- [ ] Async-Void vermeiden
- [ ] Performance-Benchmarks

**Akzeptanzkriterien**:
- Keine async-Void Methods
- CancellationToken überall übergeben
- Parallel-Ops nutzen Task.WhenAll

**Dateien**:
- Alle Service/Channel-Files

---

#### TASK-006: EPG-Caching optimieren
- **Priorität**: 🟡 Hoch
- **Status**: 📋 Geplant
- **Aufwand**: M (5h)
- **Assignee**: TBD
- **Deadline**: Q1 2025

**Beschreibung**:
EPG-Cache-Strategie überarbeiten für bessere Performance.

**Teilaufgaben**:
- [ ] Cache-Hit-Rate analysieren
- [ ] Adaptive TTL basierend auf Update-Frequency
- [ ] Prefetch für nächste 12h
- [ ] Compression für große EPG-Daten
- [ ] LRU-Eviction implementieren
- [ ] Monitoring-Metriken

**Akzeptanzkriterien**:
- Cache-Hit-Rate > 85%
- EPG-Load < 2s für 100 Kanäle
- Memory-Usage < 100 MB für EPG

**Dateien**:
- `LiveTvService.cs:157` (GetProgramsAsync)
- `Service/CacheService.cs`

---

#### TASK-007: Thumbnail-Cache-Cleanup
- **Priorität**: 🟡 Hoch
- **Status**: 📋 Geplant
- **Aufwand**: S (3h)
- **Assignee**: TBD
- **Deadline**: Q1 2025

**Beschreibung**:
Automatische Bereinigung alter Thumbnails basierend auf Retention-Policy.

**Teilaufgaben**:
- [ ] Scheduled-Task für Cleanup
- [ ] LRU-Deletion-Logic
- [ ] Retention-Policy respektieren
- [ ] Disk-Space-Monitoring
- [ ] Admin-UI: Manual-Cleanup
- [ ] Logging

**Akzeptanzkriterien**:
- Cleanup läuft täglich im Maintenance-Window
- Respektiert ThumbnailCacheRetentionDays
- Admin kann manuell cleanen

**Dateien**:
- `Service/ThumbnailCacheService.cs`
- `Service/TaskService.cs`

---

### Code-Qualität 🟢

#### TASK-008: Unit-Tests schreiben
- **Priorität**: 🟢 Mittel
- **Status**: 💡 Idee
- **Aufwand**: XL (24h)
- **Assignee**: TBD
- **Deadline**: Q2 2025

**Beschreibung**:
Umfassende Unit-Tests für Core-Services.

**Teilaufgaben**:
- [ ] xUnit-Projekt aufsetzen
- [ ] StreamService-Tests (GUID-Encoding, Parsing)
- [ ] XtreamClient-Tests (Mock-API)
- [ ] CacheService-Tests
- [ ] ConnectionManager-Tests
- [ ] CI/CD-Integration
- [ ] Coverage > 80%

**Akzeptanzkriterien**:
- Test-Coverage > 80%
- Alle Tests passing
- CI/CD run on PR

**Dateien**:
- `Jellyfin.Xtream.Tests/` (neu)

---

#### TASK-009: Code-Dokumentation vervollständigen
- **Priorität**: 🟢 Mittel
- **Status**: 💡 Idee
- **Aufwand**: M (6h)
- **Assignee**: TBD
- **Deadline**: Q2 2025

**Beschreibung**:
XML-Documentation für alle public APIs.

**Teilaufgaben**:
- [ ] Missing XML-Comments identifizieren
- [ ] API-Docs schreiben
- [ ] Remarks/Examples ergänzen
- [ ] StyleCop-Warnings fixen
- [ ] Swagger/OpenAPI generieren

**Akzeptanzkriterien**:
- Keine StyleCop-Warnings
- Alle public APIs dokumentiert
- API-Docs generiert

---

#### TASK-010: Code-Duplication beseitigen
- **Priorität**: ⚪ Niedrig
- **Status**: 💡 Idee
- **Aufwand**: S (4h)
- **Assignee**: TBD
- **Deadline**: Q3 2025

**Beschreibung**:
DRY-Violations in Channel-Classes refactoren.

**Teilaufgaben**:
- [ ] Base-Channel-Class erstellen
- [ ] Gemeinsame Logik extrahieren
- [ ] ChannelItemInfo-Creation vereinheitlichen
- [ ] Tests anpassen

**Dateien**:
- `VodChannel.cs`
- `SeriesChannel.cs`
- `CatchupChannel.cs`
- `Channels/BaseChannel.cs` (neu)

---

## Phase 2: Feature-Erweiterungen (v0.2.0)

### Channel-Management 🟡

#### TASK-011: Channel-Gruppen implementieren
- **Priorität**: 🟡 Hoch
- **Status**: 💡 Idee
- **Aufwand**: L (12h)
- **Assignee**: TBD
- **Deadline**: Q2 2025

**Beschreibung**:
Benutzerdefinierte Channel-Gruppierung für bessere Organisation.

**Teilaufgaben**:
- [ ] ChannelGroup-Model
- [ ] UI für Gruppen-Verwaltung
- [ ] StreamService: Group-Filtering
- [ ] Config-Schema-Update
- [ ] Migration
- [ ] Dokumentation

**Dateien**:
- `Configuration/ChannelGroup.cs` (neu)
- `Configuration/Web/XtreamChannelGroups.html` (neu)
- `Service/StreamService.cs`

---

#### TASK-012: Favoriten-System
- **Priorität**: 🟡 Hoch
- **Status**: 💡 Idee
- **Aufwand**: L (10h)
- **Assignee**: TBD
- **Deadline**: Q2 2025

**Beschreibung**:
User-spezifische Favoriten-Kanäle.

**Teilaufgaben**:
- [ ] User-Favorites-Storage (DB oder Config)
- [ ] API-Endpoints für Favorites
- [ ] UI-Integration
- [ ] Filter-Logic
- [ ] Per-User-Config

---

### EPG-Features 🟢

#### TASK-013: EPG-Images unterstützen
- **Priorität**: 🟢 Mittel
- **Status**: 💡 Idee
- **Aufwand**: M (6h)
- **Assignee**: TBD
- **Deadline**: Q2 2025

**Beschreibung**:
Program-Cover-Images in Catchup-Channel anzeigen.

**Teilaufgaben**:
- [ ] EpgInfo: Image-URL-Property
- [ ] ThumbnailCache für EPG-Images
- [ ] CatchupChannel: Image-Mapping
- [ ] UI-Anpassungen

**Dateien**:
- `Client/Models/EpgInfo.cs`
- `CatchupChannel.cs`

---

#### TASK-014: EPG-Search implementieren
- **Priorität**: 🟢 Mittel
- **Status**: 💡 Idee
- **Aufwand**: L (10h)
- **Assignee**: TBD
- **Deadline**: Q2 2025

**Beschreibung**:
Volltextsuche in EPG-Daten.

**Teilaufgaben**:
- [ ] Search-Index aufbauen
- [ ] API-Endpoint `/Xtream/Search/Epg`
- [ ] Full-Text-Search-Logic
- [ ] UI-Integration
- [ ] Performance-Optimierung

---

### Streaming-Features 🟢

#### TASK-015: Adaptive-Streaming-Support
- **Priorität**: 🟢 Mittel
- **Status**: 💡 Idee
- **Aufwand**: XL (20h)
- **Assignee**: TBD
- **Deadline**: Q3 2025

**Beschreibung**:
HLS/DASH-Support für adaptive Bitrate.

**Teilaufgaben**:
- [ ] HLS-Manifest-Parsing
- [ ] Bandwidth-Detection
- [ ] Quality-Selection-Logic
- [ ] Fallback-Mechanismen
- [ ] Testing mit verschiedenen Streams

---

## Phase 3: Unraid-Optimierung (v0.3.0)

### Docker-Integration 🟡

#### TASK-016: Unraid-App-Template erstellen
- **Priorität**: 🟡 Hoch
- **Status**: 💡 Idee
- **Aufwand**: M (6h)
- **Assignee**: TBD
- **Deadline**: Q3 2025

**Beschreibung**:
Community-App-Template für Unraid-App-Store.

**Teilaufgaben**:
- [ ] XML-Template erstellen
- [ ] Icon/Logo design
- [ ] Dokumentation
- [ ] Community-App PR

**Deliverables**:
- `unraid/candytv-template.xml` (neu)
- `unraid/icon.png` (neu)
- `unraid/README.md` (neu)

---

#### TASK-017: Docker-Compose optimieren
- **Priorität**: 🟢 Mittel
- **Status**: 💡 Idee
- **Aufwand**: S (3h)
- **Assignee**: TBD
- **Deadline**: Q3 2025

**Beschreibung**:
Optimiertes Docker-Compose-File für Unraid.

**Teilaufgaben**:
- [ ] docker-compose.yml erstellen
- [ ] Environment-Variables definieren
- [ ] Volume-Mappings optimieren
- [ ] Health-Checks konfigurieren
- [ ] Dokumentation

**Deliverables**:
- `docker-compose.yml` (neu)
- `docker/README.md` (neu)

---

## Phase 4: Monitoring & Admin-Tools (v0.4.0)

### Admin-Dashboard 🟡

#### TASK-018: Enhanced-Admin-Dashboard
- **Priorität**: 🟡 Hoch
- **Status**: 💡 Idee
- **Aufwand**: XL (16h)
- **Assignee**: TBD
- **Deadline**: Q4 2025

**Beschreibung**:
Erweitertes Admin-Dashboard mit detaillierten Statistiken.

**Teilaufgaben**:
- [ ] Dashboard-Design
- [ ] Real-Time-Statistiken
- [ ] Charts/Graphs (Chart.js)
- [ ] Health-Status-Anzeige
- [ ] Performance-Metriken
- [ ] Provider-Connection-Status

**Dateien**:
- `Configuration/Web/XtreamDashboard.html` (neu)
- `Configuration/Web/XtreamDashboard.js` (neu)
- `Api/XtreamController.cs` (Dashboard-Endpoints)

---

#### TASK-019: Log-Viewer implementieren
- **Priorität**: 🟢 Mittel
- **Status**: 💡 Idee
- **Aufwand**: M (8h)
- **Assignee**: TBD
- **Deadline**: Q4 2025

**Beschreibung**:
Web-basierter Log-Viewer im Admin-UI.

**Teilaufgaben**:
- [ ] Log-File-Reading-API
- [ ] UI mit Filtering/Search
- [ ] Real-Time-Log-Streaming (SignalR)
- [ ] Log-Level-Filtering
- [ ] Export-Funktion

---

## Laufende Tasks (Continuous)

### Dokumentation 📚

#### TASK-020: User-Manual schreiben
- **Priorität**: 🟡 Hoch
- **Status**: 💡 Idee
- **Aufwand**: M (8h)
- **Assignee**: TBD
- **Deadline**: Q2 2025

**Deliverables**:
- `docs/user-manual.md`
- Screenshots
- Video-Tutorials (optional)

---

#### TASK-021: Developer-Guide schreiben
- **Priorität**: 🟢 Mittel
- **Status**: 💡 Idee
- **Aufwand**: M (6h)
- **Assignee**: TBD
- **Deadline**: Q3 2025

**Deliverables**:
- `docs/developer-guide.md`
- Architecture-Diagrams
- API-Referenz

---

### Maintenance 🔧

#### TASK-022: Dependency-Updates
- **Priorität**: 🟢 Mittel
- **Status**: 🔄 Ongoing
- **Aufwand**: XS (1h/Monat)
- **Assignee**: TBD
- **Deadline**: Monatlich

**Teilaufgaben**:
- [ ] NuGet-Packages aktualisieren
- [ ] Breaking-Changes prüfen
- [ ] Tests ausführen
- [ ] Release-Notes

---

#### TASK-023: Performance-Monitoring
- **Priorität**: 🟡 Hoch
- **Status**: 🔄 Ongoing
- **Aufwand**: XS (2h/Sprint)
- **Assignee**: TBD
- **Deadline**: Wöchentlich

**Activities**:
- `/Xtream/OptimizationStats` monitoren
- Memory-Usage tracken
- API-Response-Times loggen
- Bottlenecks identifizieren

---

## Backlog (Nicht priorisiert)

### TASK-024: M3U-Import/Export
- **Priorität**: ⚪ Niedrig
- **Aufwand**: L (12h)
- **Status**: 💡 Idee

Beschreibung: M3U-Playlist-Import/Export für Kanal-Konfigurationen.

---

### TASK-025: Recording-Support
- **Priorität**: ⚪ Niedrig
- **Aufwand**: XL (24h+)
- **Status**: 💡 Idee

Beschreibung: Jellyfin-Recording-Integration für Live-TV.

---

### TASK-026: Multi-Language-Support
- **Priorität**: ⚪ Niedrig
- **Aufwand**: M (8h)
- **Status**: 💡 Idee

Beschreibung: Lokalisierung der Admin-UI (Englisch/Deutsch).

---

### TASK-027: Theme-Support
- **Priorität**: ⚪ Niedrig
- **Aufwand**: S (4h)
- **Status**: 💡 Idee

Beschreibung: Dark/Light-Theme für Admin-UI.

---

### TASK-028: Config-Backup/Restore
- **Priorität**: 🟢 Mittel
- **Aufwand**: M (6h)
- **Status**: 💡 Idee

Beschreibung: Automatisches Config-Backup und manueller Restore.

---

## Sprint-Planung

### Sprint 1 (2 Wochen) - Sicherheit

**Ziel**: Kritische Sicherheits-Tasks abschließen

**Tasks**:
- TASK-001: Credential-Encryption (L)
- TASK-003: Memory-Leak-Analyse (M)

**Team**: 1 Developer
**Kapazität**: 60h
**Story Points**: 20

---

### Sprint 2 (2 Wochen) - Stabilität

**Ziel**: Error-Handling & Performance

**Tasks**:
- TASK-004: Error-Handling (M)
- TASK-005: Async-Patterns (M)
- TASK-006: EPG-Caching (M)

**Team**: 1 Developer
**Kapazität**: 60h
**Story Points**: 24

---

### Sprint 3 (2 Wochen) - Cleanup

**Ziel**: Cache-Cleanup & Code-Quality

**Tasks**:
- TASK-007: Thumbnail-Cleanup (S)
- TASK-010: Code-Duplication (S)
- TASK-022: Dependency-Updates (XS)

**Team**: 1 Developer
**Kapazität**: 60h
**Story Points**: 12

---

### Sprint 4 (3 Wochen) - Proxy-System

**Ziel**: Stream-URL-Proxy implementieren

**Tasks**:
- TASK-002: Stream-URL-Proxy (XL)

**Team**: 1 Developer
**Kapazität**: 90h
**Story Points**: 26

---

## Task-Status-Dashboard

### Aktueller Stand

| Phase | Completed | In Progress | Planned | Total |
|-------|-----------|-------------|---------|-------|
| Phase 1 | 0 | 0 | 10 | 10 |
| Phase 2 | 0 | 0 | 5 | 5 |
| Phase 3 | 0 | 0 | 2 | 2 |
| Phase 4 | 0 | 0 | 2 | 2 |
| Ongoing | 0 | 2 | 2 | 4 |
| Backlog | 0 | 0 | 5 | 5 |
| **Total** | **0** | **2** | **26** | **28** |

### Velocity-Tracking

| Sprint | Planned SP | Completed SP | Velocity |
|--------|------------|--------------|----------|
| Sprint 1 | 20 | - | - |
| Sprint 2 | 24 | - | - |
| Sprint 3 | 12 | - | - |
| Sprint 4 | 26 | - | - |

---

## Task-Templates

### Bug-Report-Template

```markdown
## Bug: [Kurzbeschreibung]

**Priorität**: 🔴/🟡/🟢
**Aufwand**: XS/S/M/L/XL
**Betroffene Version**: v0.0.2

### Beschreibung
[Detaillierte Beschreibung]

### Reproduktion
1. Schritt 1
2. Schritt 2
3. ...

### Erwartetes Verhalten
[Was sollte passieren]

### Tatsächliches Verhalten
[Was passiert tatsächlich]

### Logs
```
[Relevante Log-Einträge]
```

### Dateien
- `File1.cs:123`
- `File2.cs:456`
```

### Feature-Request-Template

```markdown
## Feature: [Name]

**Priorität**: 🔴/🟡/🟢
**Aufwand**: XS/S/M/L/XL
**Ziel-Version**: v0.x.0

### User-Story
Als [Rolle] möchte ich [Funktion], damit [Nutzen].

### Akzeptanzkriterien
- [ ] Kriterium 1
- [ ] Kriterium 2
- [ ] Kriterium 3

### Technische Details
[Implementierungs-Hinweise]

### Abhängigkeiten
- TASK-XXX
- TASK-YYY

### Mockups/Designs
[Optional: Screenshots/Wireframes]
```

---

## Review-Prozess

### Definition of Done

Eine Task gilt als "Done" wenn:

- [ ] Code geschrieben & committed
- [ ] StyleCop-Warnings behoben
- [ ] Unit-Tests geschrieben (falls anwendbar)
- [ ] Code-Review durchgeführt
- [ ] Dokumentation aktualisiert
- [ ] Manual-Testing durchgeführt
- [ ] PR gemerged

### Code-Review-Checklist

- [ ] Code folgt StyleCop-Regeln
- [ ] XML-Documentation vorhanden
- [ ] Keine Magic-Numbers
- [ ] Error-Handling korrekt
- [ ] Async-Patterns korrekt
- [ ] CancellationToken verwendet
- [ ] IDisposable korrekt
- [ ] Memory-Leaks geprüft
- [ ] Performance akzeptabel
- [ ] Security-Implications geprüft

---

## Kontakt & Collaboration

### Issue-Tracking
- **GitHub Issues**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/issues
- **Label-System**: `bug`, `enhancement`, `documentation`, `performance`, `security`

### Project-Board
- **GitHub Projects**: https://github.com/users/andreaspointecker-source/projects/[TBD]
- **Columns**: Backlog, Sprint, In Progress, Review, Done

---

**Task-Liste Version**: 1.0
**Letzte Aktualisierung**: 2025-01-13
**Maintainer**: TBD
**Review-Intervall**: Wöchentlich
