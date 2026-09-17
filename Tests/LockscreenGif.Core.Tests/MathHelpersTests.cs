using LockscreenGif.Core;

namespace LockscreenGif.Core.Tests;

public class MathHelpersTests
{
    [Theory]
    [InlineData(5329, 2, 5300)]
    [InlineData(0, 2, 0)]
    [InlineData(1234, 1, 1000)]
    [InlineData(-5329, 2, -5300)]
    public void RoundToSigFigs_RoundsAsExpected(double value, int digits, double expected)
    {
        var result = MathHelpers.RoundToSigFigs(value, digits);

        Assert.Equal(expected, result);
    }
}
