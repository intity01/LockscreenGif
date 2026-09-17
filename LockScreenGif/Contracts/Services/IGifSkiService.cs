namespace LockscreenGif.Contracts.Services;

public interface IGifSkiService
{
    string CreateTempDirectory();

    void CleanupTempDirectories();

    Task<string> CreateGif(string inputDirectory, Action<double> onPercentageProgress, double frameRate);
}
