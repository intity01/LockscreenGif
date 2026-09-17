using LockscreenGif.Core;

namespace LockscreenGif.Core.Tests;

public class VideoFpsOptionsTests
{
    [Fact]
    public void Build_AlwaysIncludesOriginal_AsFirstOption()
    {
        var options = VideoFpsOptions.Build(videoFps: 24.0);

        Assert.Equal("Original (24.00 fps)", options[0].Label);
        Assert.Equal(24.0, options[0].Fps);
    }

    [Fact]
    public void Build_OffersLowerTiersOnly_ForA60FpsSource()
    {
        var options = VideoFpsOptions.Build(videoFps: 60.0);

        Assert.Equal(5, options.Count);
        Assert.Contains(options, o => o.Label == "30 fps" && o.Fps == 30.0);
        Assert.Contains(options, o => o.Label == "15 fps" && o.Fps == 15.0);
        Assert.Contains(options, o => o.Label == "10 fps" && o.Fps == 10.0);
        Assert.Contains(options, o => o.Label == "5 fps" && o.Fps == 5.0);
    }

    [Fact]
    public void Build_OffersNoLowerTiers_ForA5FpsSource()
    {
        var options = VideoFpsOptions.Build(videoFps: 5.0);

        Assert.Single(options);
    }
}
