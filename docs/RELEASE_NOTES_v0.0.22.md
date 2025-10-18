# CandyTv v0.0.22 - Cache Compatibility Hotfix

**Release Date**: 2025-10-18  
**Type**: Hotfix (Stability)

---

## ✅ Summary

Jellyfin servers running CandyTv alongside other metadata providers (e.g. TMDb) reported repeated
`Cache entry must specify a value for Size` exceptions. CandyTv previously registered an `IMemoryCache`
with `SizeLimit` enabled, forcing every cache user across the server to provide an explicit size.
Many upstream plugins do not (and should not have to) set a size, so their requests failed.

v0.0.22 removes CandyTv's custom cache sizing and relies on the server's default cache configuration.
All standard metadata and image downloads can now co-exist without errors.

---

## 🔧 Changes

- Replaced the custom `AddMemoryCache` configuration with the vanilla registration used by Jellyfin.
- Ensures TMDb, TVDB, and other metadata/image providers can cache results without specifying `Size`.
- No behavioral changes to CandyTv's EPG or thumbnail caches—they continue to track statistics internally.

---

## 🧪 Verification

1. Restart Jellyfin after updating to v0.0.22.
2. Trigger a metadata refresh (or wait for the scheduled scan).
3. Confirm `Cache entry must specify a value for Size` no longer appears in
   `C:\Users\Anwender\AppData\Local\jellyfin\log\log_*.log`.

---

Enjoy stable metadata syncing again! 🎉
