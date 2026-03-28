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
using ExperimentalControlPlatform.Runtime;
using MahApps.Metro.IconPacks;

namespace ExperimentalControlPlatform.App.DevicePanels.Integrated;

public sealed class IntegratedCameraPanelViewModel : ObservableObject, ICameraPanelViewModel, IOutputSettingsPanelViewModel, IPanelCloseViewModel
{
    private const double MaxDisplayFps = 20.0;
    private static readonly IReadOnlyList<IntegrationPanelLifecycleAction> ConnectedLifecycleActions =
    [
        IntegrationPanelLifecycleAction.Apply,
        IntegrationPanelLifecycleAction.ApplyAndExit
    ];
    private readonly IntegratedCameraClient _client;
    private readonly IDeviceSessionRegistry _sessionRegistry;
    private readonly AsyncRelayCommand _lifecycleActionCommand;
    private readonly IntegrationPanelOutputPublisher _outputPublisher = new();
    private IntegratedCameraSession? _session;
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
    private IntegrationPanelOutputSettings _outputSettings = new(
        IntegrationPanelOutputPayloadType.Image,
        IntegrationPanelOutputEmissionMode.LatestOnly,
        5.0,
        true);

    public IntegratedCameraPanelViewModel(IntegratedCameraClient client, IDeviceSessionRegistry sessionRegistry)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _sessionRegistry = sessionRegistry ?? throw new ArgumentNullException(nameof(sessionRegistry));
        _lifecycleActionCommand = new AsyncRelayCommand(ExecuteLifecycleActionAsync, CanExecuteLifecycleAction, HandleLifecycleCommandException);
        RefreshStatusOutput();
        RefreshDiagnosticsOutput();
    }

    public string Title => "Integrated Camera";

    public event EventHandler? CloseRequested;

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

    public IReadOnlyList<IntegrationPanelOutputPayloadType> SupportedOutputPayloadTypes { get; } =
    [
        IntegrationPanelOutputPayloadType.Image
    ];

    public IntegrationPanelOutputSettings CurrentOutputSettings => _outputSettings;

    public string OutputSettingsSubtitle => "Shape the integrated-camera frame output before it reaches the runtime bus.";

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

        var session = GetOrCreateSession();
        BindSession(session);
        try
        {
            await session.ConnectAsync(BuildCaptureSettings()).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IntegratedCameraPanelViewModel] Connect failed: {ex}");
        }
    }

    public async Task DisconnectAsync()
    {
        if (_session is null)
        {
            return;
        }

        var session = _session;
        try
        {
            await session.DisconnectAsync(StopReason.UserRequested("Disconnected integrated camera panel.")).ConfigureAwait(true);
        }
        finally
        {
            _sessionRegistry.Remove(session.SessionId);
            UnbindSession();
            await session.DisposeAsync().ConfigureAwait(true);
        }
    }

    public Task CloseWithoutApplyAsync() => DisconnectAsync();

    public async Task SnapFrameAsync()
    {
        if (_session is null)
        {
            return;
        }

        try
        {
            await _session.SnapFrameAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IntegratedCameraPanelViewModel] Snap failed: {ex}");
        }
    }

    public async Task StartLivePreviewAsync()
    {
        if (_session is null || IsLivePreviewing)
        {
            return;
        }

        try
        {
            await _session.StartLiveAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IntegratedCameraPanelViewModel] Start live failed: {ex}");
        }
    }

    public async Task StopLivePreviewAsync()
    {
        if (_session is null)
        {
            return;
        }

        try
        {
            await _session.StopLiveAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IntegratedCameraPanelViewModel] Stop live failed: {ex}");
        }
    }

    public Task ApplySettingsAsync() => ApplySettingsCoreAsync();

    private async Task<bool> ApplySettingsCoreAsync()
    {
        if (!double.TryParse(_draftFrameRateInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedFrameRate) || parsedFrameRate <= 0)
        {
            SetLastValidationResult("Target frame rate must be a positive number.");
            _statusMessage = "Target frame rate must be a positive number.";
            return false;
        }

        SetLastValidationResult("Validated settings.");
        _frameRateInput = _draftFrameRateInput;
        _colorMode = _draftColorMode;
        SyncFooter();
        OnPropertyChanged(nameof(CanApplySettings));
        _lifecycleActionCommand.NotifyCanExecuteChanged();

        if (_session is null)
        {
            _statusMessage = "Integrated camera settings staged for the next capture.";
            return true;
        }

        try
        {
            await _session.ApplySettingsAsync(BuildCaptureSettings()).ConfigureAwait(true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IntegratedCameraPanelViewModel] Apply settings failed: {ex}");
            return false;
        }
    }

    public Task ApplyOutputSettingsAsync(IntegrationPanelOutputSettings settings)
    {
        _outputSettings = settings;
        _outputPublisher.Reset();
        SetLastCommand("Apply integrated camera output settings");
        SetLastValidationResult("Validated integrated camera output settings.");
        SetLastStateTransition("Applied integrated camera output settings");
        _statusMessage = "Integrated camera output settings updated.";
        if (_session?.LatestFrame.Current is not null)
        {
            ApplySessionFrame(_session.LatestFrame.Current);
        }
        else
        {
            RefreshAppliedSettingsOutput();
        }

        return Task.CompletedTask;
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

    private void ApplyFrame(IntegratedCameraFrame frame)
    {
        PreviewImage = LoadImage(frame);
        CurrentFrameValue = $"{frame.Width} x {frame.Height}";
        ResolutionText = $"{frame.Width} x {frame.Height}";
        PixelFormatText = frame.IsColor ? "RGB" : "Mono";
        ExposureText = "Auto";
        _lastFrameCapturedAt = DateTimeOffset.Now;
        _frameSequence++;
        var timestamp = _lastFrameCapturedAt ?? DateTimeOffset.Now;
        var payloadValue = $"{frame.Width}x{frame.Height}; Color={(_colorMode == "Color" ? "true" : "false")}";
        if (_outputPublisher.ShouldPublish(_outputSettings, timestamp, payloadValue))
        {
            DataOutput = new IntegrationPanelDataOutput
            {
                Timestamp = _outputSettings.IncludeMetadata ? timestamp : null,
                DeviceId = _outputSettings.IncludeMetadata ? SelectedCamera?.InstanceId : null,
                EndpointId = _outputSettings.IncludeMetadata ? SelectedCamera?.DisplayName : null,
                PayloadType = _outputSettings.PayloadType.ToString(),
                PayloadValue = payloadValue,
                Units = "pixels",
                SequenceNumber = _frameSequence,
                CaptureRate = _smoothedFrameRate,
                SourceMode = _lastSourceMode,
                OutputEmissionMode = _outputSettings.EmissionMode.ToString(),
                OutputFrequencyHz = _outputSettings.OutputFrequencyHz,
                MetadataIncluded = _outputSettings.IncludeMetadata
            };
        }
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

    private ImageSource LoadImage(IntegratedCameraFrame frame)
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
        return parameter switch
        {
            IntegrationPanelLifecycleAction.Apply => IsConnected && CanApplySettings,
            IntegrationPanelLifecycleAction.ApplyAndExit => IsConnected,
            _ => false
        };
    }

    private async Task ExecuteLifecycleActionAsync(object? parameter)
    {
        switch (parameter)
        {
            case IntegrationPanelLifecycleAction.Apply:
                SetLastCommand("Apply integrated camera settings");
                await ApplySettingsAsync().ConfigureAwait(false);
                return;
            case IntegrationPanelLifecycleAction.ApplyAndExit:
                await ApplyAndExitAsync().ConfigureAwait(false);
                return;
            default:
                return;
        }
    }

    private void HandleLifecycleCommandException(Exception exception)
    {
        SetLastError(exception.Message);
        _statusMessage = exception.Message;
    }

    private async Task ApplyAndExitAsync()
    {
        SetLastCommand("Apply and exit integrated camera settings");
        if (!await ApplySettingsCoreAsync().ConfigureAwait(true))
        {
            return;
        }

        await DisconnectAsync().ConfigureAwait(true);
        CloseRequested?.Invoke(this, EventArgs.Empty);
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
        AppliedSettingsOutput = AppliedSettingsOutput with
        {
            SessionSettings = AppliedSettingsOutput.SessionSettings.WithOutputSettings(_outputSettings)
        };
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
        if (_session is null)
        {
            UnbindSession();
            return;
        }

        var session = _session;
        _sessionRegistry.Remove(session.SessionId);
        UnbindSession();
        _ = RunSessionDisposeAsync(session, "Disposed integrated camera panel.");
    }

    private IntegratedCameraCaptureSettings BuildCaptureSettings()
    {
        var camera = SelectedCamera ?? throw new InvalidOperationException("No integrated camera selected.");
        var deviceId = camera.InstanceId ?? $"{camera.Index}:{camera.DisplayName}";
        return new IntegratedCameraCaptureSettings(
            deviceId,
            camera.Index,
            camera.DisplayName,
            ParseFrameRate(),
            _colorMode == "Color");
    }

    private IntegratedCameraSession GetOrCreateSession()
    {
        var camera = SelectedCamera ?? throw new InvalidOperationException("No integrated camera selected.");
        var deviceId = camera.InstanceId ?? $"{camera.Index}:{camera.DisplayName}";
        var sessionId = new DeviceSessionId("IntegratedCamera", deviceId);
        return _sessionRegistry.GetOrAdd(
            sessionId,
            () => new IntegratedCameraSession(_client, deviceId, camera.Index, camera.DisplayName));
    }

    private void BindSession(IntegratedCameraSession session)
    {
        if (ReferenceEquals(_session, session))
        {
            return;
        }

        UnbindSession();
        _session = session;
        session.State.Changed += OnSessionStateChanged;
        session.Diagnostics.Changed += OnSessionDiagnosticsChanged;
        session.AppliedSettings.Changed += OnSessionAppliedSettingsChanged;
        session.SessionEnd.Changed += OnSessionEndChanged;
        session.LatestFrame.Changed += OnSessionFrameChanged;

        ApplySessionState(session.State.Current!);
        ApplySessionDiagnostics(session.Diagnostics.Current!);
        ApplySessionAppliedSettings(session.AppliedSettings.Current);
        ApplySessionEnd(session.SessionEnd.Current);
        if (session.LatestFrame.Current is not null)
        {
            ApplySessionFrame(session.LatestFrame.Current);
        }
    }

    private void UnbindSession()
    {
        if (_session is null)
        {
            return;
        }

        _session.State.Changed -= OnSessionStateChanged;
        _session.Diagnostics.Changed -= OnSessionDiagnosticsChanged;
        _session.AppliedSettings.Changed -= OnSessionAppliedSettingsChanged;
        _session.SessionEnd.Changed -= OnSessionEndChanged;
        _session.LatestFrame.Changed -= OnSessionFrameChanged;
        _session = null;
    }

    private void OnSessionStateChanged(IntegratedCameraSessionState state) => _ = RunOnUiAsync(() => ApplySessionState(state));
    private void OnSessionDiagnosticsChanged(DeviceDiagnosticsSnapshot snapshot) => _ = RunOnUiAsync(() => ApplySessionDiagnostics(snapshot));
    private void OnSessionAppliedSettingsChanged(IntegratedCameraCaptureSettings? settings) => _ = RunOnUiAsync(() => ApplySessionAppliedSettings(settings));
    private void OnSessionEndChanged(DeviceSessionEndSnapshot? snapshot) => _ = RunOnUiAsync(() => ApplySessionEnd(snapshot));
    private void OnSessionFrameChanged(IntegratedCameraFrame? frame)
    {
        if (frame is not null)
        {
            _ = RunOnUiAsync(() => ApplySessionFrame(frame));
        }
    }

    private void ApplySessionState(IntegratedCameraSessionState state)
    {
        _isBusy = state.Busy;
        _statusMessage = state.StatusMessage;
        _lastFrameCapturedAt = state.LastFrameCapturedAt;
        _frameSequence = state.FrameSequence;
        _lastSourceMode = state.LastSourceMode;
        IsConnected = state.Connected;
        IsLivePreviewing = state.LivePreviewing;
        _frameRateInput = state.TargetFrameRate.ToString("0.###", CultureInfo.InvariantCulture);
        _draftFrameRateInput = _frameRateInput;
        _colorMode = state.ColorEnabled ? "Color" : "Mono";
        _draftColorMode = _colorMode;
        SyncFooter();
        OnPropertyChanged(nameof(FrameRateInputDraft));
        OnPropertyChanged(nameof(SelectedColorOptionDraft));
        OnPropertyChanged(nameof(CanApplySettings));
        _lifecycleActionCommand.NotifyCanExecuteChanged();
    }

    private void ApplySessionDiagnostics(DeviceDiagnosticsSnapshot snapshot)
    {
        _lastCommand = snapshot.LastCommand;
        _lastHardwareResponse = snapshot.LastHardwareResponse;
        _lastError = snapshot.LastError;
        _lastStateTransition = snapshot.LastStateTransition;
        _lastValidationResult = snapshot.LastValidationResult;
        RefreshDiagnosticsOutput();
        RefreshStatusOutput();
    }

    private void ApplySessionAppliedSettings(IntegratedCameraCaptureSettings? settings)
    {
        if (settings is null)
        {
            return;
        }

        _frameRateInput = settings.TargetFrameRate.ToString("0.###", CultureInfo.InvariantCulture);
        _draftFrameRateInput = _frameRateInput;
        _colorMode = settings.ColorEnabled ? "Color" : "Mono";
        _draftColorMode = _colorMode;
        RefreshAppliedSettingsOutput();
        SyncFooter();
        OnPropertyChanged(nameof(FrameRateInputDraft));
        OnPropertyChanged(nameof(SelectedColorOptionDraft));
        OnPropertyChanged(nameof(CanApplySettings));
        _lifecycleActionCommand.NotifyCanExecuteChanged();
    }

    private void ApplySessionEnd(DeviceSessionEndSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            SessionEndOutput = null;
            return;
        }

        SessionEndOutput = new IntegrationPanelSessionEndOutput
        {
            EndedAt = snapshot.EndedAt,
            ExitReason = $"{snapshot.ReasonCode}: {snapshot.ReasonMessage}",
            ConnectionClosed = snapshot.ConnectionClosed,
            LiveStopped = snapshot.LiveStopped,
            AppliedSettingsSnapshot = AppliedSettingsOutput,
            FinalStatus = BuildStatusOutput()
        };
    }

    private void ApplySessionFrame(IntegratedCameraFrame frame)
    {
        var nowTicks = Stopwatch.GetTimestamp();
        if ((_lastSourceMode ?? "session") == "live" && _lastDisplayTimestamp.HasValue)
        {
            var minTicks = Stopwatch.Frequency / MaxDisplayFps;
            if (nowTicks - _lastDisplayTimestamp.Value < minTicks)
            {
                UpdateMeasuredFrameRate(frame.TimestampTicks);
                return;
            }
        }

        _lastDisplayTimestamp = nowTicks;
        UpdateMeasuredFrameRate(frame.TimestampTicks);
        ApplyFrame(frame);
        RefreshAppliedSettingsOutput();
    }

    private void RefreshAppliedSettingsOutput()
    {
        var settings = _session?.AppliedSettings.Current;
        if (settings is null)
        {
            return;
        }

        AppliedSettingsOutput = new IntegrationPanelAppliedSettingsOutput
        {
            AppliedAt = DateTimeOffset.Now,
            DeviceSettings = new Dictionary<string, string?>
            {
                ["Device"] = Title,
                ["Camera"] = settings.DisplayName
            },
            EndpointSettings = new Dictionary<string, string?>
            {
                ["FrameRateFps"] = settings.TargetFrameRate.ToString("0.###", CultureInfo.InvariantCulture),
                ["ColorMode"] = settings.ColorEnabled ? "Color" : "Mono"
            },
            SessionSettings = new Dictionary<string, string?>
            {
                ["Connected"] = IsConnected ? "true" : "false",
                ["LivePreviewing"] = IsLivePreviewing ? "true" : "false"
            },
            NormalizationNotes = ["Mapped from runtime session settings."]
        };
        AppliedSettingsOutput = AppliedSettingsOutput with
        {
            SessionSettings = AppliedSettingsOutput.SessionSettings.WithOutputSettings(_outputSettings)
        };
    }

    private static async Task RunSessionDisposeAsync(IntegratedCameraSession session, string reason)
    {
        try
        {
            await session.DisconnectAsync(StopReason.UserRequested(reason)).ConfigureAwait(false);
            await session.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[IntegratedCameraPanelViewModel] Session dispose failed: {ex}");
        }
    }
}
