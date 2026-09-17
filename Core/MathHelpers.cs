namespace LockscreenGif.Core;

public static class MathHelpers
{
    /// <summary>
    /// Rounds a value to the given number of significant figures.
    /// e.g. RoundToSigFigs(5329, 2) -> 5300
    /// </summary>
    public static double RoundToSigFigs(double value, int digits = 2)
    {
        if (value == 0)
        {
            return 0;
        }

        var abs = Math.Abs(value);
        var exponent = (int)Math.Floor(Math.Log10(abs));
        var scale = Math.Pow(10, exponent - digits + 1);
        return Math.Round(value / scale) * scale;
    }
}
