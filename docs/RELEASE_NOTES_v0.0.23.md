# CandyTv v0.0.23 - Stream URL Rollback

**Release Date**: 2025-10-18  
**Type**: Hotfix (Playback)

---

## 🚨 What Happened?

In v0.0.21 we introduced tokenised stream URLs to keep Xtream credentials server-side.
However, many Jellyfin clients issue preliminary `HEAD`/`RANGE` requests before starting
playback. Those extra requests exhausted the single-use tokens and streams stopped loading.

---

## ✅ Fix in v0.0.23

- Restored the classic Xtream URLs (including username/password) for VOD, series, and catch-up.
- Live TV (restream) behaviour is unchanged—it already used direct URLs.
- Token infrastructure remains in the codebase but is now dormant until we implement
  a multi-request-aware proxy.

---

## 🧪 Testing Checklist

1. Update to v0.0.23 and restart Jellyfin.
2. Play any Xtream channel or VOD item—URLs should again contain `.../{username}/{password}/...`.
3. Verify playback succeeds even after metadata refresh or repeated start/stop.

---

Thank you for the quick feedback—compatibility takes priority! 🎯
