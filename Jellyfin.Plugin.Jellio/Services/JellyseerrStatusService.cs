using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Jellio.Helpers;

namespace Jellyfin.Plugin.Jellio.Services;

public static class JellyseerrStatusService
{
    private static readonly ConcurrentDictionary<string, (int TmdbId, DateTime Timestamp)> _tmdbCache = new();
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromHours(24);

    public static async Task<int?> GetTmdbId(
        HttpClient client, string imdbId, string type, string? title)
    {
        var cacheKey = $"{imdbId}:{type}";

        if (_tmdbCache.TryGetValue(cacheKey, out var cached) &&
            DateTime.UtcNow - cached.Timestamp < _cacheTtl)
        {
            return cached.TmdbId;
        }

        if (string.IsNullOrWhiteSpace(title))
            return null;

        var searchUri = $"api/v1/search?query={Uri.EscapeDataString(title)}";
        using var resp = await client.GetAsync(searchUri);

        if (!resp.IsSuccessStatusCode)
            return null;

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStreamAsync());
        if (!doc.RootElement.TryGetProperty("results", out var results) ||
            results.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var el in results.EnumerateArray())
        {
            var mediaType = el.TryGetProperty("mediaType", out var mt) ? mt.GetString() : null;
            if (string.IsNullOrEmpty(mediaType) ||
                !string.Equals(mediaType, type, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (el.TryGetProperty("id", out var idEl) && idEl.GetInt32() is var idVal)
            {
                _tmdbCache[cacheKey] = (idVal, DateTime.UtcNow);
                return idVal;
            }
        }

        return null;
    }

    public static async Task<string> GetMediaStatus(HttpClient client, int tmdbId)
    {
        try
        {
            using var resp = await client.GetAsync($"api/v1/media/{tmdbId}");
            if (!resp.IsSuccessStatusCode)
                return "unknown";

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStreamAsync());
            if (doc.RootElement.TryGetProperty("status", out var statusEl))
            {
                return statusEl.GetInt32() switch
                {
                    0 => "unknown",
                    1 => "pending",
                    2 => "processing",
                    3 => "partially_available",
                    4 => "available",
                    5 => "deleted",
                    _ => "unknown"
                };
            }
        }
        catch
        {
            // Jellyseerr unreachable or malformed response
        }

        return "unknown";
    }
}
