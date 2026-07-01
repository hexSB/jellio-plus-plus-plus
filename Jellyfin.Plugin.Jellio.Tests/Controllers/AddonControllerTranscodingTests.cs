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

    private static MediaSourceInfo CreateSource(
        string codec,
        int width,
        int height,
        int? bitDepth = null)
    {
        return new MediaSourceInfo
        {
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
            ],
        };
    }
}
