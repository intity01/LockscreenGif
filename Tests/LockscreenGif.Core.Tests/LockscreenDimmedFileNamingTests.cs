using LockscreenGif.Core;

namespace LockscreenGif.Core.Tests;

public class LockscreenDimmedFileNamingTests
{
    [Fact]
    public void BuildDimmedFileNames_CreatesOneNamePerResolution()
    {
        var result = LockscreenDimmedFileNaming.BuildDimmedFileNames(
            displayResolutions: ["1920_1080", "2560_1440"],
            existingDimmedFileNames: []);

        Assert.Contains("LockScreen___1920_1080_notdimmed.jpg", result);
        Assert.Contains("LockScreen___2560_1440_notdimmed.jpg", result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void BuildDimmedFileNames_MergesExistingFilesWithoutDuplicates()
    {
        var result = LockscreenDimmedFileNaming.BuildDimmedFileNames(
            displayResolutions: ["1920_1080"],
            existingDimmedFileNames: ["LockScreen___1920_1080_notdimmed.jpg", "LockScreen___1921_1081_notdimmed.jpg"]);

        Assert.Equal(2, result.Count);
        Assert.Contains("LockScreen___1920_1080_notdimmed.jpg", result);
        Assert.Contains("LockScreen___1921_1081_notdimmed.jpg", result);
    }

    [Fact]
    public void BuildDimmedFileNames_IgnoresBlankExistingNames()
    {
        var result = LockscreenDimmedFileNaming.BuildDimmedFileNames(
            displayResolutions: [],
            existingDimmedFileNames: ["", "   ", null!]);

        Assert.Empty(result);
    }
}
