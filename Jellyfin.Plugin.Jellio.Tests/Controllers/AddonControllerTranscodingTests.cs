using Jellyfin.Plugin.Jellio.Controllers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.Jellio.Tests.Controllers;

public class AddonControllerTranscodingTests
{
    [Fact]
    public void GetVideoCodecs_Adaptive4KAv1_AllowsJellyfinVideoTranscode()
    {
        var source = CreateSource("av1", width: 3840, height: 2160);

        var codecs = AddonController.GetVideoCodecs(source, "adaptive");

        Assert.Equal(["h264"], codecs);
        Assert.True(AddonController.ShouldForceAdaptiveAv1Transcode(source));
    }

    [Fact]
    public void GetVideoCodecs_Disabled4KAv1_DoesNotForceVideoTranscode()
    {
        var source = CreateSource("av1", width: 3840, height: 2160);

        var codecs = AddonController.GetVideoCodecs(source, "disabled");

        Assert.Equal(["h264", "hevc", "av1"], codecs);
        Assert.True(AddonController.ShouldForceAdaptiveAv1Transcode(source));
    }

    [Fact]
    public void GetVideoCodecs_Adaptive1080pAv1_DoesNotForceVideoTranscode()
    {
        var source = CreateSource("av1", width: 1920, height: 1080);

        var codecs = AddonController.GetVideoCodecs(source, "adaptive");

        Assert.Equal(["h264", "hevc", "av1"], codecs);
        Assert.False(AddonController.ShouldForceAdaptiveAv1Transcode(source));
    }

    [Fact]
    public void GetVideoCodecs_Adaptive4KHevc10Bit_DoesNotForceVideoTranscode()
    {
        var source = CreateSource("hevc", width: 3840, height: 2160, bitDepth: 10);

        var codecs = AddonController.GetVideoCodecs(source, "adaptive");

        Assert.Equal(["h264", "hevc", "av1"], codecs);
        Assert.False(AddonController.ShouldForceAdaptiveAv1Transcode(source));
    }

    [Fact]
    public void GetVideoCodecs_Adaptive4KAvc_DoesNotForceVideoTranscode()
    {
        var source = CreateSource("h264", width: 3840, height: 2160);

        var codecs = AddonController.GetVideoCodecs(source, "adaptive");

        Assert.Equal(["h264", "hevc", "av1"], codecs);
        Assert.False(AddonController.ShouldForceAdaptiveAv1Transcode(source));
    }

    [Fact]
    public void GetAudioCodecs_Adaptive_PrefersAacFallbackBeforeOpus()
    {
        var codecs = AddonController.GetAudioCodecs("adaptive");

        Assert.Equal(["aac", "mp3", "ac3", "eac3", "flac", "opus"], codecs);
    }

    [Fact]
    public void GetAudioCodecs_Force_UsesAacOnly()
    {
        var codecs = AddonController.GetAudioCodecs("force");

        Assert.Equal(["aac"], codecs);
    }

    [Theory]
    [InlineData("adaptive", "adaptive", true)]
    [InlineData("disabled", "disabled", true)]
    [InlineData("force", "adaptive", false)]
    [InlineData("adaptive", "force", false)]
    [InlineData("force", "force", false)]
    public void ShouldEnableDirectPlayback_DisablesDirectPlaybackForForcedModes(
        string videoMode,
        string audioMode,
        bool expected)
    {
        var enabled = AddonController.ShouldEnableDirectPlayback(videoMode, audioMode);

        Assert.Equal(expected, enabled);
    }

    [Fact]
    public void DescribeJellyfinStreamMode_AdaptiveSupportedCodecs_ReportsDirectStream()
    {
        var source = CreateSource("h264", width: 1920, height: 1080, audioCodec: "aac");
        var audioStream = source.MediaStreams.First(stream => stream.Type == MediaStreamType.Audio);

        var mode = AddonController.DescribeJellyfinStreamMode(source, audioStream, "adaptive", "adaptive", 120);

        Assert.Equal("Direct stream/remux expected (no Jellyfin transcoding)", mode);
    }

    [Fact]
    public void DescribeJellyfinStreamMode_Adaptive4KAv1_ReportsVideoTranscode()
    {
        var source = CreateSource("av1", width: 3840, height: 2160, audioCodec: "aac");
        var audioStream = source.MediaStreams.First(stream => stream.Type == MediaStreamType.Audio);

        var mode = AddonController.DescribeJellyfinStreamMode(source, audioStream, "adaptive", "adaptive", 120);

        Assert.Equal("Transcoding expected (video 4K AV1 forced to H.264)", mode);
    }

    [Fact]
    public void DescribeJellyfinStreamMode_ForcedVideoAndAudio_ReportsTranscode()
    {
        var source = CreateSource("h264", width: 1920, height: 1080, audioCodec: "aac");
        var audioStream = source.MediaStreams.First(stream => stream.Type == MediaStreamType.Audio);

        var mode = AddonController.DescribeJellyfinStreamMode(source, audioStream, "force", "force", 120);

        Assert.Equal("Transcoding expected (video forced to H.264; audio forced to AAC)", mode);
    }

    [Fact]
    public void DescribeJellyfinStreamMode_AdaptiveHighBitrate_ReportsTranscode()
    {
        var source = CreateSource("h264", width: 3840, height: 2160, audioCodec: "aac", bitrate: 140000000);
        var audioStream = source.MediaStreams.First(stream => stream.Type == MediaStreamType.Audio);

        var mode = AddonController.DescribeJellyfinStreamMode(source, audioStream, "adaptive", "adaptive", 120);

        Assert.Equal("Transcoding expected (source bitrate 140 Mbps exceeds max 120 Mbps)", mode);
    }

    [Fact]
    public void DescribeJellyfinStreamMode_DisabledUnsupportedAudio_ReportsNoTranscodeWarning()
    {
        var source = CreateSource("h264", width: 1920, height: 1080, audioCodec: "truehd");
        var audioStream = source.MediaStreams.First(stream => stream.Type == MediaStreamType.Audio);

        var mode = AddonController.DescribeJellyfinStreamMode(source, audioStream, "disabled", "disabled", 120);

        Assert.Equal("No transcoding requested (audio transcoding disabled; unsupported codec truehd may fail)", mode);
    }

    private static MediaSourceInfo CreateSource(
        string codec,
        int width,
        int height,
        int? bitDepth = null,
        string? audioCodec = null,
        int? bitrate = null)
    {
        return new MediaSourceInfo
        {
            Bitrate = bitrate,
            MediaStreams =
            [
                new MediaStream
                {
                    Type = MediaStreamType.Video,
                    Codec = codec,
                    Width = width,
                    Height = height,
                    BitDepth = bitDepth,
                },
                new MediaStream
                {
                    Type = MediaStreamType.Audio,
                    Codec = audioCodec ?? "aac",
                    Index = 1,
                },
            ],
        };
    }
}
