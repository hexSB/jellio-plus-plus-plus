using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Jellio.Helpers;
using Jellyfin.Plugin.Jellio.Models;
using Jellyfin.Plugin.Jellio.Services;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.Jellio.Controllers;

[ApiController]
[ConfigAuthorize]
[Route("jelliopp/{config}")]
[Produces(MediaTypeNames.Application.Json)]
public class AddonController : ControllerBase
{
    private const string TranscodingModeAdaptive = "adaptive";
    private const string TranscodingModeForce = "force";
    private const string TranscodingModeDisabled = "disabled";

    private static readonly string PluginVersion =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    private readonly IUserManager _userManager;
    private readonly IUserViewManager _userViewManager;
    private readonly IDtoService _dtoService;
    private readonly ILibraryManager _libraryManager;
    private static readonly HttpClient _httpClient = new();

    public AddonController(
        IUserManager userManager,
        IUserViewManager userViewManager,
        IDtoService dtoService,
        ILibraryManager libraryManager
    )
    {
        _userManager = userManager;
        _userViewManager = userViewManager;
        _dtoService = dtoService;
        _libraryManager = libraryManager;
    }

    private async Task<string?> GetTitleFromCinemeta(string imdbId, string type)
    {
        try
        {
            var stremioType = type == "movie" ? "movie" : "series";
            var response = await _httpClient.GetAsync($"https://v3-cinemeta.strem.io/meta/{stremioType}/tt{imdbId}.json");
            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
                if (doc.RootElement.TryGetProperty("meta", out var meta) &&
                    meta.TryGetProperty("name", out var name))
                {
                    return name.GetString();
                }
            }
        }
        catch
        {
            // If Cinemeta fails, we'll just return null
        }

