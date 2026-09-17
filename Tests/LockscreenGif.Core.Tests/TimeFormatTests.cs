using LockscreenGif.Core;

namespace LockscreenGif.Core.Tests;

public class TimeFormatTests
{
    [Theory]
    [InlineData("1:30.5", 90.5)]
    [InlineData("0:00.0", 0)]
    [InlineData("10:00", 600)]
    public void TryParseMinutesSeconds_ParsesValidInput(string input, double expectedSeconds)
    {
        var success = TimeFormat.TryParseMinutesSeconds(input, out var seconds);

        Assert.True(success);
        Assert.Equal(expectedSeconds, seconds, precision: 3);
    }

    [Theory]
    [InlineData("not-a-time")]
    [InlineData("1:2:3")]
    [InlineData("")]
    [InlineData("abc:def")]
    public void TryParseMinutesSeconds_RejectsInvalidInput(string input)
    {
        var success = TimeFormat.TryParseMinutesSeconds(input, out _);

        Assert.False(success);
    }

    [Fact]
    public void ToPaddedMinutesSeconds_FormatsWithLeadingZero()
    {
        var result = TimeFormat.ToPaddedMinutesSeconds(65.4);

        Assert.Equal("01:05.4", result);
    }

    [Fact]
    public void ToShortMinutesSeconds_FormatsWithoutLeadingZero()
    {
        var result = TimeFormat.ToShortMinutesSeconds(65.4);

        Assert.Equal("1:05.4", result);
    }
}
