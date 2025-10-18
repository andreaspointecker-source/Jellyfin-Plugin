# CandyTv v0.0.21 - Single Connection Streaming

**Release Date**: 2025-01-18  
**Type**: Feature & Security Update

---

## 🔐 Executive Summary

Xtream providers that only permit **one active connection** are now fully supported. Streams no longer expose credentials, and playback is funneled through a Jellyfin proxy that enforces the single-session rule.

### Key Outcomes
- ✅ One provider connection at a time (live, VOD, series, catch-up)
- ✅ Credentials removed from playback URLs
- ✅ Unified proxy endpoint `/Xtream/Stream/{token}`

---

## 🚀 What's New

### 1. Stream Proxy & Tokens
- Generates short-lived tokens per playback request
- Serves media from `/Xtream/Stream/{token}` inside Jellyfin
- Prevents clients from seeing Xtream usernames or passwords

### 2. Provider Connection Lease
- Global semaphore shared by all playback types
- Live restream (`Restream.cs`) acquires the same lease
- Stops parallel channel playback when provider only allows one session

### 3. Catch-up / VOD / Series Alignment
- All media routes through the proxy instead of direct Xtream URLs
- Ensures identical enforcement for every content type

---

## 🔄 Changes & Fixes
- Updated `StreamService` to emit proxy URLs for non-restream requests
- Added `StreamTokenService` with token issuance and provider lease management
- Exposed `/Xtream/Stream/{token}` endpoint returning proxied streams
- Integrated lease acquisition into `Restream.Open`/`Close`
- Registered new services via dependency injection

---

## 🧪 Testing Tips

1. Play a live channel, then start a second stream (live or VOD)
   - Second stream should wait until the first stops (provider sees only one session)
2. Inspect the Jellyfin network tab: all playback URLs should point to `/Xtream/Stream/<token>`
3. Confirm credentials are absent from Jellyfin logs and client URLs

---

## 📦 Packaging Checklist
- `CandyTv.dll`
- `CandyTV.png`
- `Diacritics.dll`
- `FuzzySharp.dll`
- `ICU4N.dll`
- `ICU4N.Transliterator.dll`
- `J2N.dll`
- `Newtonsoft.Json.dll`

---

**Enjoy safer streaming with zero credential exposure!**
