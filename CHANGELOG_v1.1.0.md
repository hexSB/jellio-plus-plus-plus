# Jellio v1.1.0 - Jellyseerr Integration & Configuration Management

## 🎉 Major Features

### Jellyseerr Integration
- **Automatic Media Requests**: When a stream is unavailable in Stremio, users can now click on a special "📥 Request via Jellyseerr" stream option that sends automatic requests to Jellyseerr
- **Smart Search**: Automatically searches Jellyseerr/TMDB for media using IMDb IDs to find exact matches
- **Movie & TV Show Support**: Handles both movies and TV series requests with proper API formatting
- **Request Deduplication**: Intelligent deduplication system prevents duplicate requests with race-condition-free locking mechanism
- **Configurable**: Full configuration UI with toggle switches, URL input, and API key management

### Configuration Management
- **Dual Persistence**: Configuration is saved both locally (browser localStorage) and server-side (Jellyfin plugin configuration)
- **Server-Side Storage**: Added new `/Jellio/save-config` and `/Jellio/get-config` endpoints for persistent configuration
- **Enhanced UI**: New "Save Configuration to Jellyfin" button with toast notifications for save confirmation
- **Auto-Load**: Configuration automatically loads from server on plugin page load, with localStorage as fallback

## 🔧 Technical Improvements

### Backend (C#/.NET 9)
- Added `PluginConfiguration.cs` for server-side config storage with properties:
  - `JellyseerrEnabled` (bool)
  - `JellyseerrUrl` (string)
  - `JellyseerrApiKey` (string)
  - `PublicBaseUrl` (string)
- New `RequestController.cs` with Jellyseerr API integration:
  - TMDB search functionality
  - Movie/TV show request creation
  - Request deduplication with ConcurrentDictionary cache
  - Per-request locking to prevent race conditions
  - 30-second TTL for deduplication entries
- Updated `WebController.cs` with configuration endpoints
- Modified `AddonController.cs` to include Jellyseerr fallback streams
- Added `SaveConfigRequest.cs` model for configuration API

### Frontend (React/TypeScript)
- New `useConfigStorage.ts` hook for dual persistence (localStorage + server)
- Enhanced configuration form with Jellyseerr fields:
  - Enable/disable toggle
  - Jellyseerr URL input with helpful placeholder
  - API key input with secure handling
- Added "Save Configuration to Jellyfin" button
- Toast notifications for save confirmation
- Automatic trailing slash removal from URLs
- Server configuration takes precedence over localStorage

## 🛠️ Bug Fixes & Refinements
- Fixed Docker networking issues (use internal container addresses instead of external URLs)
- Fixed API key handling (removed unnecessary base64 encoding)
- Fixed request body formatting for movies vs TV shows
- Implemented proper request deduplication with race condition protection

## 📝 Configuration Notes
- **Jellyseerr URL**: Use internal Docker addresses (e.g., `http://192.168.0.125:5055`) for Docker setups instead of external domains
- **API Key**: Use plain text API keys directly from Jellyseerr settings (no encoding needed)
- **Public Base URL**: Required for generating dummy stream URLs that trigger Jellyseerr requests

## 🔄 Migration from v1.0.0
No breaking changes. Existing configurations will continue to work. New Jellyseerr features are optional and disabled by default.

## 📦 Installation
1. Download `jellio_1.1.0.0.zip` from the releases page
2. Upload to Jellyfin via Admin Dashboard > Plugins > Upload Plugin
3. Restart Jellyfin server
4. Configure Jellyseerr settings in the plugin configuration page (optional)

## 🙏 Credits
Community-maintained fork with Jellyfin 10.11.x support by @InfiniteAvenger

## 🔗 Links
- GitHub Repository: https://github.com/InfiniteAvenger/jellio-plus
- Full Changelog: https://github.com/InfiniteAvenger/jellio-plus/compare/v1.0.0...v1.1.0
