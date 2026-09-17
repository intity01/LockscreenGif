namespace LockscreenGif.Core;

public enum FileSizeSeverity
{
    Informational,
    Warning,
    Error,
}

/// <summary>
/// Rough, empirical estimate of how much temporary disk space generating a GIF
/// will require, based on the chosen resolution/fps/trim duration.
/// </summary>
public static class GifFileSizeEstimator
{
    private const double AssumedAspectRatio = 9.0 / 16.0;
    private const double BytesPerPixel = 3; // 24-bit RGB
    private const double AssumedPngCompressionFactor = 0.35; // empirical

    public static double EstimateMegabytes(int width, double fps, double durationSeconds)
    {
        var height = (int)Math.Round(width * AssumedAspectRatio);
        var bytesPerFrame = width * height * BytesPerPixel * AssumedPngCompressionFactor;
        var kbPerFrame = bytesPerFrame / 1024.0;

        var frameCount = durationSeconds * fps;
        var totalMB = frameCount * kbPerFrame / 1024.0;

        return MathHelpers.RoundToSigFigs(totalMB, 2);
    }

    public static FileSizeSeverity GetSeverity(double estimatedMegabytes) => estimatedMegabytes switch
    {
        > 5000 => FileSizeSeverity.Error,
        > 1000 => FileSizeSeverity.Warning,
        _ => FileSizeSeverity.Informational,
    };
}
