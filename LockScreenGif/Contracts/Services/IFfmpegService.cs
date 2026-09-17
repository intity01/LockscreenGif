namespace LockscreenGif.Contracts.Services;

public interface IFfmpegService
{
    string CreateTempDirectory();

    void CleanupTempDirectories();

    Task<string> ApplyFastStartAsync(string inputFile);

    Task<string> TrimVideoAsync(string inputFile, TimeSpan startTime, TimeSpan endTime);

    Task<string> ExtractPngFramesAsync(
        string videoInput,
        Action<double> onPercentageProgress,
        int width,
        double fps,
        TimeSpan videoDuration);
}
