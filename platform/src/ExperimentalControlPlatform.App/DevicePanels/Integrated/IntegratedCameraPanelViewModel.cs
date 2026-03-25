using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ExperimentalControlPlatform.App.DevicePanels;
using ExperimentalControlPlatform.App.DevicePanels.Camera;
using ExperimentalControlPlatform.App.DevicePanels.Contracts;
using ExperimentalControlPlatform.App.Modals;
using ExperimentalControlPlatform.Devices.Uvc;
using MahApps.Metro.IconPacks;

namespace ExperimentalControlPlatform.App.DevicePanels.Integrated;

public sealed class IntegratedCameraPanelViewModel : ObservableObject, ICameraPanelViewModel
{
    private const double MaxDisplayFps = 20.0;
    private static readonly IReadOnlyList<IntegrationPanelLifecycleAction> ConnectedLifecycleActions =
    [
        IntegrationPanelLifecycleAction.Apply
    ];
    private readonly IntegratedCameraClient _client;
    private readonly AsyncRelayCommand _lifecycleActionCommand;
    private CancellationTokenSource? _liveCancellation;
    private Task? _liveTask;
    private bool _isBusy;
    private bool _isConnected;
    private bool _isLivePreviewing;
    private IReadOnlyList<UvcCameraInfo> _cameraOptions = [];
    private UvcCameraInfo? _selectedCamera;
    private string _frameRateInput = "20";
    private string _draftFrameRateInput = "20";
    private string _draftColorMode = "Color";
    private string _colorMode = "Color";
    private ImageSource? _previewImage;
    private string _currentFrameValue = "No frame captured";
    private string _resolutionText = "--";
    private string _pixelFormatText = "--";
    private string _exposureText = "--";
    private string _frameRateText = "--";
    private string _footerConnectionLabel = "Hardware: Disconnected";
    private string _footerCameraLabel = "No camera";
    private string _footerFormatLabel = "Color";
    private string _footerTriggerLabel = "UVC";
    private string _footerSystemStateLabel = "System Ready";
    private string _statusMessage = "Ready to discover camera";
    private List<string> _latestDiagnostics = ["No diagnostics collected yet."];
    private long? _lastFrameTimestamp;
    private long? _lastDisplayTimestamp;
    private double? _smoothedFrameRate;
    private IntegrationPanelDataOutput? _dataOutput;
    private IntegrationPanelAppliedSettingsOutput? _appliedSettingsOutput;
    private IntegrationPanelStatusOutput? _statusOutput;
    private IntegrationPanelDiagnosticsOutput? _diagnosticsOutput;
    private IntegrationPanelSessionEndOutput? _sessionEndOutput;
    private string? _lastCommand;
    private string? _lastHardwareResponse;
    private string? _lastError;
    private string? _lastStateTransition;
    private string? _lastValidationResult;
    private DateTimeOffset? _lastFrameCapturedAt;
    private long _frameSequence;
    private string? _lastSourceMode;

    public IntegratedCameraPanelViewModel(IntegratedCameraClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _lifecycleActionCommand = new AsyncRelayCommand(ExecuteLifecycleActionAsync, CanExecuteLifecycleAction, HandleLifecycleCommandException);
        RefreshStatusOutput();
        RefreshDiagnosticsOutput();
    }

    public string Title => "Integrated Camera";

    System.Collections.IEnumerable ICameraPanelViewModel.CameraOptions => CameraOptions;

    public IReadOnlyList<string> ChannelOptions { get; } = [];

    public string PrimaryValueLabel => "Current frame: ";

    public IReadOnlyList<UvcCameraInfo> CameraOptions
    {
        get => _cameraOptions;
        private set => SetProperty(ref _cameraOptions, value);
    }

