using LockscreenGif.Core;

namespace LockscreenGif.Core.Tests;

public class GifFileSizeEstimatorTests
{
    [Fact]
    public void EstimateMegabytes_ReturnsZero_WhenDurationIsZero()
    {
        var result = GifFileSizeEstimator.EstimateMegabytes(width: 1920, fps: 30, durationSeconds: 0);

        Assert.Equal(0, result);
    }

    [Fact]
    public void EstimateMegabytes_ScalesWithDuration()
    {
        var shortClip = GifFileSizeEstimator.EstimateMegabytes(width: 1280, fps: 30, durationSeconds: 5);
        var longClip = GifFileSizeEstimator.EstimateMegabytes(width: 1280, fps: 30, durationSeconds: 10);

        Assert.True(longClip > shortClip);
    }

    [Theory]
    [InlineData(10, FileSizeSeverity.Informational)]
    [InlineData(1500, FileSizeSeverity.Warning)]
    [InlineData(6000, FileSizeSeverity.Error)]
    public void GetSeverity_ReturnsExpectedThreshold(double estimatedMegabytes, FileSizeSeverity expected)
    {
        var severity = GifFileSizeEstimator.GetSeverity(estimatedMegabytes);

        Assert.Equal(expected, severity);
    }
}
