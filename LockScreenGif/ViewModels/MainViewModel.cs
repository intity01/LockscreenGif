using CommunityToolkit.Mvvm.ComponentModel;
using LockscreenGif.Contracts.Services;
using LockscreenGif.Helpers;

namespace LockscreenGif.ViewModels;

public enum RemoveLockscreenOutcome
{
    Failed,
    PartiallyFailed,
    Succeeded,
}

public record RemoveLockscreenResult(RemoveLockscreenOutcome Outcome, int SuccessfulDeletions, int FailedDeletions);

public record GifGenerationRequest(string VideoPath, TimeSpan Start, TimeSpan End, int Width, double Fps);

/// <summary>
/// Orchestrates the app's two main flows (apply/remove lockscreen, generate GIF from video).
/// Kept free of direct references to XAML-named elements so it can be exercised without a UI:
/// callers (MainPage) are responsible for translating progress/results into control updates.
/// </summary>
public partial class MainViewModel : ObservableRecipient
{
    private readonly ILockscreenService _lockscreenService;
    private readonly IFfmpegService _ffmpegService;
    private readonly IGifSkiService _gifSkiService;
    private readonly IAppNotificationService _notificationService;

    public MainViewModel(
        ILockscreenService lockscreenService,
        IFfmpegService ffmpegService,
        IGifSkiService gifSkiService,
        IAppNotificationService notificationService)
    {
        _lockscreenService = lockscreenService;
        _ffmpegService = ffmpegService;
        _gifSkiService = gifSkiService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Applies the currently selected GIF (<see cref="ILockscreenService.CurrentImage"/>) as the
    /// Windows lockscreen. Shows a success/failure toast notification either way.
    /// </summary>
    public async Task<bool> ApplyLockscreenAsync()
    {
        Logger.Info("Trying to set lockscreen");
        var success = await _lockscreenService.ApplyGifAsLockscreenAsync();
        _gifSkiService.CleanupTempDirectories();

        _notificationService.Show(success
            ? string.Format("AppNotificationSuccess".GetLocalized(), AppContext.BaseDirectory)
            : string.Format("AppNotificationFailure".GetLocalized(), AppContext.BaseDirectory));

        return success;
    }

    /// <summary>
    /// Removes any previously-applied animated lockscreen. Shows a toast notification describing
    /// the outcome; the caller decides whether to additionally show a follow-up dialog.
    /// </summary>
    public async Task<RemoveLockscreenResult> RemoveLockscreenAsync()
    {
        Logger.Info("Trying to delete applied animated lockscreen");
        var result = await _lockscreenService.RemoveAppliedGif();

        if (result is null)
        {
            _notificationService.Show(string.Format("AppNotificationDeleteFailure".GetLocalized(), AppContext.BaseDirectory));
            return new RemoveLockscreenResult(RemoveLockscreenOutcome.Failed, 0, 0);
        }

        if (result.FailedDeletions != 0)
        {
            _notificationService.Show(string.Format(
                "AppNotificationDeletePartialFailure".GetLocalized(),
                AppContext.BaseDirectory,
                result.SuccessfulDeletions,
                result.FailedDeletions));
            return new RemoveLockscreenResult(RemoveLockscreenOutcome.PartiallyFailed, result.SuccessfulDeletions, result.FailedDeletions);
        }

        _notificationService.Show(string.Format("AppNotificationDeleteSuccess".GetLocalized(), AppContext.BaseDirectory));
        return new RemoveLockscreenResult(RemoveLockscreenOutcome.Succeeded, result.SuccessfulDeletions, result.FailedDeletions);
    }

    /// <summary>
    /// Runs the trim -> extract frames -> encode GIF pipeline. Progress callbacks report raw
    /// 0-100% for their respective phase; the caller (View) decides how to weight/display them
    /// and is responsible for any UI-thread marshaling.
    /// </summary>
    public async Task<string> GenerateGifAsync(
        GifGenerationRequest request,
        Action? onTrimCompleted,
        Action<double> onExtractFramesProgress,
        Action<double> onCreateGifProgress)
    {
        Logger.Info($"Starting GIF generation pipeline for {request.VideoPath}");

        var trimmedVideo = await _ffmpegService.TrimVideoAsync(request.VideoPath, request.Start, request.End);
        onTrimCompleted?.Invoke();

        var framesDir = await _ffmpegService.ExtractPngFramesAsync(
            trimmedVideo,
            onExtractFramesProgress,
            request.Width,
            request.Fps,
            request.End - request.Start);

        return await _gifSkiService.CreateGif(framesDir, onCreateGifProgress, request.Fps);
    }
}
