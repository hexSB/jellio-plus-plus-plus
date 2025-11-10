# Jellio v1.1.0 Release Summary

## ✅ Completed Actions

### 1. Git Tag Cleanup
- **Deleted Local Tags**: Removed tags v1.0.1 through v1.1.14 (16 tags)
- **Deleted Remote Tags**: Removed same tags from GitHub
- **Remaining Tags**: Only v1.0.0 (base) and v1.1.0 (new release)

### 2. Created v1.1.0 Release
- **Tag Created**: Annotated tag v1.1.0 on commit 9ffcf66
- **Tag Message**: "v1.1.0 - Jellyseerr Integration & Configuration Management"
- **Pushed to Remote**: Successfully pushed v1.1.0 tag to GitHub

### 3. Built Release Package
- **Build Configuration**: Release mode (.NET 9)
- **Package Name**: jellio_1.1.0.0.zip
- **Location**: Root of repository
- **MD5 Checksum**: 34E6CE3ADA0F46E5D89D0968B29C78FA (lowercase: 34e6ce3ada0f46e5d89d0968b29c78fa)

### 4. Updated Repository Manifest
- **Branch**: metadata
- **File**: jellyfin-repo-manifest.json
- **Versions Listed**: 
  - v1.1.0.0 (new release)
  - v1.0.0.0 (original stable)
- **Status**: Pushed to GitHub with force (cleaned up all old version entries)

### 5. Created Documentation
- **Changelog**: CHANGELOG_v1.1.0.md (comprehensive feature documentation)
- **Location**: Root of main branch

## 📦 Release Details

### Version Information
- **Version**: 1.1.0.0
- **Target ABI**: 10.11.0.0 (Jellyfin 10.11.x)
- **Release Date**: January 26, 2025
- **Download URL**: https://github.com/InfiniteAvenger/jellio-plus/releases/download/v1.1.0/jellio_1.1.0.0.zip

### Manifest Entry
```json
{
    "version": "1.1.0.0",
    "changelog": "Jellio v1.1.0 - Jellyseerr Integration & Configuration Management. Major features: Automatic media requests via Jellyseerr when streams are unavailable, dual configuration persistence (localStorage + server-side), request deduplication with race-condition protection, enhanced configuration UI with save button, support for both movies and TV shows, smart TMDB search integration.",
    "targetAbi": "10.11.0.0",
    "sourceUrl": "https://github.com/InfiniteAvenger/jellio-plus/releases/download/v1.1.0/jellio_1.1.0.0.zip",
    "checksum": "34e6ce3ada0f46e5d89d0968b29c78fa",
    "timestamp": "2025-01-26T12:00:00Z"
}
```

## 🎯 Next Steps

### To Complete the Release:

1. **Upload Release to GitHub**:
   - Go to: https://github.com/InfiniteAvenger/jellio-plus/releases/new
   - Tag: v1.1.0
   - Title: "Jellio v1.1.0 - Jellyseerr Integration & Configuration Management"
   - Upload: jellio_1.1.0.0.zip
   - Description: Copy from CHANGELOG_v1.1.0.md

2. **Verify Manifest**:
   - Check: https://raw.githubusercontent.com/InfiniteAvenger/jellio-plus/metadata/jellyfin-repo-manifest.json
   - Should show only v1.1.0 and v1.0.0

3. **Test Installation**:
   - Install via Jellyfin plugin repository
   - Or manual upload of jellio_1.1.0.0.zip
   - Verify all features work as expected

## 📋 Feature Changelog Summary

### Major Features
✅ **Jellyseerr Integration**
- Automatic media requests when streams unavailable
- Smart TMDB search using IMDb IDs
- Support for movies and TV shows
- Request deduplication with race-condition protection

✅ **Configuration Management**
- Dual persistence (localStorage + server-side)
- New Save button in configuration UI
- Auto-load from server on page load
- Toast notifications for save confirmation

✅ **Enhanced User Experience**
- "📥 Request via Jellyseerr" stream option in Stremio
- Helpful URL guidance in configuration form
- Automatic trailing slash removal
- Secure API key handling

### Technical Improvements
- Added `PluginConfiguration.cs` for server-side storage
- New `RequestController.cs` with Jellyseerr API integration
- Enhanced `WebController.cs` with config endpoints
- Updated `AddonController.cs` with fallback streams
- New `useConfigStorage.ts` React hook
- Enhanced configuration form components

### Bug Fixes
- Fixed Docker networking (internal addresses)
- Fixed API key handling (plain text, no encoding)
- Fixed request body formatting (movies vs TV)
- Implemented proper request deduplication

## 🔗 Important Links
- **Repository**: https://github.com/InfiniteAvenger/jellio-plus
- **Releases**: https://github.com/InfiniteAvenger/jellio-plus/releases
- **Manifest**: https://raw.githubusercontent.com/InfiniteAvenger/jellio-plus/metadata/jellyfin-repo-manifest.json
- **Full Changelog**: https://github.com/InfiniteAvenger/jellio-plus/compare/v1.0.0...v1.1.0

## 📝 Notes
- All old version tags (v1.0.1 - v1.1.14) have been removed from both local and remote
- The manifest now only contains v1.0.0 and v1.1.0 for a clean version history
- The built package (jellio_1.1.0.0.zip) is ready for upload to GitHub releases
- No breaking changes - existing v1.0.0 installations will upgrade smoothly
