# Jellio v1.1.0 - Jellyseerr Integration

## What's New

Added Jellyseerr integration! When a stream is unavailable in Stremio, you'll now see a **"📥 Request via Jellyseerr"** option. Click it to automatically request the movie or TV show in Jellyseerr.

## Configuration

To enable this feature:
1. Open the Jellio plugin configuration page
2. Enable "Jellyseerr Integration"
3. Enter your Jellyseerr URL (use internal Docker address for Docker setups, e.g., `http://192.168.0.125:5055`)
4. Enter your Jellyseerr API Key (found in Jellyseerr Settings > General > API Key)
5. Click "Save Configuration to Jellyfin"

## Installation

Download `jellio_1.1.0.0.zip` and upload it via Jellyfin Admin Dashboard > Plugins > Upload Plugin. Restart Jellyfin after installation.

---

**Checksum (MD5)**: `34e6ce3ada0f46e5d89d0968b29c78fa`
