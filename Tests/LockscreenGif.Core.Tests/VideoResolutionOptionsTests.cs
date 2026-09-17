using LockscreenGif.Core;

namespace LockscreenGif.Core.Tests;

public class VideoResolutionOptionsTests
{
    [Fact]
    public void Build_AlwaysIncludesOriginal_AsFirstOption()
    {
        var options = VideoResolutionOptions.Build(videoWidth: 640, videoHeight: 360);

        Assert.Equal("Original (360p)", options[0].Label);
        Assert.Equal(640, options[0].Width);
    }

    [Fact]
    public void Build_NeverOffersUpscaling()
    {
        // A 640-wide source should not offer 720p/1080p/1440p downscale options either,
        // since it's already below all of those thresholds.
        var options = VideoResolutionOptions.Build(videoWidth: 640, videoHeight: 360);

        Assert.Single(options);
    }

    [Fact]
    public void Build_OffersAllLowerTiers_ForA4KSource()
    {
        var options = VideoResolutionOptions.Build(videoWidth: 3840, videoHeight: 2160);

        Assert.Equal(5, options.Count);
        Assert.Contains(options, o => o.Label == "1440p" && o.Width == 2560);
        Assert.Contains(options, o => o.Label == "1080p" && o.Width == 1920);
        Assert.Contains(options, o => o.Label == "720p" && o.Width == 1280);
        Assert.Contains(options, o => o.Label == "480p" && o.Width == 854);
    }
}
