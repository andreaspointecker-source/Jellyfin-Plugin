# CandyTv v0.0.25 - Direct Source Live Fix

**Release Date**: 2025-10-18  
**Type**: Hotfix (Live TV)

---

## 🛠 What’s Fixed

- Live streams now reuse Xtream’s original `direct_source` URL (including credentials, query parameters, and bitrate hints) instead of rebuilding the path.
- Added upstream HTTP status logging so troubleshooting provider issues is easier.
- Keeps restream buffering in place while ensuring strict providers no longer close the connection immediately.

---

## ✅ Verification Steps

1. Update to v0.0.25 and restart Jellyfin.
2. Play a live channel – the restream should stay open, and playback should continue without instantly stopping.
3. Check `log_YYYYMMDD*.log` for `Provider responded 200` entries if you need to confirm upstream access.

---

Thank you for your patience while we ironed out the live-stream regressions! 🎬📺
