using System.Runtime.InteropServices;
using CommunityToolkit.WinUI.Controls;
using LockscreenGif.Contracts.Services;
using LockscreenGif.Core;
using LockscreenGif.Helpers;
using LockscreenGif.Services;
using LockscreenGif.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;
using Windows.Media.Editing;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT;

namespace LockscreenGif.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    private readonly ILockscreenService _lockscreenService;
    private readonly IAppNotificationService _notificationService;
    private readonly IFfmpegService _ffmpegService;

    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _lockscreenModeTimer;
    private LockscreenService.LockScreenMode _lastLockScreenMode = LockscreenService.LockScreenMode.Unknown;

    private StorageFile? _videoFile;
    private double _startSec;
    private double _endSec;
    private uint _videoWidth;
    private uint _videoHeight;
    private double? _videoFps;
    private bool _correctingPosition;
    private MediaPlaybackSession? _session;

    private void Seek(double sec)
    {
        if (_session != null)
        {
            _session.Position = TimeSpan.FromSeconds(sec);
        }
    }

    // COM interop for file pickers
    [ComImport, Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitializeWithWindow
    {
        void Initialize([In] IntPtr hwnd);
    }

    [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, PreserveSig = true)]
    public static extern IntPtr GetActiveWindow();

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        _lockscreenService = App.GetService<ILockscreenService>();
        _notificationService = App.GetService<IAppNotificationService>();
        _ffmpegService = App.GetService<IFfmpegService>();
        InitializeComponent();

        StartLockscreenModePolling();

        Unloaded += (_, _) => StopLockscreenModePolling();
    }

    private void StartLockscreenModePolling()
    {
        _lockscreenModeTimer ??= DispatcherQueue.CreateTimer();
        _lockscreenModeTimer.Interval = TimeSpan.FromSeconds(5);
        _lockscreenModeTimer.IsRepeating = true;
        _lockscreenModeTimer.Tick -= LockscreenModeTimer_Tick;
        _lockscreenModeTimer.Tick += LockscreenModeTimer_Tick;

        RefreshLockscreenModeUi();
        _lockscreenModeTimer.Start();
    }

    private void StopLockscreenModePolling()
    {
        if (_lockscreenModeTimer is null)
        {
            return;
        }

        _lockscreenModeTimer.Stop();
        _lockscreenModeTimer.Tick -= LockscreenModeTimer_Tick;
        _lockscreenModeTimer = null;
    }

    private void LockscreenModeTimer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        RefreshLockscreenModeUi();
    }

    private void RefreshLockscreenModeUi()
    {
        var mode = LockscreenService.TryGetLockScreenMode();
        if (mode == _lastLockScreenMode)
        {
            return;
        }

        _lastLockScreenMode = mode;

        var ok = mode is LockscreenService.LockScreenMode.PictureOrOther;

        if (PrereqWarningIcon != null)
        {
            PrereqWarningIcon.Visibility = ok ? Visibility.Collapsed : Visibility.Visible;
        }

        if (LockscreenModeWarning != null)
        {
            LockscreenModeWarning.IsOpen = !ok;
        }

        if (PrereqStep1Badge != null)
        {
            PrereqStep1Badge.Background = ok
                ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
                : new SolidColorBrush(Colors.OrangeRed);
        }

        if (PrereqStep1Title != null)
        {
            PrereqStep1Title.Foreground = ok
                ? (Brush)Application.Current.Resources["TextFillColorPrimaryBrush"]
                : new SolidColorBrush(Colors.OrangeRed);
        }

        if (PrereqStep1Body != null)
        {
            PrereqStep1Body.Foreground = ok
                ? (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
                : new SolidColorBrush(Colors.OrangeRed);
        }
    }

    private async void OpenGifButton_click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.As<IInitializeWithWindow>().Initialize(GetActiveWindow());
        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".gif");

        var file = await picker.PickSingleFileAsync();
        if (file != null && file.ContentType == "image/gif")
        {
            _lockscreenService.CurrentImage = file;
            currentImage.Source = _lockscreenService.CurrentImageBitmap!;
            ApplyButton.IsEnabled = true;
        }
    }

    private void UpdateFileSizeWarning()
    {
        var width = (int)((ComboBoxItem)ComboResolution.SelectedItem).Tag;   // e.g. 480, 720, 1080
        var fps = (double?)((ComboBoxItem)ComboFps.SelectedItem).Tag ?? 0;

        var durationSec = _endSec - _startSec;
        var estimatedMB = GifFileSizeEstimator.EstimateMegabytes(width, fps, durationSec);

        FileSizeWarning.Message =
            $"Generating the GIF may temporarily take up to {estimatedMB} MB of space to generate the GIF. " +
            "Ensure you have enough space free.";
        FileSizeWarning.IsOpen = true;
        FileSizeWarning.Severity = GifFileSizeEstimator.GetSeverity(estimatedMB) switch
        {
            FileSizeSeverity.Error => InfoBarSeverity.Error,
            FileSizeSeverity.Warning => InfoBarSeverity.Warning,
            _ => InfoBarSeverity.Informational,
        };
    }

    private async void SetLockscreenButton_click(object sender, RoutedEventArgs e)
    {
        ApplyButton.IsEnabled = false;
        await ViewModel.ApplyLockscreenAsync();
    }

    private async void RemoveAnimatedLockscreenButton_click(object sender, RoutedEventArgs e)
    {
        var result = await ViewModel.RemoveLockscreenAsync();
        if (result.Outcome == RemoveLockscreenOutcome.Succeeded)
        {
            var dialog = new ContentDialog
            {
                Title = "Removal Successful",
                Content = "You may need to lock and unlock your device before reapplying a GIF for it to take effect.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };

            await dialog.ShowAsync();
        }
    }

    private async void OpenVideoButton_click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.As<IInitializeWithWindow>().Initialize(GetActiveWindow());
        picker.ViewMode = PickerViewMode.Thumbnail;
        picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
        picker.FileTypeFilter.Add(".mp4");
        picker.FileTypeFilter.Add(".mkv");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            _videoFile = file;
            (_videoFps, _videoWidth, _videoHeight) = await GetVideoInfoAsync(file);

            PopulateResolutionList();
            PopulateFpsList();

            ShowVideoUi();
            GenerateLoading.Visibility = Visibility.Collapsed;


            VideoPreview.SetMediaPlayer(new MediaPlayer());
            VideoPreview.MediaPlayer.MediaOpened += VideoPreview_MediaOpened;
            _session = VideoPreview.MediaPlayer.PlaybackSession;
            _session.PositionChanged += Session_PositionChanged;
            VideoPreview.MediaPlayer.IsMuted = true;
            VideoPreview.MediaPlayer.Source = MediaSource.CreateFromStorageFile(file);

            GenerateButton.IsEnabled = false;
        }
    }

    private void HideVideoUi()
    {
        VideoPreview.Visibility = Visibility.Collapsed;
        TrimControlsPanel.Visibility = Visibility.Collapsed;
        ComboSettingsStack.Visibility = Visibility.Collapsed;
        ComboFps.Visibility = Visibility.Collapsed;
        FileSizeWarning.IsOpen = false;
    }

    private void ShowVideoUi()
    {
        VideoPreview.Visibility = Visibility.Visible;
        TrimControlsPanel.Visibility = Visibility.Visible;
        ComboSettingsStack.Visibility = Visibility.Visible;
        ComboFps.Visibility = Visibility.Visible;
    }

    private void PopulateResolutionList()
    {
        ComboResolution.Items.Clear();
        foreach (var option in VideoResolutionOptions.Build(_videoWidth, _videoHeight))
        {
            ComboResolution.Items.Add(new ComboBoxItem { Content = option.Label, Tag = option.Width });
        }
        ComboResolution.SelectedIndex = 0;
    }

    private void PopulateFpsList()
    {
        ComboFps.Items.Clear();
        foreach (var option in VideoFpsOptions.Build(_videoFps))
        {
            ComboFps.Items.Add(new ComboBoxItem { Content = option.Label, Tag = option.Fps });
        }
        ComboFps.SelectedIndex = 0;
    }

    private void StartTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (TryParseTime(StartTimeTextBox.Text, out var sec))
        {
            sec = Math.Max(0, Math.Min(sec, TrimSelector.Maximum));
            _startSec = sec;
            TrimSelector.RangeStart = _startSec;
            StartTimeTextBox.Text = TimeFormat.ToPaddedMinutesSeconds(_startSec);
            if (_session?.Position.TotalSeconds < _startSec)
            {
                Seek(_startSec);
            }
        }
        else
        {
            StartTimeTextBox.Text = TimeFormat.ToPaddedMinutesSeconds(_startSec);
        }
        UpdateFileSizeWarning();

    }

    private void EndTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (TryParseTime(EndTimeTextBox.Text, out var sec))
        {
            sec = Math.Max(0, Math.Min(sec, TrimSelector.Maximum));
            _endSec = sec;
            TrimSelector.RangeEnd = _endSec;
            EndTimeTextBox.Text = TimeFormat.ToPaddedMinutesSeconds(_endSec);
            if (_session?.Position.TotalSeconds > _endSec)
            {
                Seek(_startSec);
            }
        }
        else
        {
            EndTimeTextBox.Text = TimeFormat.ToPaddedMinutesSeconds(_endSec);
        }
        UpdateFileSizeWarning();

    }

    private void VideoPreview_MediaOpened(MediaPlayer sender, object args)
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            var duration = sender.PlaybackSession.NaturalDuration.TotalSeconds;
            if (_videoFps == null || _videoFps == 0 || duration == 0 || VideoPreview.MediaPlayer.NaturalDuration == TimeSpan.MaxValue)
            {
                Logger.Error("Failed to read video data");
                // TODO remux with ffmpeg -i input.mp4 -c copy -movflags +faststart fixed.mp4
                _notificationService.Show(string.Format("AppVideoLoadNotificationFailure".GetLocalized(), AppContext.BaseDirectory));
                HideVideoUi();
                return;
            }

            TrimSelector.Minimum = 0;
            TrimSelector.Maximum = duration;
            TrimSelector.RangeStart = 0;
            TrimSelector.RangeEnd = duration;
            TrimSelector.StepFrequency = 0.1;

            _startSec = 0;
            _endSec = duration;

            // initialize the TextBox values (with .f)
            StartTimeTextBox.Text = TimeFormat.ToPaddedMinutesSeconds(0);
            EndTimeTextBox.Text = TimeFormat.ToPaddedMinutesSeconds(duration);

            GenerateButton.IsEnabled = true;
            UpdateFileSizeWarning();
        });
    }

    private void TrimSelector_ValueChanged(object sender, RangeChangedEventArgs e)
    {
        if (e.ChangedRangeProperty == RangeSelectorProperty.MinimumValue)
        {
            _startSec = e.NewValue;
        }
        else
        {
            _endSec = e.NewValue;
        }

        // update the text inputs in sync
        StartTimeTextBox.Text = TimeFormat.ToPaddedMinutesSeconds(_startSec);
        EndTimeTextBox.Text = TimeFormat.ToPaddedMinutesSeconds(_endSec);
        UpdateFileSizeWarning();

    }

    private void TrimSelector_RangeDragging(object sender, CustomControls.RangeDraggingEventArgs e)
    {
        Seek(e.NewValue);
    }

    private void TrimSelector_ThumbDragStarted(object sender, DragStartedEventArgs e)
    {
        VideoPreview.MediaPlayer.Pause();
        _correctingPosition = true;
    }

    private void TrimSelector_ThumbDragCompleted(object sender, DragCompletedEventArgs e)
    {
        Seek(_startSec);
        VideoPreview.MediaPlayer.Play();
        _correctingPosition = false;
    }


    private void Session_PositionChanged(MediaPlaybackSession sender, object args)
    {
        if (_correctingPosition)
        {
            return;
        }

        var sec = sender.Position.TotalSeconds;

        if (sec < _startSec || sec > _endSec)
        {
            _correctingPosition = true;
            if (sec < _startSec)
            {
                Seek(_startSec);
                _correctingPosition = false;

            }
            else if (sec > _endSec)
            {
                Seek(_startSec);
                _correctingPosition = false;
            }
        }
    }

    private static bool TryParseTime(string text, out double secs) =>
        TimeFormat.TryParseMinutesSeconds(text, out secs);

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_videoFile == null)
        {
            return;
        }

        try
        {
            GenerateButton.IsEnabled = false;
            GenerateLoading.Value = 0;
            GenerateLoading.IsIndeterminate = true;
            GenerateLoading.Visibility = Visibility.Visible;

            var chosenWidth = (int)((ComboBoxItem)ComboResolution.SelectedItem).Tag;
            var chosenFps = (double)((ComboBoxItem)ComboFps.SelectedItem).Tag;

            var request = new GifGenerationRequest(
                _videoFile.Path,
                TimeSpan.FromSeconds(_startSec),
                TimeSpan.FromSeconds(_endSec),
                chosenWidth,
                chosenFps);

            var gifLocation = await ViewModel.GenerateGifAsync(
                request,
                onTrimCompleted: () => GenerateLoading.IsIndeterminate = false,
                onExtractFramesProgress: percent => DispatcherQueue.TryEnqueue(() => GenerateLoading.Value = percent * 0.3),
                onCreateGifProgress: percent => DispatcherQueue.TryEnqueue(() => GenerateLoading.Value = 30 + percent * 0.7));

            _lockscreenService.CurrentImage = await StorageFile.GetFileFromPathAsync(gifLocation);
            currentImage.Source = _lockscreenService.CurrentImageBitmap!;
            ApplyButton.IsEnabled = true;
            GenerateLoading.Value = 100;
        }
        catch (Exception ex)
        {
            GenerateLoading.ShowError = true;
            Logger.Error("Failed to create gif from video", ex);
        }
        finally
        {
            _ffmpegService.CleanupTempDirectories();
            GenerateButton.IsEnabled = true;
        }

        return;
    }

    public static async Task<(double? Fps, uint Width, uint Height)> GetVideoInfoAsync(StorageFile file)
    {
        if (file is null)
        {
            Logger.Error("File missing");
            return (null, 0, 0);
        }

        try
        {
            var clip = await MediaClip.CreateFromFileAsync(file);
            var props = clip.GetVideoEncodingProperties();

            double? fps = props.FrameRate.Denominator == 0
                                  ? null
                                  : (double)props.FrameRate.Numerator / props.FrameRate.Denominator;

            return (fps, props.Width, props.Height);
        }
        catch (Exception ex)
        {
            Logger.Error("failed to get video info", ex);
        }
        return (null, 0, 0);

    }

    private void ComboResolution_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboFps.SelectedItem != null && ComboResolution.SelectedItem != null)
        {
            UpdateFileSizeWarning();
        }
    }
}
