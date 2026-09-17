namespace LockscreenGif.Core;

public record VideoFpsOption(string Label, double? Fps);

/// <summary>
/// Builds the list of selectable output frame rates offered to the user, based on
/// the source video's frame rate (never upscale beyond the original).
/// </summary>
public static class VideoFpsOptions
{
    public static IReadOnlyList<VideoFpsOption> Build(double? videoFps)
    {
        var options = new List<VideoFpsOption>
        {
            new($"Original ({videoFps:F2} fps)", videoFps),
        };

        if (videoFps > 30)
        {
            options.Add(new VideoFpsOption("30 fps", 30.0));
        }

        if (videoFps > 15)
        {
            options.Add(new VideoFpsOption("15 fps", 15.0));
        }

        if (videoFps > 10)
        {
            options.Add(new VideoFpsOption("10 fps", 10.0));
        }

        if (videoFps > 5)
        {
            options.Add(new VideoFpsOption("5 fps", 5.0));
        }

        return options;
    }
}
