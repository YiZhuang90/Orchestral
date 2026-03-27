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
using ExperimentalControlPlatform.Runtime;
using MahApps.Metro.IconPacks;

namespace ExperimentalControlPlatform.App.DevicePanels.HuaTeng;

public sealed class HuaTengPanelViewModel : ObservableObject, ICameraPanelViewModel, IOutputSettingsPanelViewModel
{
    private const double MaxDisplayFps = 20.0;
    private static readonly IReadOnlyList<IntegrationPanelLifecycleAction> ConnectedLifecycleActions =
    [
        IntegrationPanelLifecycleAction.Apply
    ];
    private readonly HuaTengCameraProbeClient _probeClient;
    private readonly IDeviceSessionRegistry _sessionRegistry;
    private readonly AsyncRelayCommand _lifecycleActionCommand;
    private readonly IntegrationPanelOutputPublisher _outputPublisher = new();
    private HuaTengCameraSession? _session;
    private bool _isBusy;
    private bool _isConnecting;
    private bool _isConnected;
    private bool _isLivePreviewing;
    private IReadOnlyList<HuaTengCameraInfo> _cameraOptions = [];
    private HuaTengCameraInfo? _selectedCamera;
    private string _selectedPixelFormat = "Auto";
    private string _selectedTriggerMode = "Triggered";
    private string _selectedColorTone = "Neutral";
    private string _exposureInput = "1000";
    private string _frameRateInput = "3";
    private string _draftSelectedPixelFormat = "Auto";
    private string _draftSelectedTriggerMode = "Triggered";
    private string _draftSelectedColorTone = "Neutral";
    private string _draftExposureInput = "1000";
    private string _draftFrameRateInput = "3";
    private ImageSource? _previewImage;
    private string _currentFrameValue = "No frame captured";
    private string _resolutionText = "--";
    private string _pixelFormatText = "--";
    private string _exposureText = "--";
    private string _frameRateText = "--";
    private string _currentRoiValue = "Full frame";
    private ImageSource? _frozenBackgroundImage;
    private string _footerConnectionLabel = "Hardware: Disconnected";
    private string _footerCameraLabel = "No camera";
    private string _footerFormatLabel = "Auto";
    private string _footerTriggerLabel = "Triggered";
    private string _footerSystemStateLabel = "System Ready";
    private string _statusMessage = "Ready to discover camera";
    private List<string> _latestDiagnostics = ["No diagnostics collected yet."];
    private long? _lastFrameTimestamp;
    private int? _lastCaptureTimestampTenths;
    private long? _lastDisplayTimestamp;
    private double? _smoothedFrameRate;
    private int _currentFrameWidth;
    private int _currentFrameHeight;
    private int _fullFrameWidth;
    private int _fullFrameHeight;
    private bool _hasRoiSelection;
    private bool _isRoiEditMode;
    private Rect? _appliedRoiPixels;
    private Rect _roiRectNormalized = new(0.2, 0.2, 0.6, 0.6);
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
        3.0,
        true);

    public HuaTengPanelViewModel(HuaTengCameraProbeClient probeClient, IDeviceSessionRegistry sessionRegistry)
    {
        _probeClient = probeClient ?? throw new ArgumentNullException(nameof(probeClient));
        _sessionRegistry = sessionRegistry ?? throw new ArgumentNullException(nameof(sessionRegistry));
        _lifecycleActionCommand = new AsyncRelayCommand(ExecuteLifecycleActionAsync, CanExecuteLifecycleAction, HandleLifecycleCommandException);
        RefreshStatusOutput();
        RefreshDiagnosticsOutput();
    }

    public string Title => "HuaTeng Camera";

    System.Collections.IEnumerable ICameraPanelViewModel.CameraOptions => CameraOptions;

    public IReadOnlyList<string> ChannelOptions { get; } = [];

    public string PrimaryValueLabel => "Current ROI: ";

    public IReadOnlyList<HuaTengCameraInfo> CameraOptions
    {
        get => _cameraOptions;
        private set
        {
            _cameraOptions = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedCameraItem));
        }
    }

    public HuaTengCameraInfo? SelectedCamera
    {
        get => _selectedCamera;
        set
        {
            if (SetProperty(ref _selectedCamera, value))
            {
                OnPropertyChanged(nameof(SelectedCameraItem));
                SyncFooter();
                RefreshStatusOutput();
            }
        }
    }

    public object? SelectedCameraItem
    {
        get => SelectedCamera;
        set => SelectedCamera = value as HuaTengCameraInfo;
    }

    public IReadOnlyList<string> PixelFormatOptions { get; } = ["Auto", "Mono8", "Bgr8"];

    public IReadOnlyList<string> ColorOptions => PixelFormatOptions;

    public IReadOnlyList<string> TriggerModeOptions { get; } = ["Triggered", "Continuous"];

    public IReadOnlyList<string> ColorToneOptions { get; } = ["Neutral", "Warm", "Cool", "Auto"];

    public string SelectedPixelFormatDraft
    {
        get => _draftSelectedPixelFormat;
        set
        {
            if (SetProperty(ref _draftSelectedPixelFormat, value))
            {
                OnPropertyChanged(nameof(SelectedColorOptionDraft));
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string SelectedColorOptionDraft
    {
        get => SelectedPixelFormatDraft;
        set => SelectedPixelFormatDraft = value;
    }

    public string SelectedTriggerModeDraft
    {
        get => _draftSelectedTriggerMode;
        set
        {
            if (SetProperty(ref _draftSelectedTriggerMode, value))
            {
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string ExposureInputDraft
    {
        get => _draftExposureInput;
        set
        {
            if (SetProperty(ref _draftExposureInput, value))
            {
                OnPropertyChanged(nameof(CanApplySettings));
                _lifecycleActionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public Visibility ExposureControlVisibility => Visibility.Visible;

    public Visibility AdvancedSettingsVisibility => Visibility.Visible;

    public Visibility RoiToolbarVisibility => Visibility.Visible;

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

    public ImageSource? PreviewImage
    {
        get => _previewImage;
        private set
        {
            if (SetProperty(ref _previewImage, value))
            {
                OnPropertyChanged(nameof(BackgroundFrameImage));
                OnPropertyChanged(nameof(CanSetRoi));
            }
        }
    }

    public ImageSource? BackgroundFrameImage
    {
        get => _hasRoiSelection ? _frozenBackgroundImage ?? _previewImage : _previewImage;
    }

    public string CurrentFrameValue
    {
        get => _currentFrameValue;
        private set => SetProperty(ref _currentFrameValue, value);
    }

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

    public string CurrentRoiLabel => "Current ROI: ";

    public string CurrentRoiValue
    {
        get => _currentRoiValue;
        private set
        {
            if (SetProperty(ref _currentRoiValue, value))
            {
                OnPropertyChanged(nameof(CurrentPrimaryValue));
            }
        }
    }

    public string CurrentPrimaryValue => CurrentRoiValue;

    public Rect RoiRectNormalized
    {
        get => _roiRectNormalized;
        set
        {
            if (SetProperty(ref _roiRectNormalized, value))
            {
                UpdateRoiSummary();
            }
        }
    }

    public Visibility RoiOverlayVisibility => _hasRoiSelection ? Visibility.Visible : Visibility.Collapsed;

    public Visibility RoiHandleVisibility => _hasRoiSelection && _isRoiEditMode ? Visibility.Visible : Visibility.Collapsed;

    public Visibility RoiLiveVisibility => _hasRoiSelection && IsLivePreviewing && !_isRoiEditMode ? Visibility.Visible : Visibility.Collapsed;

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

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string DiagnosticsTitle => "HuaTeng camera diagnostics";

    public string DiagnosticsSubtitle => "USB camera state and frame pipeline";

    public IReadOnlyList<IntegrationPanelOutputPayloadType> SupportedOutputPayloadTypes { get; } =
    [
        IntegrationPanelOutputPayloadType.Image
    ];

    public IntegrationPanelOutputSettings CurrentOutputSettings => _outputSettings;

    public string OutputSettingsSubtitle => "Shape the HuaTeng frame output before it reaches the runtime bus.";

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
                SyncFooter();
                OnPropertyChanged(nameof(ConnectionToggleLabel));
                OnPropertyChanged(nameof(ConnectionToggleIconKind));
                OnPropertyChanged(nameof(CanToggleLive));
                OnPropertyChanged(nameof(CanSnapFrame));
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
                OnPropertyChanged(nameof(CanToggleLive));
                RefreshStatusOutput();
            }
        }
    }

    public bool CanToggleConnection => !_isConnecting;

    public bool CanToggleLive => !_isConnecting && IsConnected;

    public bool CanSnapFrame => !_isConnecting && IsConnected && !IsLivePreviewing;

    public bool CanApplySettings => !_isConnecting && HasPendingSettings();

    public bool CanSetRoi => PreviewImage is not null;

    public bool CanClearRoi => _hasRoiSelection;

    public string ConnectionToggleLabel => IsConnected ? "Disconnect" : "Connect";

    public PackIconMaterialKind ConnectionToggleIconKind => IsConnected
        ? PackIconMaterialKind.LinkVariantOff
        : PackIconMaterialKind.LinkVariant;

    public string LiveToggleLabel => IsLivePreviewing ? "Stop live" : "Start live";

    public PackIconMaterialKind LiveToggleIconKind => IsLivePreviewing
        ? PackIconMaterialKind.Stop
        : PackIconMaterialKind.Play;

    public string EmptyStateText => "No frame captured";

    public bool IsRoiActive => _hasRoiSelection;

    public void ApplyDefaultRoi()
    {
        _hasRoiSelection = true;
        _isRoiEditMode = true;
        _appliedRoiPixels = null;
        _frozenBackgroundImage = PreviewImage;
        RoiRectNormalized = new Rect(0.2, 0.2, 0.6, 0.6);
        OnPropertyChanged(nameof(BackgroundFrameImage));
        OnPropertyChanged(nameof(RoiOverlayVisibility));
        OnPropertyChanged(nameof(RoiHandleVisibility));
        OnPropertyChanged(nameof(RoiLiveVisibility));
        OnPropertyChanged(nameof(CanClearRoi));
    }

    public void ClearRoi()
    {
        _hasRoiSelection = false;
        _isRoiEditMode = false;
        _appliedRoiPixels = null;
        _frozenBackgroundImage = null;
        CurrentRoiValue = "Full frame";
        OnPropertyChanged(nameof(BackgroundFrameImage));
        OnPropertyChanged(nameof(RoiOverlayVisibility));
        OnPropertyChanged(nameof(RoiHandleVisibility));
        OnPropertyChanged(nameof(RoiLiveVisibility));
        OnPropertyChanged(nameof(CanClearRoi));
    }

    public async Task BeginRoiEditAsync()
    {
        if (!_hasRoiSelection)
        {
            ApplyDefaultRoi();
        }

        if (IsLivePreviewing)
        {
            await StopLivePreviewAsync().ConfigureAwait(false);
        }

        _isRoiEditMode = true;
        _frozenBackgroundImage ??= PreviewImage;
        OnPropertyChanged(nameof(BackgroundFrameImage));
        OnPropertyChanged(nameof(RoiHandleVisibility));
        OnPropertyChanged(nameof(RoiLiveVisibility));
    }

    public async Task ApplyRoiAsync()
    {
        if (_session is null || !_hasRoiSelection || SelectedCamera is null)
        {
            return;
        }

        var roiPixels = GetAppliedRoiPixels();
        if (roiPixels is null)
        {
            return;
        }

        try
        {
            await _session.ApplyRoiAsync(ToCaptureRegion(roiPixels.Value)).ConfigureAwait(true);
            _isRoiEditMode = true;
            OnPropertyChanged(nameof(RoiHandleVisibility));
            OnPropertyChanged(nameof(RoiLiveVisibility));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HuaTengPanelViewModel] Apply ROI failed: {ex}");
        }
    }

    public CameraSettingsDialogViewModel CreateDetailedSettingsDialog()
    {
        return new CameraSettingsDialogViewModel(
            "Trigger mode and color tone live here. Additional advanced camera parameters will follow the same pattern.",
            TriggerModeOptions,
            _draftSelectedTriggerMode,
            ColorToneOptions,
            _draftSelectedColorTone);
    }

    public async Task ApplySettingsAsync()
    {
        if (!TryParsePositiveDouble(_draftExposureInput, out _))
        {
            SetLastValidationResult("Exposure must be a positive number.");
            StatusMessage = "Exposure must be a positive number.";
            FooterSystemStateLabel = "Invalid setting";
            return;
        }

        if (!TryParsePositiveDouble(_draftFrameRateInput, out _))
        {
            SetLastValidationResult("Target frame rate must be a positive number.");
            StatusMessage = "Target frame rate must be a positive number.";
            FooterSystemStateLabel = "Invalid setting";
            return;
        }

        SetLastValidationResult("Validated settings.");
        _selectedPixelFormat = _draftSelectedPixelFormat;
        _selectedTriggerMode = _draftSelectedTriggerMode;
        _exposureInput = _draftExposureInput;
        _frameRateInput = _draftFrameRateInput;

        SyncFooter();
        OnPropertyChanged(nameof(CanApplySettings));
        _lifecycleActionCommand.NotifyCanExecuteChanged();

        if (_session is null)
        {
            StatusMessage = "Camera settings staged for the next capture.";
            FooterSystemStateLabel = "System Ready";
            return;
        }

        try
        {
            await _session.ApplySettingsAsync(BuildCaptureSettings()).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HuaTengPanelViewModel] Apply settings failed: {ex}");
        }
    }

    public async Task ApplyDetailedSettingsAsync(CameraSettingsDialogViewModel dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        SetLastCommand("Apply detailed camera settings");
        _draftSelectedTriggerMode = dialog.SelectedTriggerMode;
        _selectedTriggerMode = dialog.SelectedTriggerMode;
        _draftSelectedColorTone = dialog.SelectedColorTone;
        _selectedColorTone = dialog.SelectedColorTone;
        SetLastValidationResult("Validated detailed settings.");
        SyncFooter();

        OnPropertyChanged(nameof(SelectedTriggerModeDraft));
        OnPropertyChanged(nameof(CanApplySettings));
        _lifecycleActionCommand.NotifyCanExecuteChanged();

        if (_session is null)
        {
            StatusMessage = "Detailed settings staged for the next capture.";
            return;
        }

        try
        {
            await _session.ApplySettingsAsync(BuildCaptureSettings()).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HuaTengPanelViewModel] Apply detailed settings failed: {ex}");
        }
    }

    public Task ApplyOutputSettingsAsync(IntegrationPanelOutputSettings settings)
    {
        _outputSettings = settings;
        _outputPublisher.Reset();
        SetLastCommand("Apply HuaTeng output settings");
        SetLastValidationResult("Validated HuaTeng output settings.");
        SetLastStateTransition("Applied HuaTeng output settings");
        StatusMessage = "HuaTeng output settings updated.";
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

    public async Task RefreshCamerasAsync()
    {
        await RunBusyAsync(async () =>
        {
            SetLastCommand("List cameras");
            var probe = await _probeClient.ListCamerasAsync().ConfigureAwait(false);
            _latestDiagnostics = probe.Diagnostics.ToList();
            SetLastHardwareResponse(probe.Summary);

            CameraOptions = probe.Cameras;
            SelectedCamera ??= CameraOptions.FirstOrDefault();
            if (SelectedCamera is not null)
            {
                SelectedCamera = CameraOptions.FirstOrDefault(camera => camera.SerialNumber == SelectedCamera.SerialNumber)
                    ?? CameraOptions.FirstOrDefault();
            }

            if (CameraOptions.Count == 0)
            {
                IsConnected = false;
                FooterSystemStateLabel = "No camera";
                StatusMessage = probe.Summary;
            }
            else
            {
                ClearLastError();
                StatusMessage = probe.Summary;
            }
        }).ConfigureAwait(false);
    }

    public async Task ConnectAsync()
    {
        if (_isConnecting)
        {
            return;
        }

        _isConnecting = true;
        RaiseCommandState();

        try
        {
            await RefreshCamerasAsync().ConfigureAwait(false);
            if (SelectedCamera is null)
            {
                SetLastError("No HuaTeng camera is available over USB.");
                StatusMessage = "No HuaTeng camera is available over USB.";
                FooterSystemStateLabel = "No camera";
                return;
            }

            var session = GetOrCreateSession();
            BindSession(session);
            await session.ConnectAsync(BuildCaptureSettings()).ConfigureAwait(true);
            _isRoiEditMode = false;
            OnPropertyChanged(nameof(RoiHandleVisibility));
            OnPropertyChanged(nameof(RoiLiveVisibility));
        }
        finally
        {
            _isConnecting = false;
            RaiseCommandState();
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
            await session.DisconnectAsync(StopReason.UserRequested("Disconnected HuaTeng panel.")).ConfigureAwait(true);
        }
        finally
        {
            _sessionRegistry.Remove(session.SessionId);
            UnbindSession();
            await session.DisposeAsync().ConfigureAwait(true);
        }
    }

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
            Debug.WriteLine($"[HuaTengPanelViewModel] Snap failed: {ex}");
        }
    }

    public async Task StartLivePreviewAsync()
    {
        if (IsLivePreviewing)
        {
            return;
        }

        if (!IsConnected)
        {
            await ConnectAsync().ConfigureAwait(false);
        }

        if (!IsConnected)
        {
            return;
        }

        if (SelectedCamera is null)
        {
            await RefreshCamerasAsync().ConfigureAwait(false);
        }

        var camera = SelectedCamera ?? CameraOptions.FirstOrDefault();
        if (camera is null)
        {
            IsConnected = false;
            StatusMessage = "No HuaTeng camera is available over USB.";
            FooterSystemStateLabel = "No camera";
            return;
        }

        if (!ReferenceEquals(SelectedCamera, camera))
        {
            SelectedCamera = camera;
        }

        _isRoiEditMode = false;
        _lastFrameTimestamp = null;
        _lastCaptureTimestampTenths = null;
        _lastDisplayTimestamp = null;
        _smoothedFrameRate = null;
        FrameRateText = "--";
        OnPropertyChanged(nameof(RoiHandleVisibility));
        OnPropertyChanged(nameof(RoiLiveVisibility));

        try
        {
            await _session!.StartLiveAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HuaTengPanelViewModel] Start live failed: {ex}");
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
            Debug.WriteLine($"[HuaTengPanelViewModel] Stop live failed: {ex}");
        }

        OnPropertyChanged(nameof(RoiLiveVisibility));
    }

    public string GetDiagnosticsSummary()
    {
        var cameraName = SelectedCamera?.DisplayName ?? "None";
        return string.Join(Environment.NewLine,
        [
            $"Device: {cameraName}",
            $"Connection: {(IsConnected ? "Connected" : "Disconnected")}",
            $"Port type: {SelectedCamera?.PortType ?? "--"}",
            $"Serial: {SelectedCamera?.SerialNumber ?? "--"}",
            $"Pixel format: {FooterFormatLabel}",
            $"Trigger mode: {FooterTriggerLabel}",
            $"Exposure: {ExposureText}",
            $"Target frame rate: {_frameRateInput} fps",
            $"Status: {StatusMessage}",
            .._latestDiagnostics.Select((line, index) => $"Detail {index + 1}: {line}")
        ]);
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
        _ = RunSessionDisposeAsync(session, "Disposed HuaTeng panel.");
    }

    private async Task RunBusyAsync(Func<Task> operation)
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        RaiseCommandState();

        try
        {
            await operation().ConfigureAwait(false);
        }
        finally
        {
            _isBusy = false;
            RaiseCommandState();
        }
    }

    private void RaiseCommandState()
    {
        OnPropertyChanged(nameof(CanToggleConnection));
        OnPropertyChanged(nameof(CanToggleLive));
        OnPropertyChanged(nameof(CanSnapFrame));
        OnPropertyChanged(nameof(CanApplySettings));
        OnPropertyChanged(nameof(CanSetRoi));
        OnPropertyChanged(nameof(CanClearRoi));
        OnPropertyChanged(nameof(RoiHandleVisibility));
        OnPropertyChanged(nameof(RoiLiveVisibility));
        RefreshStatusOutput();
        _lifecycleActionCommand.NotifyCanExecuteChanged();
    }

    private static double? ParseExposure(string exposureInput)
    {
        return double.TryParse(exposureInput, NumberStyles.Float, CultureInfo.InvariantCulture, out var exposure)
            ? exposure
            : null;
    }

    private void SyncFooter()
    {
        FooterConnectionLabel = IsConnected ? "Hardware: Connected" : "Hardware: Disconnected";
        FooterCameraLabel = SelectedCamera?.DisplayName ?? "No camera";
        FooterFormatLabel = _selectedPixelFormat;
        FooterTriggerLabel = _selectedTriggerMode;
    }

    private HuaTengCaptureSettings BuildCaptureSettings()
    {
        var camera = SelectedCamera ?? throw new InvalidOperationException("No HuaTeng camera selected.");
        var deviceId = string.IsNullOrWhiteSpace(camera.SerialNumber)
            ? $"{camera.Index}:{camera.DisplayName}"
            : camera.SerialNumber;
        return new HuaTengCaptureSettings(
            deviceId,
            camera.Index,
            camera.DisplayName,
            _selectedPixelFormat,
            _selectedTriggerMode,
            _selectedColorTone,
            ParseExposure(_exposureInput),
            ParseFrameRate(),
            ToCaptureRegion(_appliedRoiPixels ?? GetAppliedRoiPixels()));
    }

    private HuaTengCameraSession GetOrCreateSession()
    {
        var camera = SelectedCamera ?? throw new InvalidOperationException("No HuaTeng camera selected.");
        var deviceId = string.IsNullOrWhiteSpace(camera.SerialNumber)
            ? $"{camera.Index}:{camera.DisplayName}"
            : camera.SerialNumber;
        var sessionId = new DeviceSessionId("HuaTengCamera", deviceId);
        return _sessionRegistry.GetOrAdd(
            sessionId,
            () => new HuaTengCameraSession(_probeClient, deviceId, camera.Index, camera.DisplayName));
    }

    private void BindSession(HuaTengCameraSession session)
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

    private void OnSessionStateChanged(HuaTengSessionState state) => _ = RunOnUiAsync(() => ApplySessionState(state));
    private void OnSessionDiagnosticsChanged(DeviceDiagnosticsSnapshot snapshot) => _ = RunOnUiAsync(() => ApplySessionDiagnostics(snapshot));
    private void OnSessionAppliedSettingsChanged(HuaTengCaptureSettings? settings) => _ = RunOnUiAsync(() => ApplySessionAppliedSettings(settings));
    private void OnSessionEndChanged(DeviceSessionEndSnapshot? snapshot) => _ = RunOnUiAsync(() => ApplySessionEnd(snapshot));
    private void OnSessionFrameChanged(HuaTengFrame? frame)
    {
        if (frame is not null)
        {
            _ = RunOnUiAsync(() => ApplySessionFrame(frame));
        }
    }

    private bool HasPendingSettings()
    {
        return !string.Equals(_selectedPixelFormat, _draftSelectedPixelFormat, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_selectedTriggerMode, _draftSelectedTriggerMode, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(_exposureInput, _draftExposureInput, StringComparison.Ordinal)
            || !string.Equals(_frameRateInput, _draftFrameRateInput, StringComparison.Ordinal);
    }

    private double ParseFrameRate()
    {
        return TryParsePositiveDouble(_frameRateInput, out var fps) ? fps : 3.0;
    }

    private static bool TryParsePositiveDouble(string input, out double value)
    {
        if (double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) && parsed > 0)
        {
            value = parsed;
            return true;
        }

        value = 0;
        return false;
    }

    private static ImageSource LoadImage(HuaTengFrameResult result)
    {
        var pixelFormat = result.IsMono ? PixelFormats.Gray8 : PixelFormats.Bgr24;
        var stride = result.IsMono ? result.Width : result.Width * 3;
        var image = BitmapSource.Create(
            result.Width,
            result.Height,
            96,
            96,
            pixelFormat,
            null,
            result.PixelData,
            stride);
        image.Freeze();
        return image;
    }

    private static ImageSource LoadImage(HuaTengFrame frame)
    {
        var pixelFormat = frame.IsMono ? PixelFormats.Gray8 : PixelFormats.Bgr24;
        var stride = frame.IsMono ? frame.Width : frame.Width * 3;
        var image = BitmapSource.Create(
            frame.Width,
            frame.Height,
            96,
            96,
            pixelFormat,
            null,
            frame.PixelData,
            stride);
        image.Freeze();
        return image;
    }

    private void ApplySessionState(HuaTengSessionState state)
    {
        _isBusy = state.Busy;
        IsConnected = state.Connected;
        IsLivePreviewing = state.LivePreviewing;
        StatusMessage = state.StatusMessage;
        _selectedPixelFormat = state.PixelFormat;
        _draftSelectedPixelFormat = state.PixelFormat;
        _selectedTriggerMode = state.TriggerMode;
        _draftSelectedTriggerMode = state.TriggerMode;
        _selectedColorTone = state.ColorTone;
        _draftSelectedColorTone = state.ColorTone;
        _exposureInput = state.ExposureUs?.ToString("0.###", CultureInfo.InvariantCulture) ?? _exposureInput;
        _draftExposureInput = _exposureInput;
        _frameRateInput = state.TargetFrameRate.ToString("0.###", CultureInfo.InvariantCulture);
        _draftFrameRateInput = _frameRateInput;
        _lastFrameCapturedAt = state.LastFrameCapturedAt;
        _frameSequence = state.FrameSequence;
        _lastSourceMode = state.LastSourceMode;
        if (state.AppliedRoi is not null)
        {
            _appliedRoiPixels = ToRect(state.AppliedRoi);
            _hasRoiSelection = true;
            UpdateNormalizedRoiFromAppliedPixels();
        }
        else if (!IsLivePreviewing)
        {
            _appliedRoiPixels = null;
            _hasRoiSelection = false;
        }

        SyncFooter();
        RaiseCommandState();
        RefreshAppliedSettingsOutput();
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

    private void ApplySessionAppliedSettings(HuaTengCaptureSettings? settings)
    {
        if (settings is null)
        {
            return;
        }

        _selectedPixelFormat = settings.PixelFormat;
        _draftSelectedPixelFormat = settings.PixelFormat;
        _selectedTriggerMode = settings.TriggerMode;
        _draftSelectedTriggerMode = settings.TriggerMode;
        _selectedColorTone = settings.ColorTone;
        _draftSelectedColorTone = settings.ColorTone;
        _exposureInput = settings.ExposureUs?.ToString("0.###", CultureInfo.InvariantCulture) ?? _exposureInput;
        _draftExposureInput = _exposureInput;
        _frameRateInput = settings.TargetFrameRate.ToString("0.###", CultureInfo.InvariantCulture);
        _draftFrameRateInput = _frameRateInput;
        _appliedRoiPixels = ToRect(settings.Roi);
        _hasRoiSelection = settings.Roi is not null;
        if (_hasRoiSelection)
        {
            UpdateNormalizedRoiFromAppliedPixels();
        }

        SyncFooter();
        RefreshAppliedSettingsOutput();
        OnPropertyChanged(nameof(CanApplySettings));
        OnPropertyChanged(nameof(SelectedColorOptionDraft));
        OnPropertyChanged(nameof(SelectedTriggerModeDraft));
        RaiseCommandState();
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

    private void ApplySessionFrame(HuaTengFrame frame)
    {
        _latestDiagnostics = frame.Diagnostics.ToList();
        if (!frame.Ok)
        {
            return;
        }

        PreviewImage = LoadImage(frame);
        _currentFrameWidth = frame.Width;
        _currentFrameHeight = frame.Height;
        if (!_hasRoiSelection)
        {
            _fullFrameWidth = frame.Width;
            _fullFrameHeight = frame.Height;
        }

        CurrentFrameValue = $"{frame.Width} x {frame.Height}";
        ResolutionText = $"{frame.Width} x {frame.Height}";
        PixelFormatText = frame.PixelFormat;
        ExposureText = $"{frame.ExposureUs:0} us";
        UpdateMeasuredFrameRate(frame.TimestampTenthsOfMilliseconds);
        StatusMessage = frame.Summary;
        FooterSystemStateLabel = IsLivePreviewing ? "Streaming" : "Frame ready";
        FooterFormatLabel = frame.PixelFormat;
        FooterTriggerLabel = frame.TriggerMode;
        _appliedRoiPixels = ToRect(frame.Roi);
        _hasRoiSelection = frame.Roi is not null;
        if (_hasRoiSelection)
        {
            UpdateNormalizedRoiFromAppliedPixels();
        }

        UpdateRoiSummary();
        CaptureFrameOutput(frame, _lastSourceMode ?? "session");
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
                ["PixelFormat"] = settings.PixelFormat,
                ["TriggerMode"] = settings.TriggerMode,
                ["ColorTone"] = settings.ColorTone,
                ["ExposureUs"] = settings.ExposureUs?.ToString("0.###", CultureInfo.InvariantCulture),
                ["TargetFrameRateFps"] = settings.TargetFrameRate.ToString("0.###", CultureInfo.InvariantCulture)
            },
            SessionSettings = new Dictionary<string, string?>
            {
                ["LivePreviewing"] = IsLivePreviewing ? "true" : "false",
                ["AppliedRoi"] = _appliedRoiPixels.HasValue ? CurrentRoiValue : "Full frame"
            },
            NormalizationNotes = ["Mapped from runtime session settings."]
        };
        AppliedSettingsOutput = AppliedSettingsOutput with
        {
            SessionSettings = AppliedSettingsOutput.SessionSettings.WithOutputSettings(_outputSettings)
        };
    }

    private static CaptureRegion? ToCaptureRegion(Rect? rect)
    {
        return rect is null ? null : new CaptureRegion(rect.Value.X, rect.Value.Y, rect.Value.Width, rect.Value.Height);
    }

    private static Rect? ToRect(CaptureRegion? region)
    {
        return region is null ? null : new Rect(region.X, region.Y, region.Width, region.Height);
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

    private static async Task RunSessionDisposeAsync(HuaTengCameraSession session, string reason)
    {
        try
        {
            await session.DisconnectAsync(StopReason.UserRequested(reason)).ConfigureAwait(false);
            await session.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[HuaTengPanelViewModel] Session dispose failed: {ex}");
        }
    }

    private Task ApplyLiveFrameAsync(HuaTengFrameResult result)
    {
        return Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _latestDiagnostics = result.Diagnostics.ToList();
            if (!result.Ok)
            {
                SetLastHardwareResponse(result.Summary);
                SetLastError(result.Summary);
                StatusMessage = result.Summary;
                FooterSystemStateLabel = "Fault";
                return;
            }

            ClearLastError();
            SetLastHardwareResponse(result.Summary);
            _currentFrameWidth = result.Width;
            _currentFrameHeight = result.Height;
            if (!_hasRoiSelection)
            {
                _fullFrameWidth = result.Width;
                _fullFrameHeight = result.Height;
            }

            CurrentFrameValue = $"{result.Width} x {result.Height}";
            ResolutionText = $"{result.Width} x {result.Height}";
            PixelFormatText = result.PixelFormat;
            ExposureText = $"{result.ExposureUs:0} us";
            UpdateMeasuredFrameRate(result);
            StatusMessage = result.Summary;
            FooterSystemStateLabel = "Streaming";
            FooterFormatLabel = result.PixelFormat;
            FooterTriggerLabel = result.TriggerMode;
            if (result.Camera is not null)
            {
                SelectedCamera = result.Camera;
            }

            if (result.Roi.HasValue)
            {
                _appliedRoiPixels = result.Roi;
                UpdateNormalizedRoiFromAppliedPixels();
            }

            var nowTicks = Stopwatch.GetTimestamp();
            var displayIntervalTicks = (long)(Stopwatch.Frequency / MaxDisplayFps);
            if (!_lastDisplayTimestamp.HasValue || (nowTicks - _lastDisplayTimestamp.Value) >= displayIntervalTicks)
            {
                PreviewImage = LoadImage(result);
                _lastDisplayTimestamp = nowTicks;
            }

            OnPropertyChanged(nameof(CanSetRoi));
            UpdateRoiSummary();
            CaptureFrameOutput(result, "live");
        }).Task;
    }

    private async Task RestartLivePreviewAsync()
    {
        await StopLivePreviewAsync().ConfigureAwait(false);
        await StartLivePreviewAsync().ConfigureAwait(false);
    }

    private void UpdateMeasuredFrameRate(HuaTengFrameResult result)
    {
        UpdateMeasuredFrameRate(result.TimestampTenthsOfMilliseconds);
    }

    private void UpdateMeasuredFrameRate(int timestampTenthsOfMilliseconds)
    {
        if (timestampTenthsOfMilliseconds > 0)
        {
            if (_lastCaptureTimestampTenths.HasValue)
            {
                var deltaTenths = timestampTenthsOfMilliseconds - _lastCaptureTimestampTenths.Value;
                if (deltaTenths > 0)
                {
                    var seconds = deltaTenths / 10000.0;
                    var fps = 1.0 / seconds;
                    _smoothedFrameRate = _smoothedFrameRate.HasValue
                        ? (_smoothedFrameRate.Value * 0.7) + (fps * 0.3)
                        : fps;

                    FrameRateText = $"{_smoothedFrameRate.Value:0.0} fps";
                }
            }

            _lastCaptureTimestampTenths = timestampTenthsOfMilliseconds;
            _lastFrameTimestamp = Stopwatch.GetTimestamp();
            if (!_smoothedFrameRate.HasValue)
            {
                FrameRateText = "--";
            }
            return;
        }

        var nowTicks = Stopwatch.GetTimestamp();
        if (_lastFrameTimestamp.HasValue)
        {
            var seconds = (nowTicks - _lastFrameTimestamp.Value) / (double)Stopwatch.Frequency;
            if (seconds > 0)
            {
                var fps = 1.0 / seconds;
                _smoothedFrameRate = _smoothedFrameRate.HasValue
                    ? (_smoothedFrameRate.Value * 0.7) + (fps * 0.3)
                    : fps;

                FrameRateText = $"{_smoothedFrameRate.Value:0.0} fps";
            }
        }
        else
        {
            FrameRateText = "--";
        }

        _lastFrameTimestamp = nowTicks;
    }

    private void UpdateRoiSummary()
    {
        if (!_hasRoiSelection)
        {
            CurrentRoiValue = "Full frame";
            return;
        }

        if (_appliedRoiPixels.HasValue)
        {
            var applied = _appliedRoiPixels.Value;
            CurrentRoiValue = $"{(int)Math.Round(applied.Width)} x {(int)Math.Round(applied.Height)} @ ({(int)Math.Round(applied.X)}, {(int)Math.Round(applied.Y)})";
            return;
        }

        var sourceWidth = _fullFrameWidth > 0 ? _fullFrameWidth : _currentFrameWidth;
        var sourceHeight = _fullFrameHeight > 0 ? _fullFrameHeight : _currentFrameHeight;
        if (sourceWidth > 0 && sourceHeight > 0)
        {
            var draftX = (int)Math.Round(RoiRectNormalized.X * sourceWidth);
            var draftY = (int)Math.Round(RoiRectNormalized.Y * sourceHeight);
            var draftWidth = (int)Math.Round(RoiRectNormalized.Width * sourceWidth);
            var draftHeight = (int)Math.Round(RoiRectNormalized.Height * sourceHeight);
            CurrentRoiValue = $"{draftWidth} x {draftHeight} @ ({draftX}, {draftY})";
            return;
        }

        CurrentRoiValue = $"{RoiRectNormalized.Width * 100:0}% x {RoiRectNormalized.Height * 100:0}%";
    }

    private Rect? GetAppliedRoiPixels()
    {
        if (!_hasRoiSelection)
        {
            return null;
        }

        var sourceWidth = _fullFrameWidth > 0 ? _fullFrameWidth : _currentFrameWidth;
        var sourceHeight = _fullFrameHeight > 0 ? _fullFrameHeight : _currentFrameHeight;
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return null;
        }

        var x = Math.Round(RoiRectNormalized.X * sourceWidth);
        var y = Math.Round(RoiRectNormalized.Y * sourceHeight);
        var width = Math.Round(RoiRectNormalized.Width * sourceWidth);
        var height = Math.Round(RoiRectNormalized.Height * sourceHeight);

        x = Math.Max(0, x - (x % 2));
        y = Math.Max(0, y - (y % 2));
        width = Math.Max(12, width - (width % 12));
        height = Math.Max(12, height - (height % 12));
        width = Math.Min(width, sourceWidth - x);
        height = Math.Min(height, sourceHeight - y);

        width -= width % 12;
        height -= height % 12;
        width = Math.Max(12, width);
        height = Math.Max(12, height);

        if (x + width > sourceWidth)
        {
            x = Math.Max(0, sourceWidth - width);
        }

        if (y + height > sourceHeight)
        {
            y = Math.Max(0, sourceHeight - height);
        }

        return new Rect(x, y, width, height);
    }

    private void UpdateNormalizedRoiFromAppliedPixels()
    {
        if (!_appliedRoiPixels.HasValue)
        {
            return;
        }

        var sourceWidth = _fullFrameWidth > 0 ? _fullFrameWidth : _currentFrameWidth;
        var sourceHeight = _fullFrameHeight > 0 ? _fullFrameHeight : _currentFrameHeight;
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            return;
        }

        var applied = _appliedRoiPixels.Value;
        RoiRectNormalized = new Rect(
            applied.X / sourceWidth,
            applied.Y / sourceHeight,
            applied.Width / sourceWidth,
            applied.Height / sourceHeight);
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

        SetLastCommand("Apply camera settings");
        await ApplySettingsAsync().ConfigureAwait(false);
    }

    private void HandleLifecycleCommandException(Exception exception)
    {
        SetLastError(exception.Message);
        StatusMessage = exception.Message;
    }

    private void CaptureFrameOutput(HuaTengFrameResult result, string sourceMode)
    {
        _lastFrameCapturedAt = DateTimeOffset.Now;
        _frameSequence++;
        _lastSourceMode = sourceMode;
        PublishFrameOutput(SelectedCamera?.SerialNumber, SelectedCamera?.SerialNumber ?? SelectedCamera?.DisplayName, $"{result.Width}x{result.Height}; ROI={CurrentRoiValue}");
        CaptureAppliedSettingsSnapshot($"Verified during {sourceMode}.");
    }

    private void CaptureFrameOutput(HuaTengFrame frame, string sourceMode)
    {
        _lastFrameCapturedAt = DateTimeOffset.Now;
        _frameSequence++;
        _lastSourceMode = sourceMode;
        PublishFrameOutput(frame.DeviceId, SelectedCamera?.SerialNumber ?? SelectedCamera?.DisplayName, $"{frame.Width}x{frame.Height}; ROI={CurrentRoiValue}");
        RefreshAppliedSettingsOutput();
    }

    private void CaptureAppliedSettingsSnapshot(string note)
    {
        AppliedSettingsOutput = new IntegrationPanelAppliedSettingsOutput
        {
            AppliedAt = DateTimeOffset.Now,
            DeviceSettings = new Dictionary<string, string?>
            {
                ["Device"] = Title,
                ["Connection"] = SelectedCamera?.SerialNumber ?? "Disconnected",
                ["Camera"] = SelectedCamera?.DisplayName
            },
            EndpointSettings = new Dictionary<string, string?>
            {
                ["PixelFormat"] = _selectedPixelFormat,
                ["TriggerMode"] = _selectedTriggerMode,
                ["ColorTone"] = _selectedColorTone,
                ["ExposureUs"] = _exposureInput,
                ["TargetFrameRateFps"] = _frameRateInput
            },
            SessionSettings = new Dictionary<string, string?>
            {
                ["LivePreviewing"] = IsLivePreviewing ? "true" : "false",
                ["AppliedRoi"] = _appliedRoiPixels.HasValue ? CurrentRoiValue : "Full frame"
            },
            NormalizationNotes = new[] { note }
        };
        AppliedSettingsOutput = AppliedSettingsOutput with
        {
            SessionSettings = AppliedSettingsOutput.SessionSettings.WithOutputSettings(_outputSettings)
        };
    }

    private async Task<bool> TryApplySettingsSnapshotAsync(string successStatusMessage)
    {
        if (SelectedCamera is null)
        {
            SetLastError("No HuaTeng camera is available over USB.");
            StatusMessage = "No HuaTeng camera is available over USB.";
            FooterSystemStateLabel = "No camera";
            return false;
        }

        var success = false;
        await RunBusyAsync(async () =>
        {
            var result = await _probeClient.CaptureSnapshotAsync(
                SelectedCamera.Index,
                _selectedPixelFormat,
                _selectedTriggerMode,
                ParseExposure(_exposureInput),
                _appliedRoiPixels).ConfigureAwait(false);

            _latestDiagnostics = result.Diagnostics.ToList();
            if (!result.Ok)
            {
                SetLastHardwareResponse(result.Summary);
                SetLastError(result.Summary);
                StatusMessage = $"Applying settings failed: {result.Summary}";
                FooterSystemStateLabel = "Fault";
                return;
            }

            ClearLastError();
            SetLastHardwareResponse(result.Summary);
            PreviewImage = LoadImage(result);
            _currentFrameWidth = result.Width;
            _currentFrameHeight = result.Height;
            if (!_hasRoiSelection)
            {
                _fullFrameWidth = result.Width;
                _fullFrameHeight = result.Height;
            }

            CurrentFrameValue = $"{result.Width} x {result.Height}";
            ResolutionText = $"{result.Width} x {result.Height}";
            PixelFormatText = result.PixelFormat;
            ExposureText = $"{result.ExposureUs:0} us";
            FrameRateText = "--";
            StatusMessage = successStatusMessage;
            FooterSystemStateLabel = "Frame ready";
            FooterFormatLabel = result.PixelFormat;
            FooterTriggerLabel = result.TriggerMode;
            if (result.Camera is not null)
            {
                SelectedCamera = result.Camera;
            }

            OnPropertyChanged(nameof(CanSetRoi));
            UpdateRoiSummary();
            CaptureFrameOutput(result, "apply");
            success = true;
        }).ConfigureAwait(false);

        return success;
    }

    private void PublishFrameOutput(string? deviceId, string? endpointId, string payloadValue)
    {
        var timestamp = _lastFrameCapturedAt ?? DateTimeOffset.Now;
        if (!_outputPublisher.ShouldPublish(_outputSettings, timestamp, payloadValue))
        {
            return;
        }

        DataOutput = new IntegrationPanelDataOutput
        {
            Timestamp = _outputSettings.IncludeMetadata ? timestamp : null,
            DeviceId = _outputSettings.IncludeMetadata ? deviceId : null,
            EndpointId = _outputSettings.IncludeMetadata ? endpointId : null,
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

    private IntegrationPanelStatusOutput BuildStatusOutput()
    {
        return new IntegrationPanelStatusOutput
        {
            Connected = IsConnected,
            ReadyState = _isBusy || _isConnecting ? "Busy" : IsConnected ? "Ready" : "Disconnected",
            FaultState = _lastError,
            LiveState = IsLivePreviewing ? "Streaming" : "Stopped",
            SelectedEndpoint = SelectedCamera is null ? null : $"HuaTeng:{SelectedCamera.DisplayName}",
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
}
