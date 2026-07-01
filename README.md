# Jellio+++
[![Release](https://img.shields.io/github/v/release/hexSB/jellio-plus-plus-plus)](https://github.com/hexSB/jellio-plus-plus-plus/releases)

Stream your Jellyfin library directly in Stremio with seamless integration.

**Jellio+++** is a fork of a fork:

- [**Jellio**](https://github.com/vanchaxy/jellio) by [Vanchaxy](https://github.com/vanchaxy) - the original Jellyfin↔Stremio bridge.
- [**Jellio+**](https://github.com/InfiniteAvenger/jellio-plus) by [InfiniteAvenger](https://github.com/InfiniteAvenger) - fork adding Jellyfin 10.11.x support.
- [**Jellio++**](https://github.com/wujekbogdan/jellio-plus-plus) by [wujekbogdan](https://github.com/wujekbogdan) - adds HLS streaming, OpenSubtitles hashes, and public base URL support.
- **Jellio+++** - this fork. Rewritten plugin UI, transcoding controls, subtitle support, and Jellyfin 12 compatibility.

Every fork gets another `+`. We don't make the rules.

## Features

- **Full Library Integration** - Access your entire Jellyfin movie and TV show collection in Stremio
- **Cross-Platform** - Works on all Stremio-supported devices (Windows, macOS, Linux, Android, iOS)
- **HLS Streaming** - Adaptive bitrate streaming with proper seeking via `master.m3u8`
- **Transcoding Controls** - Choose adaptive, forced, or disabled transcoding separately for video and audio
- **Subtitle Support** - Text subtitles (SRT, ASS, VTT) served from Jellyfin; OpenSubtitles hash for automatic subtitle matching
- **Audio Track Selection** - Pick specific audio tracks (language, codec, channels) directly in Stremio
- **AV1 Compatibility** - Adaptive video mode transcodes AV1 to H.264 to prevent black screen issues
- **Public Base URL** - Override the server URL for HTTPS connectivity behind reverse proxies or tunnels
- **Jellyseerr Integration** - Request missing content directly from Stremio
- **Logs Viewer** - In-app streaming logs with auto-refresh for debugging
- **Modern UI** - React-based configuration interface with dark mode support

## How it Works

### Browsing Your Library in Stremio

Jellio+++ allows you to instantly stream media from your Jellyfin server through Stremio. Simply search for the media in Stremio, and if it is on your Jellyfin server, it will appear!

![Jellio+++ Streaming in Stremio](assets/jellio-stream.PNG)

### Jellyseerr Integration

Enable the optional Jellyseerr functionality to be able to directly request media to be sent to Jellyseerr with a simple in-app solution.

![Jellyseerr Integration](assets/jellyseer-integration.PNG)

### Installation

NOTICE: Your Jellyfin instance needs to be reachable over HTTPS because Stremio requires HTTPS for addon URLs. You need an HTTPS tunnel such as Cloudflare Tunnel, Tailscale Funnel, ngrok, etc.

1. Open Jellyfin Dashboard > Plugins > Manage Repositories
2. Click "New Repository" and add "Jellio+++" for the name, and "https://raw.githubusercontent.com/hexSB/jellio-plus-plus-plus/metadata/jellyfin-repo-manifest.json" for the repository url
3. Go back to Plugins, and under "All" find and install Jellio+++
4. Restart Jellyfin
5. Jellyfin Dashboard > Plugins > Installed > Jellio+++ and then click "Settings"
6. Select which libraries you want to be included in Stremio
7. (Optional) Input your local Jellyseerr url (e.g. http://192.168.0.105:5055) and your Jellyseerr API key. Also include your Public URL for Jellyfin (e.g. https://jellyfin.yourserver.com)
8. Click "Save Configuration for Jellyfin"
9. Lastly, click "Install." Copy that link and paste it in your Stremio addons. You're all done!

## Configuration

### Transcoding Settings

- **Video Transcoding Mode** (default: Adaptive) - Adaptive copies HEVC/H.264 when supported and transcodes unsupported video; Force Transcode always re-encodes to H.264; No Transcode never requests video transcoding
- **Audio Transcoding Mode** (default: Adaptive) - Adaptive copies Opus/EAC3/AAC when supported and transcodes unsupported audio; Force Transcode always re-encodes to AAC; No Transcode never requests audio transcoding
- **Max Video Bitrate** - Maximum video bitrate in Mbps (10-200, default: 120)

### Public Base URL

If your Jellyfin server is behind a reverse proxy, Cloudflare Tunnel, or Tailscale Funnel, set the public HTTPS URL here so Stremio can reach it.

## Development

### Backend stack

Build the plugin and start Jellyfin + Stremio:

```bash
docker compose run --rm dotnet-builder
docker compose up -d
```

Jellyfin: http://localhost:8096. Stremio: http://localhost:11470. Test media goes in `./media/`.

### Plugin UI

```bash
cd jellio-web
npm install
npm run dev
```

Served at http://localhost:5173/jelliopp/. All API calls are mocked with MSW; the UI does not connect to any backend.

## Requirements

- Jellyfin 12.0.0+
- Stremio (any platform)
- HTTPS access to Jellyfin (required by Stremio)
