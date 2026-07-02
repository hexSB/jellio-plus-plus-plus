using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.Jellio.Services;

public static class JellyseerrStatusService
{
    private static readonly ConcurrentDictionary<string, (int TmdbId, DateTime Timestamp)> _tmdbCache = new();
    private static readonly ConcurrentDictionary<string, (string Status, DateTime Timestamp)> _localStatusCache = new();
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan _localStatusTtl = TimeSpan.FromMinutes(30);

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
        {
            return null;
        }

        var searchUri = $"api/v1/search?query={Uri.EscapeDataString(title)}";
        using var resp = await client.GetAsync(searchUri);

        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }

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

    public static async Task<string> GetMediaStatus(HttpClient client, int tmdbId, string type)
    {
        try
        {
            var mediaRoute = string.Equals(type, "tv", StringComparison.OrdinalIgnoreCase) ? "tv" : "movie";
            using var resp = await client.GetAsync($"api/v1/{mediaRoute}/{tmdbId}");
            if (!resp.IsSuccessStatusCode)
            {
                return GetLocalStatus(tmdbId, type) ?? "unknown";
            }

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStreamAsync());

            if (doc.RootElement.TryGetProperty("mediaInfo", out var mediaInfo) &&
                mediaInfo.ValueKind == JsonValueKind.Object &&
                mediaInfo.TryGetProperty("status", out var statusEl))
            {
                var status = MapMediaStatus(statusEl.GetInt32());
                if (status != "unknown" && status != "deleted")
                {
                    MarkLocalStatus(tmdbId, type, status);
                }

                return status;
            }
        }
        catch
        {
            // Jellyseerr unreachable or malformed response
        }

        return GetLocalStatus(tmdbId, type) ?? "unknown";
    }

    public static void MarkLocalStatus(int tmdbId, string type, string status)
    {
        _localStatusCache[GetStatusCacheKey(tmdbId, type)] = (status, DateTime.UtcNow);
    }

    internal static string MapMediaStatus(int status)
    {
        return status switch
        {
            1 => "unknown",
            2 => "pending",
            3 => "processing",
            4 => "partially_available",
            5 => "available",
            6 => "deleted",
            _ => "unknown"
        };
    }

    internal static bool HasExistingRequestOrMedia(string status)
    {
        return status is "pending" or "processing" or "partially_available" or "available";
    }

    private static string? GetLocalStatus(int tmdbId, string type)
    {
        var cacheKey = GetStatusCacheKey(tmdbId, type);
        if (_localStatusCache.TryGetValue(cacheKey, out var cached) &&
            DateTime.UtcNow - cached.Timestamp < _localStatusTtl)
        {
            return cached.Status;
        }

        return null;
    }

    private static string GetStatusCacheKey(int tmdbId, string type)
    {
        return $"{type}:{tmdbId.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }
}
