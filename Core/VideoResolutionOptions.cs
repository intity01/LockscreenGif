namespace LockscreenGif.Core;

public record VideoResolutionOption(string Label, int Width);

/// <summary>
/// Builds the list of selectable output resolutions offered to the user, based on
/// the source video's width (never upscale beyond the original).
/// </summary>
public static class VideoResolutionOptions
{
    public static IReadOnlyList<VideoResolutionOption> Build(uint videoWidth, uint videoHeight)
    {
        var options = new List<VideoResolutionOption>
        {
            new($"Original ({videoHeight}p)", (int)videoWidth),
        };

        if (videoWidth > 2560)
        {
            options.Add(new VideoResolutionOption("1440p", 2560));
        }

        if (videoWidth > 1920)
        {
            options.Add(new VideoResolutionOption("1080p", 1920));
        }

        if (videoWidth > 1280)
        {
            options.Add(new VideoResolutionOption("720p", 1280));
        }

        if (videoWidth > 854)
        {
            options.Add(new VideoResolutionOption("480p", 854));
        }

        return options;
    }
}
