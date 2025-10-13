# GitHub Veröffentlichung - Anleitung

## Voraussetzungen

1. **Git installiert**: Prüfen mit `git --version`
2. **GitHub-Account**: andreaspointecker-source
3. **Repository erstellt**: https://github.com/andreaspointecker-source/Jellyfin-Plugin

---

## Schritt 1: Git-Repository initialisieren (falls noch nicht geschehen)

```bash
cd "C:\Users\Anwender\Programme\Jellyfin.Xtream-original"

# Falls noch kein Git-Repository
git init

# Git-User konfigurieren
git config user.name "Candy"
git config user.email "your-email@example.com"
```

---

## Schritt 2: Remote-Repository hinzufügen

```bash
# Altes Remote entfernen (falls vorhanden)
git remote remove origin

# Neues Remote hinzufügen
git remote add origin https://github.com/andreaspointecker-source/Jellyfin-Plugin.git

# Prüfen
git remote -v
```

---

## Schritt 3: Dateien für Commit vorbereiten

```bash
cd "C:\Users\Anwender\Programme\Jellyfin.Xtream-original"

# Alle geänderten Dateien hinzufügen
git add .

# Status prüfen
git status
```

---

## Schritt 4: Commit erstellen

```bash
# Commit mit aussagekräftiger Message
git commit -m "Initial commit: CandyTv v0.0.2

- Complete plugin implementation
- Live TV with EPG support
- VOD and Series support
- Catchup TV functionality
- Custom channel lists with fuzzy matching
- Thumbnail caching
- Connection queue management
- Comprehensive documentation (spec.md, plan.md, tasks.md, ai-behavior.md)
"
```

---

## Schritt 5: Push zu GitHub

```bash
# Branch umbenennen zu main (falls nötig)
git branch -M main

# Push zu GitHub (erstes Mal mit -u für upstream)
git push -u origin main
```

**Hinweis**: Beim ersten Push werden Sie nach Ihren GitHub-Credentials gefragt:
- **Username**: andreaspointecker-source
- **Password**: Verwenden Sie ein **Personal Access Token** statt Passwort!

---

## Schritt 6: Release erstellen

### Option A: Via GitHub Web-UI (Empfohlen)

1. Gehen Sie zu: https://github.com/andreaspointecker-source/Jellyfin-Plugin
2. Klicken Sie auf **"Releases"** (rechte Seitenleiste)
3. Klicken Sie auf **"Create a new release"**
4. Füllen Sie aus:
   - **Tag version**: `v0.0.2`
   - **Release title**: `CandyTv v0.0.2 - Initial Release`
   - **Description**: (siehe unten)
5. Upload der DLL:
   - Klicken Sie auf **"Attach binaries"**
   - Wählen Sie: `Jellyfin.Xtream\bin\Release\net8.0\CandyTv.dll`
6. Klicken Sie auf **"Publish release"**

### Release-Description-Template:

```markdown
# CandyTv v0.0.2 - Initial Release

Professionelles Jellyfin-Plugin für Xtream-kompatible IPTV-APIs.

## Features

✅ **Live-TV**
- EPG (Electronic Program Guide) mit 10-Minuten-Cache
- Custom Channel-Listen mit Fuzzy-Matching
- Channel-Überschreibungen (Nummer, Name, Icon)
- Direct-Stream mit Restream-Buffering

✅ **Video-On-Demand**
- Kategoriebasierte Organisation
- Thumbnail-Caching
- Provider-ID-Support für Metadaten

✅ **TV-Serien**
- Hierarchische Struktur: Kategorien → Serien → Staffeln → Episoden
- Vollständige Metadaten (Genres, Cast, Ratings)
- Episode-Codec-Informationen

✅ **TV-Aufzeichnungen (Catchup)**
- EPG-basiert mit konfigurierbarer Archiv-Dauer
- Tag-für-Tag-Browsing
- Fallback bei fehlenden EPG-Daten

✅ **Performance & Optimierung**
- Connection-Queue für Rate-Limiting
- 3-Tier-Caching (Memory, Extended, Disk)
- Thumbnail-Cache mit Retention-Policy
- Optimierungs-Statistiken-API

## Installation (Unraid)

1. Download `CandyTv.dll`
2. Kopieren nach: `/mnt/user/appdata/jellyfin/plugins/CandyTv_0.0.2/`
3. Permissions setzen:
   ```bash
   chown -R 99:100 /mnt/user/appdata/jellyfin/plugins/
   chmod -R 755 /mnt/user/appdata/jellyfin/plugins/
   ```
4. Jellyfin-Container neustarten: `docker restart jellyfin`
5. In Jellyfin Admin-UI: Plugins → CandyTv → Zugangsdaten konfigurieren

## Anforderungen

- Jellyfin Server 10.10.7.0+
- .NET 8.0
- Xtream-kompatibler IPTV-Provider

## Dokumentation

- **Technische Spezifikation**: [spec.md](spec.md)
- **Entwicklungsplan**: [plan.md](plan.md)
- **Task-Liste**: [tasks.md](tasks.md)
- **AI-Verhaltensrichtlinien**: [ai-behavior.md](ai-behavior.md)

## Bekannte Probleme

⚠️ **Credential-Exposure**: Stream-URLs enthalten Plaintext-Credentials.
→ Nur auf vertrauenswürdigen/privaten Servern verwenden!

## Support

- **Issues**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/issues
- **Discussions**: https://github.com/andreaspointecker-source/Jellyfin-Plugin/discussions

---

**Owner**: Candy
**Lizenz**: GPL-3.0
**Framework**: .NET 8.0
```

