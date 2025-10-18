# Release Instructions for v0.0.22

## ✅ Was bereits erledigt ist:

1. ✅ Code committed: `ab507c2`
2. ✅ Tag erstellt: `v0.0.22`
3. ✅ Gepusht zu GitHub: master + tag

**GitHub Repository**: https://github.com/andreaspointecker-source/Jellyfin-Plugin

---

## 📦 Nächster Schritt: GitHub Release erstellen

### Option A: GitHub Web UI (Empfohlen)

1. **Gehe zu Releases**:
   ```
   https://github.com/andreaspointecker-source/Jellyfin-Plugin/releases/new
   ```

2. **Wähle Tag**: `v0.0.22` (sollte bereits ausgewählt sein)

3. **Release Title**:
   ```
   v0.0.22 - Performance & Stability Release
   ```

4. **Description**:
   - Kopiere den Inhalt aus `docs/RELEASE_NOTES_v0.0.22.md`
   - Oder nutze diese Kurzversion:

   ```markdown
   ## 🔧 Cache Compatibility Hotfix

   ### Highlights
   - **No More TMDb Failures**: Removing CandyTv’s cache size limit stops `Cache entry must specify a value for Size`.
   - **Plays Nice With Others**: Jellyfin’s shared `IMemoryCache` is used unchanged, so other plugins keep working.
   - **Zero Behaviour Change**: CandyTv’s own EPG/thumbnail caches still track stats and clean themselves.

   ### Notes
   - Restart Jellyfin after installing 0.0.22.
   - Run a metadata refresh or wait for the next scheduled scan to verify clean logs.

   **Full Changelog**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/compare/v0.0.21...v0.0.22

   See [CHANGELOG.md](CHANGELOG.md) and [Release Notes](docs/RELEASE_NOTES_v0.0.22.md) for details.
   ```

5. **Upload DLL**:
   - Drag & drop: `Jellyfin.Xtream\bin\Release\net8.0\CandyTv.dll`
   - Rename to: `CandyTv-0.0.22.dll` (optional)

6. **Publish Release** ✅

---

### Option B: GitHub CLI (wenn gh auth login gemacht wurde)

```bash
cd "C:\Users\Anwender\Programme\Jellyfin.Xtream-original"

# Authentifizieren (einmalig)
gh auth login

# Release erstellen
gh release create v0.0.22 \
  "Jellyfin.Xtream/bin/Release/net8.0/CandyTv.dll#CandyTv-0.0.21.dll" \
  --title "v0.0.22 - Performance & Stability Release" \
  --notes-file "docs/RELEASE_NOTES_v0.0.22.md"
```

---

## 📋 Checklist nach Release

- [ ] Release auf GitHub veröffentlicht
- [ ] DLL hochgeladen und herunterladbar
- [ ] Release Notes korrekt angezeigt
- [ ] Tag `v0.0.22` sichtbar
- [ ] Download-Link testen

---

## 🔗 Wichtige Links

- **Repository**: https://github.com/andreaspointecker-source/Jellyfin-Plugin
- **Releases**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/releases
- **Tag v0.0.22**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/releases/tag/v0.0.22
- **Commit**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/commit/ab507c2

---

## 📁 Build-Artifact-Location

```
C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream\bin\Release\net8.0\CandyTv.dll
```

**File Size**: ~200-300 KB (geschätzt)
**Target Framework**: .NET 8.0
**Jellyfin ABI**: 10.10.7.0

---

## 🎉 Nach dem Release

### 1. Testen
```bash
# Download DLL von GitHub
# Kopiere nach: %AppData%\Jellyfin\plugins\

# Starte Jellyfin neu
# Überprüfe: Admin > Plugins > CandyTv
# Version sollte 0.0.21 sein
```

### 2. Monitoring
```bash
# Nach 24h Laufzeit:
GET http://jellyfin:8096/Xtream/OptimizationStats

# Erwartete Werte:
# - epgCacheHitRate > 80%
# - Memory < 250 MB
# - Sockets < 50
```

### 3. Announcements (optional)
- Reddit: /r/jellyfin
- Jellyfin Forum
- Discord Server

---

## 🔄 Rollback (falls nötig)

Falls es Probleme gibt:

```bash
# Zurück zu v0.0.15
git checkout v0.0.15
dotnet build -c Release

# Oder: Download v0.0.15 von GitHub Releases
```

---

**Status**: ✅ Code gepusht, bereit für Release-Erstellung!
**Nächster Schritt**: GitHub Web UI → Create Release


