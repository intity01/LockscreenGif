using System.Globalization;

namespace LockscreenGif.Core;

/// <summary>
/// Parsing/formatting for the "minutes:seconds.tenths" time representation used
/// throughout the video trimming UI (text boxes and the trim range selector tooltip).
/// </summary>
public static class TimeFormat
{
    /// <summary>
    /// Parses a "m:ss.f" or "mm:ss.f" style string (as typed by the user) into seconds.
    /// </summary>
    public static bool TryParseMinutesSeconds(string text, out double totalSeconds)
    {
        totalSeconds = 0;
        var parts = text.Split(':');
        if (parts.Length == 2
            && int.TryParse(parts[0], out var minutes)
            && double.TryParse(parts[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var seconds))
        {
            totalSeconds = minutes * 60 + seconds;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Formats seconds as "mm:ss.f" (used by the trim text boxes).
    /// </summary>
    public static string ToPaddedMinutesSeconds(double totalSeconds) =>
        TimeSpan.FromSeconds(totalSeconds).ToString(@"mm\:ss\.f", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats seconds as "m:ss.f" (used by the trim range selector tooltip).
    /// </summary>
    public static string ToShortMinutesSeconds(double totalSeconds) =>
        TimeSpan.FromSeconds(totalSeconds).ToString(@"m\:ss\.f", CultureInfo.InvariantCulture);
}
