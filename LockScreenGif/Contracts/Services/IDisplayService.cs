namespace LockscreenGif.Contracts.Services;

public interface IDisplayService
{
    IEnumerable<string> GetDisplayResolutions();
}
