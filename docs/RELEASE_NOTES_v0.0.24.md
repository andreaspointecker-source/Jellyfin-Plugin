# CandyTv v0.0.24 - Live Channel Repair

**Release Date**: 2025-10-18  
**Type**: Hotfix (Playback)

---

## 🎯 Problem

Even after reverting to credential URLs in v0.0.23, live TV streams could still drop instantly.  
Xtream live endpoints require the `/live/{user}/{pass}/{streamId}.{ext}` pattern.  
Our restreamer was still missing `/live` and the container extension, so providers responded with errors.

---

## ✅ Fixes in v0.0.24

- Added the `/live` segment and container extension when generating the provider URL prior to restreaming.
- Defaulted the live extension to `ts` when Xtream does not send a value.
- Live playback now uses the original credential URL and remains open, while the restream buffer continues to serve Jellyfin clients.

---

## 🧪 Verification

1. Update to v0.0.24 and restart Jellyfin.
2. Start a live channel: network traces should show `/live/{user}/{pass}/{id}.ts`.
3. Confirm the stream stays open (no immediate “Restream finished” log entry).
4. Optional: Test VOD/catch-up—they continue to use direct credential URLs from v0.0.23.

---

Thanks for the patience—live TV is back in action! 📺🚀
