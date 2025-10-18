# Release Instructions for v0.0.23

## ✅ Was bereits erledigt ist:

1. ✅ Code committed: `ab507c2`
2. ✅ Tag erstellt: `v0.0.23`
3. ✅ Gepusht zu GitHub: master + tag

**GitHub Repository**: https://github.com/andreaspointecker-source/Jellyfin-Plugin

---

## 📦 Nächster Schritt: GitHub Release erstellen

### Option A: GitHub Web UI (Empfohlen)

1. **Gehe zu Releases**:
   ```
   https://github.com/andreaspointecker-source/Jellyfin-Plugin/releases/new
   ```

2. **Wähle Tag**: `v0.0.23` (sollte bereits ausgewählt sein)

3. **Release Title**:
   ```
   v0.0.23 - Performance & Stability Release
   ```

4. **Description**:
   - Kopiere den Inhalt aus `docs/RELEASE_NOTES_v0.0.23.md`
   - Oder nutze diese Kurzversion:

   ```markdown
   ## 🔄 Stream URL Rollback

   ### Highlights
   - **Direct Xtream Links Are Back**: Playback URLs again include `{username}/{password}` so clients that expect them work immediately.
   - **Fixes Token Playback Failures**: HEAD/RANGE preflight requests no longer consume single-use tokens.
   - **Proxy Paused**: Token infrastructure stays in code but is disabled until a multi-request-aware version is ready.

   ### Notes
   - Restart Jellyfin after installing 0.0.23.
   - Start a VOD/series/catch-up item and confirm the stream URL contains credentials (and plays successfully).

   **Full Changelog**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/compare/v0.0.22...v0.0.23

   See [CHANGELOG.md](CHANGELOG.md) and [Release Notes](docs/RELEASE_NOTES_v0.0.23.md) for details.
   ```

5. **Upload DLL**:
   - Drag & drop: `Jellyfin.Xtream\bin\Release\net8.0\CandyTv.dll`
   - Rename to: `CandyTv-0.0.23.dll` (optional)

6. **Publish Release** ✅

---

### Option B: GitHub CLI (wenn gh auth login gemacht wurde)

```bash
cd "C:\Users\Anwender\Programme\Jellyfin.Xtream-original"

# Authentifizieren (einmalig)
gh auth login

# Release erstellen
gh release create v0.0.23 \
  "Jellyfin.Xtream/bin/Release/net8.0/CandyTv.dll#CandyTv-0.0.21.dll" \
  --title "v0.0.23 - Performance & Stability Release" \
  --notes-file "docs/RELEASE_NOTES_v0.0.23.md"
```

---

## 📋 Checklist nach Release

- [ ] Release auf GitHub veröffentlicht
- [ ] DLL hochgeladen und herunterladbar
- [ ] Release Notes korrekt angezeigt
- [ ] Tag `v0.0.23` sichtbar
- [ ] Download-Link testen

---

## 🔗 Wichtige Links

- **Repository**: https://github.com/andreaspointecker-source/Jellyfin-Plugin
- **Releases**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/releases
- **Tag v0.0.23**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/releases/tag/v0.0.23
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