    public UvcCameraInfo? SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            if (SetProperty(ref _selectedCamera, value))
            {
                OnPropertyChanged(nameof(SelectedCameraItem));
                OnPropertyChanged(nameof(CanToggleConnection));
                OnPropertyChanged(nameof(CanSnapFrame));
                OnPropertyChanged(nameof(CanToggleLive));
                SyncFooter();
                RefreshStatusOutput();
            }
        }
    }

    public object? SelectedCameraItem
    {
        get => SelectedCamera;
        set => SelectedCamera = value as UvcCameraInfo;
    }

    public IReadOnlyList<string> ColorModeOptions { get; } = ["Color", "Mono"];

    public IReadOnlyList<string> ColorOptions => ColorModeOptions;

    public string FrameRateInputDraft
    {
        get => _draftFrameRateInput;
        set
        {
            if (SetProperty(ref _draftFrameRateInput, value))
            {
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedColorModeDraft
    {
        get => _draftColorMode;
        set
        {
            if (SetProperty(ref _draftColorMode, value))
            {
                OnPropertyChanged(nameof(SelectedColorOptionDraft));
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedColorOptionDraft
    {
        get => SelectedColorModeDraft;
        set => SelectedColorModeDraft = value;
    }

    public Visibility ExposureControlVisibility => Visibility.Collapsed;

    public string ExposureInputDraft
    {
        get => "Auto";
        set { }
    }

    public Visibility AdvancedSettingsVisibility => Visibility.Collapsed;

    public Visibility RoiToolbarVisibility => Visibility.Collapsed;

    public ImageSource? PreviewImage
    {
        get => _previewImage;
        private set
        {
            if (SetProperty(ref _previewImage, value))
            {
                OnPropertyChanged(nameof(BackgroundFrameImage));
            }
        }
    }

    public ImageSource? BackgroundFrameImage => PreviewImage;

    public string CurrentFrameValue
    {
        get => _currentFrameValue;
        private set
        {
            if (SetProperty(ref _currentFrameValue, value))
            {
                OnPropertyChanged(nameof(CurrentPrimaryValue));
            }
        }
    }

    public string CurrentPrimaryValue => CurrentFrameValue;

    public string ResolutionText
    {
        get => _resolutionText;
        private set => SetProperty(ref _resolutionText, value);
    }

    public string PixelFormatText
    {
        get => _pixelFormatText;
        private set => SetProperty(ref _pixelFormatText, value);
    }

    public string ExposureText
    {
        get => _exposureText;
        private set => SetProperty(ref _exposureText, value);
    }

    public string FrameRateText
    {
        get => _frameRateText;
        private set => SetProperty(ref _frameRateText, value);
    }

    public string FooterConnectionLabel
    {
        get => _footerConnectionLabel;
        private set => SetProperty(ref _footerConnectionLabel, value);
    }

    public string FooterCameraLabel
    {
        get => _footerCameraLabel;
        private set => SetProperty(ref _footerCameraLabel, value);
    }

    public string FooterFormatLabel
    {
        get => _footerFormatLabel;
        private set => SetProperty(ref _footerFormatLabel, value);
    }

    public string FooterTriggerLabel
    {
        get => _footerTriggerLabel;
        private set => SetProperty(ref _footerTriggerLabel, value);
    }

    public string FooterSystemStateLabel
    {
        get => _footerSystemStateLabel;
        private set => SetProperty(ref _footerSystemStateLabel, value);
    }

    public string DiagnosticsTitle => "Integrated camera diagnostics";

    public string DiagnosticsSubtitle => "Windows UVC camera state";

    public IntegrationPanelDataOutput? DataOutput
    {
        get => _dataOutput;
        private set => SetProperty(ref _dataOutput, value);
    }

    public IntegrationPanelAppliedSettingsOutput? AppliedSettingsOutput
    {
        get => _appliedSettingsOutput;
        private set => SetProperty(ref _appliedSettingsOutput, value);
    }

    public IntegrationPanelStatusOutput? StatusOutput
    {
        get => _statusOutput;
        private set => SetProperty(ref _statusOutput, value);
    }

    public IntegrationPanelDiagnosticsOutput? DiagnosticsOutput
    {
        get => _diagnosticsOutput;
        private set => SetProperty(ref _diagnosticsOutput, value);
    }

    public IntegrationPanelSessionEndOutput? SessionEndOutput
    {
        get => _sessionEndOutput;
        private set => SetProperty(ref _sessionEndOutput, value);
    }

    public IReadOnlyList<IntegrationPanelLifecycleAction> SupportedLifecycleActions => IsConnected ? ConnectedLifecycleActions : [];

    public ICommand LifecycleActionCommand => _lifecycleActionCommand;

    public bool IsConnected
    {
        get => _isConnected;
        private set
        {
            if (SetProperty(ref _isConnected, value))
            {
                OnPropertyChanged(nameof(ConnectionToggleLabel));
                OnPropertyChanged(nameof(ConnectionToggleIconKind));
                OnPropertyChanged(nameof(CanToggleConnection));
                OnPropertyChanged(nameof(CanSnapFrame));
                OnPropertyChanged(nameof(CanToggleLive));
                OnPropertyChanged(nameof(SupportedLifecycleActions));
                RefreshStatusOutput();
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLivePreviewing
    {
        get => _isLivePreviewing;
        private set
        {
            if (SetProperty(ref _isLivePreviewing, value))
            {
                OnPropertyChanged(nameof(LiveToggleLabel));
                OnPropertyChanged(nameof(LiveToggleIconKind));
                OnPropertyChanged(nameof(CanSnapFrame));
                OnPropertyChanged(nameof(CanToggleLive));
                OnPropertyChanged(nameof(CanApplySettings));
                RefreshStatusOutput();
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ConnectionToggleLabel => IsConnected ? "Disconnect" : "Connect";

    public PackIconMaterialKind ConnectionToggleIconKind => IsConnected
        ? PackIconMaterialKind.LinkOff
        : PackIconMaterialKind.Link;

    public string LiveToggleLabel => IsLivePreviewing ? "Stop live" : "Start live";

    public PackIconMaterialKind LiveToggleIconKind => IsLivePreviewing
        ? PackIconMaterialKind.Stop
        : PackIconMaterialKind.Play;

    public bool CanToggleConnection => SelectedCamera is not null && !_isBusy && !IsLivePreviewing;

    public bool CanSnapFrame => IsConnected && !_isBusy && !IsLivePreviewing;

    public bool CanToggleLive => IsConnected && !_isBusy;

    public bool CanApplySettings => !_isBusy &&
                                    (!string.Equals(_frameRateInput, _draftFrameRateInput, StringComparison.Ordinal) ||
                                     !string.Equals(_colorMode, _draftColorMode, StringComparison.Ordinal));

    public string EmptyStateText => _statusMessage;

    public bool CanSetRoi => false;

    public bool CanClearRoi => false;

    public Visibility RoiOverlayVisibility => Visibility.Collapsed;

    public Visibility RoiHandleVisibility => Visibility.Collapsed;

    public Visibility RoiLiveVisibility => Visibility.Collapsed;

    public Rect RoiRectNormalized
    {
        get => Rect.Empty;
        set { }
    }

    public async Task RefreshCamerasAsync()
    {
        var cameras = _client.ListCameras();
        CameraOptions = cameras;
        SelectedCamera ??= cameras.FirstOrDefault();
        SetLastCommand("List integrated cameras");
        _latestDiagnostics =
        [
            $"Detected cameras: {cameras.Count}",
            ..cameras.Select(camera => $"{camera.Index}: {camera.DisplayName}")
        ];
        SetLastHardwareResponse(SelectedCamera is null ? "No integrated camera found." : "Integrated camera ready.");
        _statusMessage = SelectedCamera is null ? "No integrated camera found" : "Integrated camera ready";
        OnPropertyChanged(nameof(CanToggleConnection));
        OnPropertyChanged(nameof(CanSnapFrame));
        OnPropertyChanged(nameof(CanToggleLive));
        OnPropertyChanged(nameof(SelectedCameraItem));
        SyncFooter();
        await Task.CompletedTask;
    }

    public async Task ConnectAsync()
    {
        if (SelectedCamera is null)
        {
            SetLastError("No integrated camera selected.");
            _statusMessage = "No integrated camera selected.";
            return;
        }

        _isBusy = true;
        OnPropertyChanged(nameof(CanToggleConnection));
        try
        {
            SetLastCommand("Connect integrated camera");
            _ = _client.CaptureSnapshot(SelectedCamera.Index, ParseFrameRate(), _colorMode == "Color");
            IsConnected = true;
            SessionEndOutput = null;
            ClearLastError();
            SetLastHardwareResponse("Integrated camera connection probe succeeded.");
            SetLastStateTransition($"Connected to {SelectedCamera.DisplayName}");
            _statusMessage = $"Connected to {SelectedCamera.DisplayName}.";
            SyncFooter();
        }
        finally
        {
            _isBusy = false;
            OnPropertyChanged(nameof(CanToggleConnection));
            OnPropertyChanged(nameof(CanSnapFrame));
            OnPropertyChanged(nameof(CanToggleLive));
        }

        await Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        var liveWasActive = IsLivePreviewing;
        await StopLivePreviewAsync();
        IsConnected = false;
        _statusMessage = "Disconnected.";
        SyncFooter();
        SetLastStateTransition("Disconnected camera");
        SessionEndOutput = new IntegrationPanelSessionEndOutput
        {
            EndedAt = DateTimeOffset.Now,
            ExitReason = "Disconnected by operator",
            ConnectionClosed = true,
            LiveStopped = liveWasActive,
            AppliedSettingsSnapshot = AppliedSettingsOutput,
            FinalStatus = BuildStatusOutput()
        };
    }

    public async Task SnapFrameAsync()
    {
        if (SelectedCamera is null)
        {
            return;
        }

        _isBusy = true;
        OnPropertyChanged(nameof(CanSnapFrame));
        try
        {
            SetLastCommand("Capture snapshot");
            _lastSourceMode = "snapshot";
            var result = _client.CaptureSnapshot(SelectedCamera.Index, ParseFrameRate(), _colorMode == "Color");
            ApplyFrame(result);
            ClearLastError();
            SetLastHardwareResponse("Snapshot captured.");
            SetLastStateTransition("Captured snapshot");
            _statusMessage = "Snapshot captured.";
        }
        finally
        {
            _isBusy = false;
            OnPropertyChanged(nameof(CanSnapFrame));
        }

        await Task.CompletedTask;
    }

    public async Task StartLivePreviewAsync()
    {
        if (SelectedCamera is null || IsLivePreviewing)
        {
            return;
        }

        _liveCancellation = new CancellationTokenSource();
        IsLivePreviewing = true;
        SetLastCommand("Start live preview");
        SetLastStateTransition("Started live preview");
        SyncFooter();
        _statusMessage = "Live preview started.";

        try
        {
            _liveTask = _client.StreamFramesAsync(
                SelectedCamera.Index,
                ParseFrameRate(),
                _colorMode == "Color",
                ApplyLiveFrameAsync,
                _liveCancellation.Token);

            await _liveTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _statusMessage = $"Live preview failed: {ex.Message}";
            _latestDiagnostics.Add($"Live preview failed: {ex}");
            SetLastError(ex.Message);
        }
        finally
        {
            IsLivePreviewing = false;
            _liveCancellation?.Dispose();
            _liveCancellation = null;
            _liveTask = null;
            SyncFooter();
        }
    }

    public async Task StopLivePreviewAsync()
    {
        if (_liveCancellation is null)
        {
            return;
        }

        _liveCancellation.Cancel();
        SetLastStateTransition("Stopped live preview");
        if (_liveTask is not null)
        {
            try
            {
                await _liveTask.ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }

    public async Task ApplySettingsAsync()
    {
        if (!double.TryParse(_draftFrameRateInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFrameRate) || parsedFrameRate <= 0)
        {
            SetLastValidationResult("Target frame rate must be a positive number.");
            _statusMessage = "Target frame rate must be a positive number.";
            return;
        }

        SetLastValidationResult("Validated settings.");
        var previousFrameRateInput = _frameRateInput;
        var previousColorMode = _colorMode;
        _frameRateInput = _draftFrameRateInput;
        _colorMode = _draftColorMode;
        SyncFooter();
        OnPropertyChanged(nameof(CanApplySettings));
        _lifecycleActionCommand.NotifyCanExecuteChanged();

        var restartLive = IsLivePreviewing;
        var appliedToHardware = true;
        if (restartLive)
        {
            await StopLivePreviewAsync();
            await StartLivePreviewAsync();
            appliedToHardware = _lastError is null;
        }
        else if (IsConnected)
        {
            appliedToHardware = TryApplySettingsSnapshot();
        }

        if (!appliedToHardware)
        {
            _frameRateInput = previousFrameRateInput;
            _colorMode = previousColorMode;
            SyncFooter();
            OnPropertyChanged(nameof(CanApplySettings));
            _lifecycleActionCommand.NotifyCanExecuteChanged();
            return;
        }

        _statusMessage = restartLive
            ? "Camera settings applied. Restarting live preview..."
            : "Camera settings applied.";

        ClearLastError();
        SetLastStateTransition(restartLive
            ? "Applied settings and restarted live preview"
            : "Applied settings");
        if (IsConnected)
        {
            CaptureAppliedSettingsSnapshot("Applied to connected integrated camera.");
            SessionEndOutput = null;
        }
    }

    public string GetDiagnosticsSummary()
    {
        return string.Join(Environment.NewLine, new[]
        {
            $"Device: {SelectedCamera?.DisplayName ?? "No camera"}",
            $"Connection: {(IsConnected ? "Connected" : "Disconnected")}",
            $"Mode: UVC",
            $"Target frame rate: {_frameRateInput}",
            $"Color: {_colorMode}",
            $"Resolution: {ResolutionText}",
            $"Capture rate: {FrameRateText}",
            $"Status: {_statusMessage}"
        });
    }

    public CameraSettingsDialogViewModel? CreateDetailedSettingsDialog() => null;

    public Task ApplyDetailedSettingsAsync(CameraSettingsDialogViewModel dialog) => Task.CompletedTask;

    public Task BeginRoiEditAsync() => Task.CompletedTask;

    public void ClearRoi()
    {
    }

    public Task ApplyRoiAsync() => Task.CompletedTask;

    private async Task ApplyLiveFrameAsync(IntegratedFrameResult frame)
    {
        var nowTicks = Stopwatch.GetTimestamp();
        if (_lastDisplayTimestamp.HasValue)
        {
            var minTicks = Stopwatch.Frequency / MaxDisplayFps;
            if (nowTicks - _lastDisplayTimestamp.Value < minTicks)
            {
                await RunOnUiAsync(() => UpdateMeasuredFrameRate(frame.TimestampTicks));
                return;
            }
        }

        _lastDisplayTimestamp = nowTicks;
        await RunOnUiAsync(() =>
        {
            UpdateMeasuredFrameRate(frame.TimestampTicks);
            _lastSourceMode = "live";
            SetLastHardwareResponse("Live frame received.");
            ApplyFrame(frame);
        });
    }

    private void ApplyFrame(IntegratedFrameResult frame)
    {
        PreviewImage = LoadImage(frame);
        CurrentFrameValue = $"{frame.Width} x {frame.Height}";
        ResolutionText = $"{frame.Width} x {frame.Height}";
        PixelFormatText = frame.IsColor ? "RGB" : "Mono";
        ExposureText = "Auto";
        _lastFrameCapturedAt = DateTimeOffset.Now;
        _frameSequence++;
        DataOutput = new IntegrationPanelDataOutput
        {
            Timestamp = _lastFrameCapturedAt,
            EndpointId = SelectedCamera?.DisplayName,
            PayloadType = "CameraFrame",
            PayloadValue = $"{frame.Width}x{frame.Height}; Color={(_colorMode == "Color" ? "true" : "false")}",
            Units = "pixels",
            SequenceNumber = _frameSequence,
            CaptureRate = _smoothedFrameRate,
            SourceMode = _lastSourceMode
        };
        CaptureAppliedSettingsSnapshot($"Verified during {(_lastSourceMode ?? "capture")}.");
        SyncFooter();
    }

    private void UpdateMeasuredFrameRate(long timestampTicks)
    {
        if (_lastFrameTimestamp.HasValue)
        {
            var deltaSeconds = (timestampTicks - _lastFrameTimestamp.Value) / (double)Stopwatch.Frequency;
            if (deltaSeconds > 0)
            {
                var instantRate = 1.0 / deltaSeconds;
                _smoothedFrameRate = _smoothedFrameRate.HasValue
                    ? (_smoothedFrameRate.Value * 0.8) + (instantRate * 0.2)
                    : instantRate;
                FrameRateText = $"{_smoothedFrameRate.Value:0.0} fps";
            }
        }

        _lastFrameTimestamp = timestampTicks;
    }

    private ImageSource LoadImage(IntegratedFrameResult frame)
    {
        var pixelFormat = frame.IsColor ? PixelFormats.Rgb24 : PixelFormats.Gray8;
        var stride = frame.IsColor ? frame.Width * 3 : frame.Width;
        var bitmap = BitmapSource.Create(frame.Width, frame.Height, 96, 96, pixelFormat, null, frame.PixelData, stride);
        bitmap.Freeze();
        return bitmap;
    }

    private double ParseFrameRate()
    {
        return double.TryParse(_frameRateInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var fps) && fps > 0
            ? fps
            : 20.0;
    }

    private void SyncFooter()
    {
        FooterConnectionLabel = IsConnected ? "Hardware: Connected" : "Hardware: Disconnected";
        FooterCameraLabel = SelectedCamera?.DisplayName ?? "No camera";
        FooterFormatLabel = _colorMode;
        FooterTriggerLabel = "UVC";
        FooterSystemStateLabel = IsLivePreviewing ? "Streaming" : "System Ready";
    }

    private bool CanExecuteLifecycleAction(object? parameter)
    {
        return parameter is IntegrationPanelLifecycleAction.Apply && IsConnected && CanApplySettings;
    }

    private async Task ExecuteLifecycleActionAsync(object? parameter)
    {
        if (parameter is not IntegrationPanelLifecycleAction.Apply)
        {
            return;
        }

        SetLastCommand("Apply integrated camera settings");
        await ApplySettingsAsync().ConfigureAwait(false);
    }

    private void HandleLifecycleCommandException(Exception exception)
    {
        SetLastError(exception.Message);
        _statusMessage = exception.Message;
    }

    private void CaptureAppliedSettingsSnapshot(string note)
    {
        AppliedSettingsOutput = new IntegrationPanelAppliedSettingsOutput
        {
            AppliedAt = DateTimeOffset.Now,
            DeviceSettings = new Dictionary<string, string?>
            {
                ["Device"] = Title,
                ["Camera"] = SelectedCamera?.DisplayName
            },
            EndpointSettings = new Dictionary<string, string?>
            {
                ["FrameRateFps"] = _frameRateInput,
                ["ColorMode"] = _colorMode
            },
            SessionSettings = new Dictionary<string, string?>
            {
                ["Connected"] = IsConnected ? "true" : "false",
                ["LivePreviewing"] = IsLivePreviewing ? "true" : "false"
            },
            NormalizationNotes = new[] { note }
        };
    }

    private bool TryApplySettingsSnapshot()
    {
        if (SelectedCamera is null)
        {
            SetLastError("No integrated camera selected.");
            _statusMessage = "No integrated camera selected.";
            return false;
        }

        try
        {
            _lastSourceMode = "apply";
            var result = _client.CaptureSnapshot(SelectedCamera.Index, ParseFrameRate(), _colorMode == "Color");
            ApplyFrame(result);
            ClearLastError();
            SetLastHardwareResponse("Applied settings snapshot captured.");
            return true;
        }
        catch (Exception ex)
        {
            SetLastError(ex.Message);
            _statusMessage = $"Applying settings failed: {ex.Message}";
            return false;
        }
    }

    private IntegrationPanelStatusOutput BuildStatusOutput()
    {
        return new IntegrationPanelStatusOutput
        {
            Connected = IsConnected,
            ReadyState = _isBusy ? "Busy" : IsConnected ? "Ready" : "Disconnected",
            FaultState = _lastError,
            LiveState = IsLivePreviewing ? "Streaming" : "Stopped",
            SelectedEndpoint = SelectedCamera?.DisplayName,
            BackgroundActiveEndpoints = []
        };
    }

    private void RefreshStatusOutput()
    {
        StatusOutput = BuildStatusOutput();
    }

    private void RefreshDiagnosticsOutput()
    {
        DiagnosticsOutput = new IntegrationPanelDiagnosticsOutput
        {
            LastCommand = _lastCommand,
            LastHardwareResponse = _lastHardwareResponse,
            LastError = _lastError,
            LastStateTransition = _lastStateTransition,
            LastValidationResult = _lastValidationResult
        };
    }

    private void SetLastCommand(string value)
    {
        _lastCommand = value;
        RefreshDiagnosticsOutput();
    }

    private void SetLastHardwareResponse(string value)
    {
        _lastHardwareResponse = value;
        RefreshDiagnosticsOutput();
    }

    private void SetLastError(string value)
    {
        _lastError = value;
        RefreshDiagnosticsOutput();
        RefreshStatusOutput();
    }

    private void ClearLastError()
    {
        if (_lastError is null)
        {
            return;
        }

        _lastError = null;
        RefreshDiagnosticsOutput();
        RefreshStatusOutput();
    }

    private void SetLastStateTransition(string value)
    {
        _lastStateTransition = value;
        RefreshDiagnosticsOutput();
        RefreshStatusOutput();
    }

    private void SetLastValidationResult(string value)
    {
        _lastValidationResult = value;
        RefreshDiagnosticsOutput();
    }

    private static Task RunOnUiAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(action).Task;
    }

    public void Dispose()
    {
        try
        {
            _liveCancellation?.Cancel();
            _liveTask?.GetAwaiter().GetResult();
        }
        catch
        {
        }
        finally
        {
            _liveCancellation?.Dispose();
        }
    }
}