        return null;
    }

    private async Task<IActionResult> GetJellyseerrFallbackStream(
        ConfigModel config, string imdbId, string type, int? season = null, int? episode = null)
    {
        if (!config.JellyseerrEnabled || string.IsNullOrWhiteSpace(config.JellyseerrUrl))
            return Ok(new { streams = Array.Empty<object>() });

        var title = await GetTitleFromCinemeta(imdbId, type);

        using var client = JellyseerrHttpClient.Create(config.JellyseerrUrl, config.JellyseerrApiKey);
        var tmdbId = await JellyseerrStatusService.GetTmdbId(client, imdbId, type, title);

        LogBuffer.AddLog($"[Jellyseerr] IMDB tt{imdbId} -> TMDB {tmdbId?.ToString(CultureInfo.InvariantCulture) ?? "null"}", LogLevel.Info);

        if (tmdbId is null)
        {
            return ReturnJellyseerrRequestStream(config, imdbId, type, title, season, episode);
        }

        var status = await JellyseerrStatusService.GetMediaStatus(client, tmdbId.Value);
        LogBuffer.AddLog($"[Jellyseerr] Media status for TMDB {tmdbId}: {status}", LogLevel.Info);

        return status switch
        {
            "available" => await HandleJellyseerrAvailable(config, imdbId, type, tmdbId.Value, season, episode),
            "processing" => ReturnStatusStream("🔄 Downloading — tap to refresh", "Content is being downloaded"),
            "pending" or "partially_available" => ReturnStatusStream("📥 Requested — waiting for download", "Request approved, download pending"),
            _ => ReturnJellyseerrRequestStream(config, imdbId, type, title, season, episode),
        };
    }

    private async Task<IActionResult> HandleJellyseerrAvailable(
        ConfigModel config, string imdbId, string type, int tmdbId, int? season, int? episode)
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;
        var user = _userManager.GetUserById(userId);
        if (user == null)
            return Unauthorized();

        if (type == "movie")
        {
            var query = new InternalItemsQuery(user)
            {
                HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = $"tt{imdbId}" },
                IncludeItemTypes = [BaseItemKind.Movie],
            };
            var items = _libraryManager.GetItemList(query);
            if (items.Count > 0)
                return GetStreamsResult(userId, items, config.AuthToken, config.PublicBaseUrl);
        }
        else
        {
            var seriesQuery = new InternalItemsQuery(user)
            {
                IncludeItemTypes = [BaseItemKind.Series],
                HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = $"tt{imdbId}" },
            };
            var seriesItems = _libraryManager.GetItemList(seriesQuery);
            if (seriesItems.Count > 0 && season.HasValue && episode.HasValue)
            {
                var seriesIds = seriesItems.Select(s => s.Id).ToArray();
                var episodeQuery = new InternalItemsQuery(user)
                {
                    IncludeItemTypes = [BaseItemKind.Episode],
                    AncestorIds = seriesIds,
                    ParentIndexNumber = season.Value,
                    IndexNumber = episode.Value,
                };
                var episodeItems = _libraryManager.GetItemList(episodeQuery);
                if (episodeItems.Count > 0)
                    return GetStreamsResult(userId, episodeItems, config.AuthToken, config.PublicBaseUrl);
            }
        }

        return ReturnStatusStream("✅ Available — processing in Jellyfin", "Content downloaded, waiting for library scan");
    }

    private IActionResult ReturnJellyseerrRequestStream(
        ConfigModel config, string imdbId, string type, string? title, int? season, int? episode)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Ok(new { streams = Array.Empty<object>() });

        var baseUrl = GetBaseUrl(config.PublicBaseUrl);
        var configStr = Request.RouteValues["config"]?.ToString() ?? "";
        var url = $"{baseUrl}/jelliopp/{configStr}/jellyseerr?type={type}&imdbId=tt{imdbId}&title={Uri.EscapeDataString(title)}";
        if (season.HasValue)
            url += $"&season={season.Value}";
        if (episode.HasValue)
            url += $"&episode={episode.Value}";

        var streams = new[] { new { url, name = "📥 Request via Jellyseerr", description = "Click to send request to Jellyseerr" } };
        return Ok(new { streams });
    }

    private IActionResult ReturnStatusStream(string name, string description)
    {
        var streams = new[] { new { url = "#", name, description } };
        return Ok(new { streams });
    }

    private string GetBaseUrl(string? overrideBaseUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(overrideBaseUrl))
        {
            return overrideBaseUrl!.TrimEnd('/');
        }

        return $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
    }

    private static string GetAudioStreamLabel(MediaStream? audioStream)
    {
        if (audioStream == null)
        {
            return "Default audio";
        }

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(audioStream.Language))
        {
            parts.Add(audioStream.Language);
        }

        if (!string.IsNullOrWhiteSpace(audioStream.Title))
        {
            parts.Add(audioStream.Title);
        }

        if (!string.IsNullOrWhiteSpace(audioStream.Codec))
        {
            parts.Add(audioStream.Codec.ToUpperInvariant());
        }

        if (audioStream.Channels.HasValue)
        {
            parts.Add(audioStream.Channels.Value == 6 ? "5.1" : $"{audioStream.Channels.Value} ch");
        }

        if (audioStream.IsDefault)
        {
            parts.Add("Default");
        }

        return parts.Count == 0 ? $"Audio track {audioStream.Index}" : string.Join(" - ", parts);
    }

    private static string NormalizeVideoTranscodingMode(
        string? mode,
        bool forceTranscodeVideo,
        bool enableDirectStreaming)
    {
        if (IsSupportedTranscodingMode(mode))
        {
            return mode!;
        }

        return forceTranscodeVideo || !enableDirectStreaming
            ? TranscodingModeForce
            : TranscodingModeAdaptive;
    }

    private static string NormalizeAudioTranscodingMode(string? mode, bool forceTranscodeAudio)
    {
        if (IsSupportedTranscodingMode(mode))
        {
            return mode!;
        }

        return forceTranscodeAudio ? TranscodingModeForce : TranscodingModeAdaptive;
    }

    private static bool IsSupportedTranscodingMode(string? mode)
    {
        return mode is TranscodingModeAdaptive or TranscodingModeForce or TranscodingModeDisabled;
    }

    private static string DescribeVideoTarget(string mode)
    {
        return mode switch
        {
            TranscodingModeForce => "H.264 (forced transcode)",
            TranscodingModeDisabled => "Direct stream only",
            _ => "Adaptive direct stream/transcode",
        };
    }

    private static string DescribeAudioTarget(string mode)
    {
        return mode switch
        {
            TranscodingModeForce => "AAC (forced transcode)",
            TranscodingModeDisabled => "direct stream only",
            _ => "adaptive direct stream/transcode",
        };
    }

    internal static string[] GetVideoCodecs(MediaSourceInfo source, string videoTranscodingMode)
    {
        if (videoTranscodingMode == TranscodingModeForce)
        {
            return ["h264"];
        }

        return videoTranscodingMode == TranscodingModeAdaptive
            && ShouldForceAdaptiveAv1Transcode(source)
            ? ["h264"]
            : ["h264", "hevc", "av1"];
    }

    internal static string[] GetAudioCodecs(string audioTranscodingMode)
    {
        if (audioTranscodingMode == TranscodingModeForce)
        {
            return ["aac"];
        }

        return ["aac", "mp3", "ac3", "eac3", "flac", "opus"];
    }

    internal static bool ShouldForceAdaptiveAv1Transcode(MediaSourceInfo source)
    {
        var videoStream = source.MediaStreams
            .FirstOrDefault(stream => stream.Type == MediaStreamType.Video);

        if (!IsAv1Codec(videoStream?.Codec))
        {
            return false;
        }

        return (videoStream?.Height ?? 0) > 1080
            || (videoStream?.Width ?? 0) > 1920;
    }

    private static bool IsAv1Codec(string? codec)
    {
        return string.Equals(codec, "av1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(codec, "av01", StringComparison.OrdinalIgnoreCase);
    }

    private static MetaDto MapToMeta(
        BaseItemDto dto,
        StremioType stremioType,
        string baseUrl,
        bool includeDetails = false
    )
    {
        string? releaseInfo = null;
        if (dto.PremiereDate.HasValue)
        {
            var premiereYear = dto.PremiereDate.Value.Year.ToString(CultureInfo.InvariantCulture);
            releaseInfo = premiereYear;
            if (stremioType == StremioType.Series)
            {
                releaseInfo += "-";
                if (dto.Status != "Continuing" && dto.EndDate.HasValue)
                {
                    var endYear = dto.EndDate.Value.Year.ToString(CultureInfo.InvariantCulture);
                    if (premiereYear != endYear)
                    {
                        releaseInfo += endYear;
                    }
                }
            }
        }

        var meta = new MetaDto
        {
            Id = dto.ProviderIds.TryGetValue("Imdb", out var idVal) ? idVal : $"jelliopp:{dto.Id}",
            Type = stremioType.ToString().ToLower(CultureInfo.InvariantCulture),
            Name = dto.Name,
            Poster = $"{baseUrl}/Items/{dto.Id}/Images/Primary",
            PosterShape = "poster",
            Genres = dto.Genres,
            Description = dto.Overview,
            ImdbRating = dto.CommunityRating?.ToString("F1", CultureInfo.InvariantCulture),
            ReleaseInfo = releaseInfo,
        };

        if (includeDetails)
        {
            meta.Runtime =
                dto.RunTimeTicks.HasValue && dto.RunTimeTicks.Value != 0
                    ? $"{dto.RunTimeTicks.Value / 600000000} min"
                    : null;
            meta.Logo = dto.ImageTags.ContainsKey(ImageType.Logo)
                ? $"{baseUrl}/Items/{dto.Id}/Images/Logo"
                : null;
            meta.Background =
                dto.BackdropImageTags.Length != 0
                    ? $"{baseUrl}/Items/{dto.Id}/Images/Backdrop/0"
                    : null;
            meta.Released = dto.PremiereDate?.ToString("o");
        }

        return meta;
    }

    private OkObjectResult GetStreamsResult(Guid userId, IReadOnlyList<BaseItem> items, string authToken, string? publicBaseUrl = null)
    {
        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            LogBuffer.AddLog($"[Stream] User not found: {userId}", LogLevel.Warning);
            return Ok(new { streams = Array.Empty<object>() });
        }

        LogBuffer.AddLog($"[Stream] Processing {items.Count} item(s) for user {user.Username}", LogLevel.Info);
        var baseUrl = GetBaseUrl(publicBaseUrl);
        LogBuffer.AddLog($"[Stream] Base URL: {baseUrl}", LogLevel.Info);
        var dtoOptions = new DtoOptions(true);
        var dtos = _dtoService.GetBaseItemDtos(items, dtoOptions, user, null, false);
        LogBuffer.AddLog($"[Stream] Got {dtos.Count} DTO(s)", LogLevel.Info);

        var streams = dtos.SelectMany(dto =>
        {
            int mediaSourceCount = 0;
            if (dto.MediaSources != null)
            {
                mediaSourceCount = dto.MediaSources.Count();
            }

            LogBuffer.AddLog($"[Stream] Processing DTO: {dto.Name} (Id: {dto.Id}, MediaSources: {mediaSourceCount})", LogLevel.Info);
            if (dto.MediaSources == null)
            {
                return Enumerable.Empty<StreamDto>();
            }

            return dto.MediaSources.SelectMany(source =>
            {
                var audioStreams = source.MediaStreams
                    .Where(stream => stream.Type == MediaStreamType.Audio)
                    .OrderByDescending(stream => stream.IsDefault)
                    .ThenBy(stream => stream.Index)
                    .ToList();
                var streamChoices = audioStreams.Count == 0
                    ? new MediaStream?[] { null }
                    : audioStreams.Cast<MediaStream?>().ToArray();

                var subtitleStreams = source.MediaStreams
                    .Where(stream => stream.Type == MediaStreamType.Subtitle && stream.IsTextSubtitleStream)
                    .OrderByDescending(stream => stream.IsDefault)
                    .ThenByDescending(stream => stream.IsForced)
                    .ThenBy(stream => stream.Index)
                    .ToList();

                var subtitles = subtitleStreams.Select(sub =>
                {
                    var format = sub.Codec switch
                    {
                        "subrip" => "srt",
                        "ass" => "ass",
                        "webvtt" => "vtt",
                        _ => "srt",
                    };
                    var lang = string.IsNullOrEmpty(sub.Language) ? "und" : sub.Language;
                    var label = string.IsNullOrEmpty(sub.Title) ? lang : sub.Title;
                    return new SubtitleDto
                    {
                        Id = $"jelliopp-{dto.Id}-{sub.Index}",
                        Url = $"{baseUrl}/Videos/{dto.Id}/{source.Id}/Subtitles/{sub.Index}/0/Stream.{format}?api_key={authToken}",
                        Lang = $"{label} ({lang})",
                    };
                }).ToList();

                /*
                 * Adaptive mode preserves original quality when possible.
                 * Jellyfin transcodes unsupported tracks only when the matching
                 * video/audio transcoding mode allows it.
                 * Video codecs: H.264, HEVC, AV1 up to 1080p
                 * Audio codecs keep AAC first so unsupported tracks such as
                 * TRUEHD transcode to a TV-safe fallback instead of Opus.
                 */

                var pluginConfig = Plugin.Instance?.Configuration;
                var enableDirectStreaming = pluginConfig?.EnableDirectStreaming ?? true;
                var forceTranscodeVideo = pluginConfig?.ForceTranscodeVideo ?? false;
                var forceTranscodeAudio = pluginConfig?.ForceTranscodeAudio ?? false;
                var maxVideoBitrate = pluginConfig?.MaxVideoBitrate ?? 120;
                var videoTranscodingMode = NormalizeVideoTranscodingMode(
                    pluginConfig?.VideoTranscodingMode,
                    forceTranscodeVideo,
                    enableDirectStreaming);
                var audioTranscodingMode = NormalizeAudioTranscodingMode(
                    pluginConfig?.AudioTranscodingMode,
                    forceTranscodeAudio);
                var allowVideoStreamCopy = videoTranscodingMode != TranscodingModeForce;
                var allowAudioStreamCopy = audioTranscodingMode != TranscodingModeForce;
                var enableVideoPlaybackTranscoding = videoTranscodingMode != TranscodingModeDisabled;
                var enableAudioPlaybackTranscoding = audioTranscodingMode != TranscodingModeDisabled;

                var audioCodecs = GetAudioCodecs(audioTranscodingMode);

                return streamChoices.Select(audioStream =>
                {
                    var videoCodecs = GetVideoCodecs(source, videoTranscodingMode);
                    var queryParameters = new Dictionary<string, string?>
                    {
                        ["mediaSourceId"] = source.Id,
                        ["api_key"] = authToken,
                        ["videoCodec"] = string.Join(',', videoCodecs),
                        ["audioCodec"] = string.Join(',', audioCodecs),
                        ["enableDirectPlay"] = "true",
                        ["enableDirectStream"] = "true",
                        ["enableVideoPlaybackTranscoding"] = enableVideoPlaybackTranscoding ? "true" : "false",
                        ["enableAudioPlaybackTranscoding"] = enableAudioPlaybackTranscoding ? "true" : "false",
                        ["allowVideoStreamCopy"] = allowVideoStreamCopy ? "true" : "false",
                        ["allowAudioStreamCopy"] = allowAudioStreamCopy ? "true" : "false",
                        ["videoBitRate"] = $"{maxVideoBitrate * 1000000}",
                        ["maxWidth"] = "3840",
                        ["maxHeight"] = "2160",
                    };

                    if (audioStream != null)
                    {
                        queryParameters["audioStreamIndex"] = audioStream.Index.ToString(CultureInfo.InvariantCulture);
                    }

                    var audioLabel = GetAudioStreamLabel(audioStream);
                    var streamUrl = $"{baseUrl}/Videos/{dto.Id}/master.m3u8{QueryString.Create(queryParameters)}";

                    // Enhanced logging with codec info
                    var sourceVideoCodec = source.MediaStreams
                        .FirstOrDefault(s => s.Type == MediaStreamType.Video)?.Codec ?? "unknown";
                    var sourceAudioCodec = audioStream?.Codec ?? "unknown";

                    LogBuffer.AddLog($"[Stream] Generated stream for {dto.Name} ({dto.Id}):", LogLevel.Info);
                    LogBuffer.AddLog($"[Stream]   Source video: {sourceVideoCodec}, Source audio: {sourceAudioCodec}", LogLevel.Info);

                    var targetVideo = DescribeVideoTarget(videoTranscodingMode);
                    LogBuffer.AddLog($"[Stream]   Target video: {targetVideo}", LogLevel.Info);

                    var targetAudio = DescribeAudioTarget(audioTranscodingMode);
                    LogBuffer.AddLog($"[Stream]   Target audio: {audioLabel} ({targetAudio})", LogLevel.Info);

                    LogBuffer.AddLog($"[Stream]   URL: {streamUrl}", LogLevel.Info);

                    return new StreamDto
                    {
                        Url = streamUrl,
                        Name = $"Jellio++ - {audioLabel}",
                        Description = source.Name,
                        BehaviorHints = new BehaviorHintsDto
                        {
                            Filename = string.IsNullOrEmpty(source.Path) ? null : Path.GetFileName(source.Path),
                            VideoSize = source.Size,
                            VideoHash = OpenSubtitlesHash.ComputeFromPath(source.Path),
                            NotWebReady = true,
                        },
                        Subtitles = subtitles.Count > 0 ? subtitles : null,
                    };
                });
            });
        }).ToList();

        LogBuffer.AddLog($"[Stream] Returning {streams.Count} stream(s)", LogLevel.Info);
        return Ok(new { streams });
    }

    [HttpGet("manifest.json")]
    public IActionResult GetManifest([ConfigFromBase64Json] ConfigModel config)
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;

        var userLibraries = LibraryHelper.GetUserLibraries(userId, _userManager, _userViewManager, _dtoService);
        userLibraries = Array.FindAll(userLibraries, l => config.LibrariesGuids.Contains(l.Id));
        if (userLibraries.Length != config.LibrariesGuids.Count)
        {
            return NotFound();
        }

        var catalogs = userLibraries.Select(lib =>
        {
            return new
            {
                type = lib.CollectionType switch
                {
                    CollectionType.movies => "movie",
                    CollectionType.tvshows => "series",
                    _ => null,
                },
                id = lib.Id.ToString(),
                name = $"{lib.Name} | {config.ServerName}",
                extra = new[]
                {
                    new { name = "skip", isRequired = false },
                    new { name = "search", isRequired = false },
                },
            };
        });

        var catalogNames = userLibraries.Select(l => l.Name).ToList();
        var descriptionText = $"Play movies and series from {config.ServerName}: {string.Join(", ", catalogNames)}";
        var manifest = new
        {
            id = "com.stremio.jelliopp",
            version = PluginVersion,
            name = "Jellio++",
            description = descriptionText,
            resources = new object[]
            {
                "catalog",
                "stream",
                new
                {
                    name = "meta",
                    types = new[] { "movie", "series" },
                    idPrefixes = new[] { "jelliopp" },
                },
            },
            types = new[] { "movie", "series" },
            idPrefixes = new[] { "tt", "jelliopp" },
            contactEmail = "support@jellio.stream",
            behaviorHints = new { configurable = true },
            catalogs,
        };

        return Ok(manifest);
    }

    [HttpGet("catalog/{stremioType}/{catalogId:guid}/{extra}.json")]
    [HttpGet("catalog/{stremioType}/{catalogId:guid}.json")]
    public IActionResult GetCatalog(
        [ConfigFromBase64Json] ConfigModel config,
        StremioType stremioType,
        Guid catalogId,
        string? extra = null
    )
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;

        var userLibraries = LibraryHelper.GetUserLibraries(userId, _userManager, _userViewManager, _dtoService);
        var catalogLibrary = Array.Find(userLibraries, l => l.Id == catalogId);
        if (catalogLibrary == null)
        {
            return NotFound();
        }

        var item = _libraryManager.GetParentItem(catalogLibrary.Id, userId);
        if (item is not Folder folder)
        {
            folder = _libraryManager.GetUserRootFolder();
        }

        var extras =
            extra
                ?.Split('&')
                .Select(e => e.Split('='))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1])
            ?? new Dictionary<string, string>();

        int startIndex =
            extras.TryGetValue("skip", out var skipValue)
            && int.TryParse(skipValue, out var parsedSkip)
                ? parsedSkip
                : 0;
        extras.TryGetValue("search", out var searchTerm);

        var dtoOptions = new DtoOptions
        {
            Fields = [ItemFields.ProviderIds, ItemFields.Overview, ItemFields.Genres],
        };

        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var query = new InternalItemsQuery(user)
        {
            Recursive = true, // need this for search to work
            IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series],
            Limit = 100,
            StartIndex = startIndex,
            SearchTerm = searchTerm,
            ParentId = catalogLibrary.Id,
            DtoOptions = dtoOptions,
        };
        var result = folder.GetItems(query);
        var dtos = _dtoService.GetBaseItemDtos(result.Items, dtoOptions, user, null, true);
        var baseUrl = GetBaseUrl(config.PublicBaseUrl);
        var metas = dtos.Select(dto => MapToMeta(dto, stremioType, baseUrl));

        return Ok(new { metas });
    }

    [HttpGet("meta/{stremioType}/jelliopp:{mediaId:guid}.json")]
    public IActionResult GetMeta(
        [ConfigFromBase64Json] ConfigModel config,
        StremioType stremioType,
        Guid mediaId
    )
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;

        var item = _libraryManager.GetItemById<BaseItem>(mediaId, userId);
        if (item == null)
        {
            return NotFound();
        }

        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var dtoOptions = new DtoOptions
        {
            Fields = [ItemFields.ProviderIds, ItemFields.Overview, ItemFields.Genres],
        };
        var dto = _dtoService.GetBaseItemDto(item, dtoOptions, user);
        var baseUrl = GetBaseUrl(config.PublicBaseUrl);
        var meta = MapToMeta(dto, stremioType, baseUrl, includeDetails: true);

        if (stremioType is StremioType.Series)
        {
            if (item is not Series series)
            {
                return BadRequest();
            }

            var episodes = series.GetEpisodes(user, dtoOptions, false).ToList();
            var seriesItemOptions = new DtoOptions { Fields = [ItemFields.Overview] };
            var dtos = _dtoService.GetBaseItemDtos(episodes, seriesItemOptions, user, null, true);
            var videos = dtos.Select(episode => new VideoDto
            {
                Id = $"jelliopp:{episode.Id}",
                Title = episode.Name,
                Thumbnail = $"{baseUrl}/Items/{episode.Id}/Images/Primary",
                Available = true,
                Episode = episode.IndexNumber ?? 0,
                Season = episode.ParentIndexNumber ?? 0,
                Overview = episode.Overview,
                Released = episode.PremiereDate?.ToString("o"),
            });
            meta.Videos = videos;
        }

        return Ok(new { meta });
    }

    [HttpGet("stream/{stremioType}/jelliopp:{mediaId:guid}.json")]
    public IActionResult GetStream(
        [ConfigFromBase64Json] ConfigModel config,
        StremioType stremioType,
        Guid mediaId
    )
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;
        LogBuffer.AddLog($"[Stream] Stream request for {stremioType} with ID: {mediaId}", LogLevel.Info);

        var item = _libraryManager.GetItemById<BaseItem>(mediaId, userId);
        if (item == null)
        {
            LogBuffer.AddLog($"[Stream] Item not found: {mediaId}", LogLevel.Warning);
            // If the item isn't in the library, we can't resolve provider IDs here.
            // Let Stremio fall back to IMDB-based stream routes which include IDs for request flow.
            return Ok(new { streams = Array.Empty<object>() });
        }

        LogBuffer.AddLog($"[Stream] Found item: {item.Name} (Type: {item.GetType().Name}, Id: {item.Id})", LogLevel.Info);
        var result = GetStreamsResult(userId, [item], config.AuthToken, config.PublicBaseUrl);
        LogBuffer.AddLog($"[Stream] Returning stream result for {item.Name}", LogLevel.Info);
        return result;
    }

    [HttpGet("stream/movie/tt{imdbId}.json")]
    public async Task<IActionResult> GetStreamImdbMovie(
        [ConfigFromBase64Json] ConfigModel config,
        string imdbId
    )
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;

        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            return Unauthorized();
        }

        var query = new InternalItemsQuery(user)
        {
            HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = $"tt{imdbId}" },
            IncludeItemTypes = [BaseItemKind.Movie],
        };
        var items = _libraryManager.GetItemList(query);

        if (items.Count == 0)
        {
            return await GetJellyseerrFallbackStream(config, imdbId, "movie");
        }

        return GetStreamsResult(userId, items, config.AuthToken, config.PublicBaseUrl);
    }

    [HttpGet("stream/series/tt{imdbId}:{seasonNum:int}:{episodeNum:int}.json")]
    public async Task<IActionResult> GetStreamImdbTv(
        [ConfigFromBase64Json] ConfigModel config,
        string imdbId,
        int seasonNum,
        int episodeNum
    )
    {
        var userId = (Guid)HttpContext.Items["JellioUserId"]!;
        LogBuffer.AddLog($"[Stream] TV Episode request: IMDB={imdbId}, Season={seasonNum}, Episode={episodeNum}", LogLevel.Info);

        var user = _userManager.GetUserById(userId);
        if (user == null)
        {
            LogBuffer.AddLog($"[Stream] User not found: {userId}", LogLevel.Warning);
            return Unauthorized();
        }

        var seriesQuery = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Series],
            HasAnyProviderId = new Dictionary<string, string> { ["Imdb"] = $"tt{imdbId}" },
        };
        var seriesItems = _libraryManager.GetItemList(seriesQuery);
        LogBuffer.AddLog($"[Stream] Found {seriesItems.Count} series with IMDB tt{imdbId}", LogLevel.Info);

        if (seriesItems.Count == 0)
        {
            LogBuffer.AddLog($"[Stream] Series not found for IMDB tt{imdbId}", LogLevel.Warning);
            return await GetJellyseerrFallbackStream(config, imdbId, "tv", seasonNum, episodeNum);
        }

        var seriesIds = seriesItems.Select(s => s.Id).ToArray();
        LogBuffer.AddLog($"[Stream] Series IDs: {string.Join(", ", seriesIds)}", LogLevel.Info);

        var episodeQuery = new InternalItemsQuery(user)
        {
            IncludeItemTypes = [BaseItemKind.Episode],
            AncestorIds = seriesIds,
            ParentIndexNumber = seasonNum,
            IndexNumber = episodeNum,
        };
        var episodeItems = _libraryManager.GetItemList(episodeQuery);
        LogBuffer.AddLog($"[Stream] Found {episodeItems.Count} episode(s) for Season {seasonNum}, Episode {episodeNum}", LogLevel.Info);

        if (episodeItems.Count == 0)
        {
            LogBuffer.AddLog($"[Stream] Episode not found: Season {seasonNum}, Episode {episodeNum}", LogLevel.Warning);
            return await GetJellyseerrFallbackStream(config, imdbId, "tv", seasonNum, episodeNum);
        }

        LogBuffer.AddLog($"[Stream] Returning streams for {episodeItems.Count} episode(s)", LogLevel.Info);
        return GetStreamsResult(userId, episodeItems, config.AuthToken, config.PublicBaseUrl);
    }
}