---

### Option B: Via GitHub CLI (gh)

```bash
# GitHub CLI installieren (falls noch nicht vorhanden)
# https://cli.github.com/

# Authentifizieren
gh auth login

# Release erstellen
gh release create v0.0.2 \
  --title "CandyTv v0.0.2 - Initial Release" \
  --notes-file RELEASE_NOTES.md \
  "Jellyfin.Xtream\bin\Release\net8.0\CandyTv.dll#CandyTv.dll"
```

---

### Option C: Via Git-Tags

```bash
# Tag erstellen
git tag -a v0.0.2 -m "CandyTv v0.0.2 - Initial Release"

# Tag pushen
git push origin v0.0.2

# Dann via GitHub Web-UI Release erstellen (wie Option A)
```

---

## Schritt 7: Release-DLL bauen (falls noch nicht geschehen)

```bash
cd "C:\Users\Anwender\Programme\Jellyfin.Xtream-original\Jellyfin.Xtream"

# Clean
dotnet clean Jellyfin.Xtream.sln

# Release-Build
dotnet build Jellyfin.Xtream.sln -c Release

# Prüfen
ls bin/Release/net8.0/CandyTv.dll
```

**Output**: `bin\Release\net8.0\CandyTv.dll`

---

## Schritt 8: GitHub-Repository-Setup

### README.md erstellen (empfohlen)

```bash
# Erstellen Sie eine README.md im Root
```

Siehe unten für README-Template.

### .gitignore erstellen

```bash
# Erstellen Sie eine .gitignore
```

Siehe unten für .gitignore-Template.

---

## GitHub Personal Access Token erstellen

1. Gehen Sie zu: https://github.com/settings/tokens
2. Klicken Sie auf **"Generate new token (classic)"**
3. Scopes auswählen:
   - ✅ `repo` (full control)
   - ✅ `workflow` (optional)
4. Klicken Sie auf **"Generate token"**
5. **Token kopieren** (wird nur einmal angezeigt!)
6. Verwenden Sie dieses Token als Passwort beim `git push`

---

## Troubleshooting

### Problem: "Permission denied"

```bash
# SSH-Key verwenden statt HTTPS
git remote set-url origin git@github.com:andreaspointecker-source/Jellyfin-Plugin.git

# SSH-Key zu GitHub hinzufügen (falls noch nicht geschehen)
# https://github.com/settings/keys
```

### Problem: "Repository not found"

```bash
# Prüfen Sie, ob das Repository existiert
# https://github.com/andreaspointecker-source/Jellyfin-Plugin

# Falls nicht: Erstellen Sie es zuerst auf GitHub
```

### Problem: "Fatal: Not a git repository"

```bash
cd "C:\Users\Anwender\Programme\Jellyfin.Xtream-original"
git init
git remote add origin https://github.com/andreaspointecker-source/Jellyfin-Plugin.git
```

---

## Checkliste für erfolgreiche Veröffentlichung

- [ ] Git-Repository initialisiert
- [ ] Remote-Repository hinzugefügt
- [ ] Alle Dateien committed
- [ ] Auf GitHub gepusht
- [ ] Release-DLL gebaut (`bin/Release/net8.0/CandyTv.dll`)
- [ ] GitHub-Release erstellt (v0.0.2)
- [ ] DLL zu Release hochgeladen
- [ ] README.md erstellt
- [ ] .gitignore hinzugefügt
- [ ] Repository-Settings konfiguriert (Description, Topics, etc.)

---

## Nächste Schritte nach Veröffentlichung

1. **Community-App für Unraid** (siehe plan.md)
2. **CI/CD-Pipeline** einrichten (GitHub Actions)
3. **Automated-Tests** implementieren
4. **Issue-Templates** erstellen
5. **Contributing-Guide** schreiben

---

**Viel Erfolg bei der Veröffentlichung! 🚀**
